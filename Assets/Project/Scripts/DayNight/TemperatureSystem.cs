// Path: Assets/Project/Scpripts/DayNight/TemperatureSystem.cs
// Purpose: Applies time-of-day based thermal damage and healing directly through the health service.
// Dependencies: UniTask, VContainer, ICampfireService, DayNightSystem, CampfireProtectionZone, IHealthService.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.Campfire;
using ProjectResonance.Health;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Runtime system that evaluates thermal pressure from current time-of-day conditions.
    /// </summary>
    public sealed class TemperatureSystem : IStartable, IDisposable
    {
        private readonly ICampfireService _campfireService;
        private readonly DayNightSystem _dayNightSystem;
        private readonly CampfireProtectionZone _campfireProtectionZone;
        private readonly IHealthService _healthService;

        private CancellationTokenSource _loopCancellation;
        private TimeOfDay _currentTimeOfDay = TimeOfDay.Dawn;
        private bool _isPlayerInSafeZone;
        private float _coldTimer;
        private float _heatTimer;
        private float _campfireHealTimer;

        /// <summary>
        /// Creates the runtime temperature system.
        /// </summary>
        /// <param name="campfireService">Campfire runtime service.</param>
        public TemperatureSystem(
            ICampfireService campfireService,
            DayNightSystem dayNightSystem,
            CampfireProtectionZone campfireProtectionZone,
            IHealthService healthService)
        {
            _campfireService = campfireService;
            _dayNightSystem = dayNightSystem;
            _campfireProtectionZone = campfireProtectionZone;
            _healthService = healthService;
        }

        /// <summary>
        /// Starts subscriptions and the background thermal loop.
        /// </summary>
        public void Start()
        {
            if (_dayNightSystem != null)
            {
                _currentTimeOfDay = _dayNightSystem.CurrentTimeOfDay;
                _dayNightSystem.TimeOfDayChanged += OnTimeOfDayChanged;
            }

            if (_campfireProtectionZone != null)
            {
                _campfireProtectionZone.PlayerSafeZoneChanged += OnPlayerInSafeZoneChanged;
            }

            _loopCancellation = new CancellationTokenSource();
            RunThermalLoopAsync(_loopCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops subscriptions and async work.
        /// </summary>
        public void Dispose()
        {
            if (_dayNightSystem != null)
            {
                _dayNightSystem.TimeOfDayChanged -= OnTimeOfDayChanged;
            }

            if (_campfireProtectionZone != null)
            {
                _campfireProtectionZone.PlayerSafeZoneChanged -= OnPlayerInSafeZoneChanged;
            }

            if (_loopCancellation == null)
            {
                return;
            }

            _loopCancellation.Cancel();
            _loopCancellation.Dispose();
            _loopCancellation = null;
        }

        private async UniTaskVoid RunThermalLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TickThermalState(Time.deltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Lifetime scope disposal stops the thermal loop through cancellation.
            }
        }

        private void TickThermalState(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            // A lit campfire inside the protection zone is the only authored thermal recovery source in this sprint.
            if (_campfireService != null && _campfireService.IsLit && _isPlayerInSafeZone)
            {
                _campfireHealTimer += deltaTime;
                while (_campfireHealTimer >= 10f)
                {
                    _campfireHealTimer -= 10f;
                    _healthService?.ApplyHealing(2f);
                }
            }
            else
            {
                _campfireHealTimer = 0f;
            }

            if (IsColdDamageWindow() && !_isPlayerInSafeZone)
            {
                _coldTimer += deltaTime;
                while (_coldTimer >= 60f)
                {
                    _coldTimer -= 60f;
                    _healthService?.ApplyDamage(2f);
                }
            }
            else
            {
                _coldTimer = 0f;
            }

            if (_currentTimeOfDay == TimeOfDay.Noon && !_isPlayerInSafeZone)
            {
                _heatTimer += deltaTime;
                while (_heatTimer >= 60f)
                {
                    _heatTimer -= 60f;
                    _healthService?.ApplyDamage(1f);
                }
            }
            else
            {
                _heatTimer = 0f;
            }
        }

        private bool IsColdDamageWindow()
        {
            return _currentTimeOfDay == TimeOfDay.Night || _currentTimeOfDay == TimeOfDay.PreDawn;
        }

        private void OnTimeOfDayChanged(TimeOfDayChangedEvent message)
        {
            _currentTimeOfDay = message.CurrentTimeOfDay;
        }

        private void OnPlayerInSafeZoneChanged(PlayerInSafeZoneEvent message)
        {
            _isPlayerInSafeZone = message.IsInside;
        }
    }
}
