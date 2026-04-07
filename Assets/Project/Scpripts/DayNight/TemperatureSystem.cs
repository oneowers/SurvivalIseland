// Path: Assets/Project/Scpripts/DayNight/TemperatureSystem.cs
// Purpose: Applies time-of-day based thermal damage and healing by publishing health-affecting events.
// Dependencies: UniTask, MessagePipe, VContainer, ICampfireService, TimeOfDayChangedEvent, PlayerInSafeZoneEvent.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.Campfire;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Event emitted when temperature should damage the player.
    /// </summary>
    public readonly struct ThermalDamageEvent
    {
        /// <summary>
        /// Creates a new thermal damage event.
        /// </summary>
        /// <param name="amount">Damage amount to apply.</param>
        public ThermalDamageEvent(float amount)
        {
            Amount = amount;
        }

        /// <summary>
        /// Gets the damage amount.
        /// </summary>
        public float Amount { get; }
    }

    /// <summary>
    /// Event emitted when temperature should heal the player.
    /// </summary>
    public readonly struct ThermalHealEvent
    {
        /// <summary>
        /// Creates a new thermal healing event.
        /// </summary>
        /// <param name="amount">Healing amount to apply.</param>
        public ThermalHealEvent(float amount)
        {
            Amount = amount;
        }

        /// <summary>
        /// Gets the healing amount.
        /// </summary>
        public float Amount { get; }
    }

    /// <summary>
    /// Runtime system that evaluates thermal pressure from current time-of-day conditions.
    /// </summary>
    public sealed class TemperatureSystem : IStartable, IDisposable
    {
        private readonly ICampfireService _campfireService;
        private readonly ISubscriber<TimeOfDayChangedEvent> _timeOfDayChangedSubscriber;
        private readonly IBufferedSubscriber<PlayerInSafeZoneEvent> _playerInSafeZoneSubscriber;
        private readonly IPublisher<ThermalDamageEvent> _thermalDamagePublisher;
        private readonly IPublisher<ThermalHealEvent> _thermalHealPublisher;

        private CancellationTokenSource _loopCancellation;
        private IDisposable _timeOfDaySubscription;
        private IDisposable _safeZoneSubscription;
        private TimeOfDay _currentTimeOfDay = TimeOfDay.Dawn;
        private bool _isPlayerInSafeZone;
        private float _coldTimer;
        private float _heatTimer;
        private float _campfireHealTimer;

        /// <summary>
        /// Creates the runtime temperature system.
        /// </summary>
        /// <param name="campfireService">Campfire runtime service.</param>
        /// <param name="timeOfDayChangedSubscriber">Time-of-day phase subscriber.</param>
        /// <param name="playerInSafeZoneSubscriber">Campfire safe-zone subscriber.</param>
        /// <param name="thermalDamagePublisher">Thermal damage publisher.</param>
        /// <param name="thermalHealPublisher">Thermal healing publisher.</param>
        public TemperatureSystem(
            ICampfireService campfireService,
            ISubscriber<TimeOfDayChangedEvent> timeOfDayChangedSubscriber,
            IBufferedSubscriber<PlayerInSafeZoneEvent> playerInSafeZoneSubscriber,
            IPublisher<ThermalDamageEvent> thermalDamagePublisher,
            IPublisher<ThermalHealEvent> thermalHealPublisher)
        {
            _campfireService = campfireService;
            _timeOfDayChangedSubscriber = timeOfDayChangedSubscriber;
            _playerInSafeZoneSubscriber = playerInSafeZoneSubscriber;
            _thermalDamagePublisher = thermalDamagePublisher;
            _thermalHealPublisher = thermalHealPublisher;
        }

        /// <summary>
        /// Starts subscriptions and the background thermal loop.
        /// </summary>
        public void Start()
        {
            _timeOfDaySubscription = _timeOfDayChangedSubscriber.Subscribe(OnTimeOfDayChanged);
            _safeZoneSubscription = _playerInSafeZoneSubscriber.Subscribe(OnPlayerInSafeZoneChanged);

            _loopCancellation = new CancellationTokenSource();
            RunThermalLoopAsync(_loopCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops subscriptions and async work.
        /// </summary>
        public void Dispose()
        {
            _timeOfDaySubscription?.Dispose();
            _safeZoneSubscription?.Dispose();

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
                    _thermalHealPublisher.Publish(new ThermalHealEvent(2f));
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
                    _thermalDamagePublisher.Publish(new ThermalDamageEvent(2f));
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
                    _thermalDamagePublisher.Publish(new ThermalDamageEvent(1f));
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
