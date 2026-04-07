// Path: Assets/Project/Scpripts/Ghosts/GhostSpawnSystem.cs
// Purpose: Spawns pooled Pale Drifts at night, requests the Lord Wraith during the pre-dawn window, and despawns all ghosts at daybreak.
// Dependencies: MessagePipe, GhostSpawnConfig, GhostSpawnArea, GhostBase, ICampfireService, IDayNightService, IHealthService, IRandomProvider, VContainer.

using System;
using System.Collections.Generic;
using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.Common.Messages;
using ProjectResonance.Common.Random;
using ProjectResonance.DayNight;
using ProjectResonance.Health;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Owns pooled spawning for all nightly ghost enemies.
    /// </summary>
    public sealed class GhostSpawnSystem : IStartable, IDisposable
    {
        private readonly GhostSpawnConfig _config;
        private readonly GhostSpawnArea _spawnArea;
        private readonly PlayerSurvivor _player;
        private readonly ICampfireService _campfireService;
        private readonly IRandomProvider _randomProvider;
        private readonly IObjectResolver _resolver;
        private readonly IHealthService _healthService;
        private readonly IDayNightService _dayNightService;
        private readonly ISubscriber<GhostsActivateEvent> _ghostsActivateSubscriber;
        private readonly ISubscriber<GhostsDeactivateEvent> _ghostsDeactivateSubscriber;
        private readonly ISubscriber<LordWraithSpawnRequestEvent> _lordWraithSpawnRequestSubscriber;
        private readonly ISubscriber<HealthDepletedMessage> _healthDepletedSubscriber;

        private readonly List<GhostBase> _activeGhosts = new List<GhostBase>(16);

        private ObjectPool<GhostBase> _paleDriftPool;
        private ObjectPool<GhostBase> _lordWraithPool;
        private IDisposable _ghostsActivateSubscription;
        private IDisposable _ghostsDeactivateSubscription;
        private IDisposable _lordWraithSpawnRequestSubscription;
        private IDisposable _healthDepletedSubscription;
        private bool _nightIsActive;
        private bool _lordWraithSpawnedThisNight;
        private int _currentDayNumber = 1;

        /// <summary>
        /// Initializes the ghost spawn system.
        /// </summary>
        /// <param name="config">Ghost configuration.</param>
        /// <param name="spawnArea">Scene spawn area anchor.</param>
        /// <param name="player">Scene player anchor.</param>
        /// <param name="campfireService">Runtime campfire service.</param>
        /// <param name="randomProvider">Injectable random source.</param>
        /// <param name="resolver">Object resolver used to instantiate pooled ghosts.</param>
        /// <param name="healthService">Runtime player health service.</param>
        /// <param name="dayNightService">Runtime day and night service.</param>
        /// <param name="ghostsActivateSubscriber">Night activation subscriber.</param>
        /// <param name="ghostsDeactivateSubscriber">Daybreak deactivation subscriber.</param>
        /// <param name="lordWraithSpawnRequestSubscriber">Lord Wraith request subscriber.</param>
        /// <param name="healthDepletedSubscriber">Player death subscriber.</param>
        public GhostSpawnSystem(
            GhostSpawnConfig config,
            GhostSpawnArea spawnArea,
            PlayerSurvivor player,
            ICampfireService campfireService,
            IRandomProvider randomProvider,
            IObjectResolver resolver,
            IHealthService healthService,
            IDayNightService dayNightService,
            ISubscriber<GhostsActivateEvent> ghostsActivateSubscriber,
            ISubscriber<GhostsDeactivateEvent> ghostsDeactivateSubscriber,
            ISubscriber<LordWraithSpawnRequestEvent> lordWraithSpawnRequestSubscriber,
            ISubscriber<HealthDepletedMessage> healthDepletedSubscriber)
        {
            _config = config;
            _spawnArea = spawnArea;
            _player = player;
            _campfireService = campfireService;
            _randomProvider = randomProvider;
            _resolver = resolver;
            _healthService = healthService;
            _dayNightService = dayNightService;
            _ghostsActivateSubscriber = ghostsActivateSubscriber;
            _ghostsDeactivateSubscriber = ghostsDeactivateSubscriber;
            _lordWraithSpawnRequestSubscriber = lordWraithSpawnRequestSubscriber;
            _healthDepletedSubscriber = healthDepletedSubscriber;
        }

        /// <summary>
        /// Starts the spawn pools and event subscriptions.
        /// </summary>
        public void Start()
        {
            _paleDriftPool = new ObjectPool<GhostBase>(
                CreatePaleDrift,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyedFromPool,
                false,
                Mathf.Max(1, _config != null ? _config.PaleDriftPoolCapacity : 8),
                Mathf.Max(1, _config != null ? _config.MaxPaleDrifts : 8));

            _lordWraithPool = new ObjectPool<GhostBase>(
                CreateLordWraith,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyedFromPool,
                false,
                Mathf.Max(1, _config != null ? _config.LordWraithPoolCapacity : 1),
                1);

            _ghostsActivateSubscription = _ghostsActivateSubscriber.Subscribe(_ => HandleGhostsActivated());
            _ghostsDeactivateSubscription = _ghostsDeactivateSubscriber.Subscribe(_ => HandleGhostsDeactivated());
            _lordWraithSpawnRequestSubscription = _lordWraithSpawnRequestSubscriber.Subscribe(_ => TrySpawnLordWraith());
            _healthDepletedSubscription = _healthDepletedSubscriber.Subscribe(_ => DespawnAllGhosts());

            if (_dayNightService != null &&
                (_dayNightService.CurrentTimeOfDay == TimeOfDay.Night || _dayNightService.CurrentTimeOfDay == TimeOfDay.PreDawn))
            {
                HandleGhostsActivated();

                if (IsLordWraithSpawnWindow())
                {
                    TrySpawnLordWraith();
                }
            }
        }

        /// <summary>
        /// Stops subscriptions and disposes all pools.
        /// </summary>
        public void Dispose()
        {
            _ghostsActivateSubscription?.Dispose();
            _ghostsDeactivateSubscription?.Dispose();
            _lordWraithSpawnRequestSubscription?.Dispose();
            _healthDepletedSubscription?.Dispose();

            DespawnAllGhosts();
            _paleDriftPool?.Dispose();
            _lordWraithPool?.Dispose();
        }

        private void HandleGhostsActivated()
        {
            if (_config == null || _spawnArea == null || _healthService == null || !_healthService.IsAlive)
            {
                return;
            }

            _nightIsActive = true;
            _lordWraithSpawnedThisNight = false;
            DespawnAllGhosts();
            SpawnPaleDrifts();
        }

        private void HandleGhostsDeactivated()
        {
            if (!_nightIsActive)
            {
                return;
            }

            _nightIsActive = false;
            _lordWraithSpawnedThisNight = false;
            DespawnAllGhosts();
            _currentDayNumber++;
        }

        private void SpawnPaleDrifts()
        {
            if (_config == null || _config.PaleDriftPrefab == null)
            {
                return;
            }

            var spawnCount = _config.GetPaleDriftCountForDay(_currentDayNumber);
            for (var spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
            {
                if (!TryFindSpawnPosition(out var spawnPosition))
                {
                    continue;
                }

                var paleDrift = _paleDriftPool.Get();
                paleDrift.Activate(spawnPosition);
            }
        }

        private void TrySpawnLordWraith()
        {
            if (!_nightIsActive || _lordWraithSpawnedThisNight || _healthService == null || !_healthService.IsAlive || !IsLordWraithSpawnWindow())
            {
                return;
            }

            if (_config == null || _config.LordWraithPrefab == null || !TryFindSpawnPosition(out var spawnPosition))
            {
                return;
            }

            var lordWraith = _lordWraithPool.Get();
            lordWraith.Activate(spawnPosition);
            _lordWraithSpawnedThisNight = true;
        }

        private bool TryFindSpawnPosition(out Vector3 spawnPosition)
        {
            var areaCenter = _spawnArea.Center;
            var attempts = _config != null ? _config.SpawnPositionAttempts : 12;

            for (var attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
            {
                var offset = _randomProvider.InsideUnitCircle() * _spawnArea.Radius;
                var candidate = areaCenter + new Vector3(offset.x, 0f, offset.y);

                // Ghost entrances stay off-screen and unfair pop-ins stay away from the player.
                if (GetPlanarDistance(candidate, _player.Position) < (_config != null ? _config.MinSpawnDistanceFromPlayer : 20f))
                {
                    continue;
                }

                if (_campfireService != null && _campfireService.IsLit)
                {
                    // Active campfire space is treated as denied spawn area, with a small extra buffer outside the safe perimeter.
                    var minimumCampfireDistance = _campfireService.ProtectionRadius + (_config != null ? _config.MinSpawnDistanceFromCampfire : 4f);
                    if (GetPlanarDistance(candidate, _campfireService.Position) < minimumCampfireDistance)
                    {
                        continue;
                    }
                }

                spawnPosition = candidate;
                return true;
            }

            spawnPosition = default;
            return false;
        }

        private GhostBase CreatePaleDrift()
        {
            var paleDrift = _resolver.Instantiate(_config.PaleDriftPrefab, _spawnArea.transform);
            paleDrift.BindPool(_paleDriftPool);
            paleDrift.gameObject.SetActive(false);
            return paleDrift;
        }

        private GhostBase CreateLordWraith()
        {
            var lordWraith = _resolver.Instantiate(_config.LordWraithPrefab, _spawnArea.transform);
            lordWraith.BindPool(_lordWraithPool);
            lordWraith.gameObject.SetActive(false);
            return lordWraith;
        }

        private void OnTakeFromPool(GhostBase ghost)
        {
            if (ghost != null && !_activeGhosts.Contains(ghost))
            {
                _activeGhosts.Add(ghost);
            }
        }

        private void OnReturnedToPool(GhostBase ghost)
        {
            _activeGhosts.Remove(ghost);

            if (ghost != null)
            {
                ghost.gameObject.SetActive(false);
            }
        }

        private void OnDestroyedFromPool(GhostBase ghost)
        {
            if (ghost != null)
            {
                UnityEngine.Object.Destroy(ghost.gameObject);
            }
        }

        private void DespawnAllGhosts()
        {
            while (_activeGhosts.Count > 0)
            {
                _activeGhosts[_activeGhosts.Count - 1].ReturnToPool();
            }
        }

        private bool IsLordWraithSpawnWindow()
        {
            // The global clock starts at dawn, so adding six hours maps the normalized cycle onto a conventional 24-hour clock.
            var clockHour = Mathf.Repeat((_dayNightService != null ? _dayNightService.CurrentTimeNormalized : 0f) * 24f + 6f, 24f);
            return clockHour >= 3f && clockHour < 4f;
        }

        private static float GetPlanarDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}
