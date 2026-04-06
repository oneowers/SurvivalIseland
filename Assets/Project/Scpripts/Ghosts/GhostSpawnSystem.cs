// Path: Assets/Project/Scpripts/Ghosts/GhostSpawnSystem.cs
// Purpose: Spawns and recycles ghosts during the night cycle.
// Dependencies: UniTask, MessagePipe, GhostSpawnerConfig, ICampfireService, IDayNightService, IHealthService, IRandomProvider, VContainer.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    /// Owns ghost pooling and night-time spawning.
    /// </summary>
    public sealed class GhostSpawnSystem : IStartable, IDisposable
    {
        private readonly GhostSpawnerConfig _config;
        private readonly GhostSpawnArea _spawnArea;
        private readonly PlayerSurvivor _player;
        private readonly IHealthService _healthService;
        private readonly IDayNightService _dayNightService;
        private readonly ICampfireService _campfireService;
        private readonly IRandomProvider _randomProvider;
        private readonly IObjectResolver _resolver;
        private readonly ISubscriber<DayPhaseChangedMessage> _dayPhaseSubscriber;
        private readonly ISubscriber<HealthDepletedMessage> _healthDepletedSubscriber;

        private readonly List<GhostPresenter> _activeGhosts = new List<GhostPresenter>();

        private CancellationTokenSource _spawnLoopCancellation;
        private ObjectPool<GhostPresenter> _pool;
        private IDisposable _dayPhaseSubscription;
        private IDisposable _healthDepletedSubscription;

        /// <summary>
        /// Initializes the ghost spawn system.
        /// </summary>
        /// <param name="config">Ghost configuration.</param>
        /// <param name="spawnArea">Scene spawn area anchor.</param>
        /// <param name="player">Scene player anchor.</param>
        /// <param name="healthService">Runtime health service.</param>
        /// <param name="dayNightService">Runtime day and night service.</param>
        /// <param name="campfireService">Runtime campfire service.</param>
        /// <param name="randomProvider">Injectable random source.</param>
        /// <param name="resolver">Object resolver used to instantiate pooled ghosts.</param>
        /// <param name="dayPhaseSubscriber">Day and night phase subscriber.</param>
        /// <param name="healthDepletedSubscriber">Player death subscriber.</param>
        public GhostSpawnSystem(
            GhostSpawnerConfig config,
            GhostSpawnArea spawnArea,
            PlayerSurvivor player,
            IHealthService healthService,
            IDayNightService dayNightService,
            ICampfireService campfireService,
            IRandomProvider randomProvider,
            IObjectResolver resolver,
            ISubscriber<DayPhaseChangedMessage> dayPhaseSubscriber,
            ISubscriber<HealthDepletedMessage> healthDepletedSubscriber)
        {
            _config = config;
            _spawnArea = spawnArea;
            _player = player;
            _healthService = healthService;
            _dayNightService = dayNightService;
            _campfireService = campfireService;
            _randomProvider = randomProvider;
            _resolver = resolver;
            _dayPhaseSubscriber = dayPhaseSubscriber;
            _healthDepletedSubscriber = healthDepletedSubscriber;
        }

        /// <summary>
        /// Starts subscriptions and the background spawn loop.
        /// </summary>
        public void Start()
        {
            var poolCapacity = Mathf.Max(1, _config.DefaultPoolCapacity);
            var maxPoolSize = Mathf.Max(poolCapacity, _config.MaxAliveGhosts);

            _pool = new ObjectPool<GhostPresenter>(
                CreateGhost,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyedFromPool,
                false,
                poolCapacity,
                maxPoolSize);

            _dayPhaseSubscription = _dayPhaseSubscriber.Subscribe(OnDayPhaseChanged);
            _healthDepletedSubscription = _healthDepletedSubscriber.Subscribe(_ => DespawnAllGhosts());

            _spawnLoopCancellation = new CancellationTokenSource();
            RunSpawnLoopAsync(_spawnLoopCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops the spawn loop and releases all runtime resources.
        /// </summary>
        public void Dispose()
        {
            _dayPhaseSubscription?.Dispose();
            _healthDepletedSubscription?.Dispose();

            if (_spawnLoopCancellation != null)
            {
                _spawnLoopCancellation.Cancel();
                _spawnLoopCancellation.Dispose();
                _spawnLoopCancellation = null;
            }

            DespawnAllGhosts();
            _pool?.Dispose();
        }

        private async UniTaskVoid RunSpawnLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (CanSpawnGhost())
                    {
                        TrySpawnGhost();
                    }

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.SpawnIntervalSeconds),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The loop is expected to stop through container disposal.
            }
        }

        private bool CanSpawnGhost()
        {
            return _dayNightService.CurrentPhase == DayPhase.Night &&
                   _healthService.IsAlive &&
                   _activeGhosts.Count < _config.MaxAliveGhosts &&
                   _config.GhostPrefab != null;
        }

        private void TrySpawnGhost()
        {
            if (!TryFindSpawnPosition(out var spawnPosition))
            {
                return;
            }

            var ghost = _pool.Get();
            ghost.Activate(spawnPosition);
        }

        private bool TryFindSpawnPosition(out Vector3 spawnPosition)
        {
            var areaCenter = _spawnArea.Center;
            for (var attemptIndex = 0; attemptIndex < _config.SpawnPositionAttempts; attemptIndex++)
            {
                var offset = _randomProvider.InsideUnitCircle() * _spawnArea.Radius;
                var candidate = areaCenter + new Vector3(offset.x, 0f, offset.y);

                // Keep ghost entries away from the player so spawn events do not feel unfair or artificial.
                if (GetPlanarDistance(candidate, _player.Position) < _config.MinSpawnDistanceFromPlayer)
                {
                    continue;
                }

                if (_campfireService.IsLit)
                {
                    var minimumCampfireDistance = _campfireService.ProtectionRadius + _config.MinDistanceFromCampfire;
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

        private GhostPresenter CreateGhost()
        {
            var ghost = _resolver.Instantiate(_config.GhostPrefab, _spawnArea.transform);
            ghost.BindPool(_pool);
            ghost.gameObject.SetActive(false);
            return ghost;
        }

        private void OnTakeFromPool(GhostPresenter ghost)
        {
            if (!_activeGhosts.Contains(ghost))
            {
                _activeGhosts.Add(ghost);
            }
        }

        private void OnReturnedToPool(GhostPresenter ghost)
        {
            _activeGhosts.Remove(ghost);
            ghost.gameObject.SetActive(false);
        }

        private void OnDestroyedFromPool(GhostPresenter ghost)
        {
            if (ghost != null)
            {
                UnityEngine.Object.Destroy(ghost.gameObject);
            }
        }

        private void OnDayPhaseChanged(DayPhaseChangedMessage message)
        {
            if (message.CurrentPhase == DayPhase.Day)
            {
                DespawnAllGhosts();
            }
        }

        private void DespawnAllGhosts()
        {
            while (_activeGhosts.Count > 0)
            {
                _activeGhosts[_activeGhosts.Count - 1].Despawn();
            }
        }

        private static float GetPlanarDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}
