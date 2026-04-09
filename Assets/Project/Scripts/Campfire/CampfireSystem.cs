// Path: Assets/Project/Scpripts/Campfire/CampfireSystem.cs
// Purpose: Owns fuel burn, ignition, extinguish warning and level progression for the campfire.
// Dependencies: UniTask, CampfireConfig, CampfireState, CampfireAnchor, VContainer.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Exposes weather state needed by the campfire burn calculation.
    /// </summary>
    public interface ICampfireWeatherService
    {
        /// <summary>
        /// Gets whether rain is currently active.
        /// </summary>
        bool IsRaining { get; }

        /// <summary>
        /// Gets whether strong wind is currently active.
        /// </summary>
        bool IsWindy { get; }
    }

    /// <summary>
    /// Runtime system that owns the campfire lifecycle.
    /// </summary>
    public sealed class CampfireSystem : ICampfireService, IStartable, IDisposable
    {
        private readonly CampfireConfig _config;
        private readonly CampfireRuntimeState _state;
        private readonly CampfireAnchor _anchor;
        private readonly ICampfireWeatherService _weatherService;

        private CancellationTokenSource _burnLoopCancellation;
        private CancellationTokenSource _extinguishWarningCancellation;

        /// <summary>
        /// Creates the runtime campfire system.
        /// </summary>
        /// <param name="config">Campfire configuration asset.</param>
        /// <param name="stateTemplate">Shared authored campfire state template asset.</param>
        /// <param name="anchor">Scene anchor for the campfire.</param>
        /// <param name="weatherService">Optional weather service used to scale burn rate.</param>
        public CampfireSystem(
            CampfireConfig config,
            CampfireState stateTemplate,
            CampfireAnchor anchor,
            ICampfireWeatherService weatherService = null)
        {
            _config = config;
            _state = new CampfireRuntimeState(_config, stateTemplate);
            _anchor = anchor;
            _weatherService = weatherService;
        }

        /// <summary>
        /// Gets the shared runtime state object.
        /// </summary>
        public CampfireRuntimeState State => _state;

        /// <inheritdoc />
        public event Action<FuelChangedEvent> FuelChanged
        {
            add => _state.FuelChanged += value;
            remove => _state.FuelChanged -= value;
        }

        /// <summary>
        /// Gets whether the campfire is lit.
        /// </summary>
        public bool IsLit => _state.IsLit;

        /// <summary>
        /// Gets whether the campfire is dying.
        /// </summary>
        public bool IsDying => _state.IsDying;

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level => _state.Level;

        /// <summary>
        /// Gets the current world position of the campfire.
        /// </summary>
        public Vector3 Position => _anchor.FirePoint.position;

        /// <summary>
        /// Gets the current protection radius.
        /// </summary>
        public float ProtectionRadius => _state.ProtectionRadius;

        /// <summary>
        /// Gets the current light radius.
        /// </summary>
        public float LightRadius => _state.LightRadius;

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        public float CurrentFuel => _state.CurrentFuel;

        /// <summary>
        /// Gets the current maximum fuel capacity.
        /// </summary>
        public float MaxFuel => _state.MaxFuel;

        /// <summary>
        /// Starts the background burn loop.
        /// </summary>
        public void Start()
        {
            _state.PublishSnapshot();

            _burnLoopCancellation = new CancellationTokenSource();
            RunBurnLoopAsync(_burnLoopCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops all background tasks owned by the campfire system.
        /// </summary>
        public void Dispose()
        {
            CancelExtinguishWarning();

            if (_burnLoopCancellation != null)
            {
                _burnLoopCancellation.Cancel();
                _burnLoopCancellation.Dispose();
                _burnLoopCancellation = null;
            }
        }

        /// <summary>
        /// Adds fuel to the campfire.
        /// </summary>
        /// <param name="amount">Fuel amount to add.</param>
        public void AddFuel(float amount)
        {
            if (amount <= 0f || _state.Level == CampfireLevel.None || _state.MaxFuel <= 0f)
            {
                return;
            }

            CancelExtinguishWarning();

            // When fuel is restored during the warning window, the existing flame stays alive instead of forcing re-ignition.
            var shouldRemainLit = _state.IsLit;
            var nextFuel = Mathf.Clamp(_state.CurrentFuel + amount, 0f, _state.MaxFuel);
            _state.ApplyRuntimeState(_state.Level, nextFuel, shouldRemainLit);
        }

        /// <summary>
        /// Attempts to ignite the campfire.
        /// </summary>
        /// <returns>True when ignition succeeded.</returns>
        public bool Ignite()
        {
            if (_state.Level == CampfireLevel.None || _state.IsLit || _state.CurrentFuel <= 0f)
            {
                return false;
            }

            CancelExtinguishWarning();
            _state.ApplyRuntimeState(_state.Level, _state.CurrentFuel, true);
            return true;
        }

        /// <summary>
        /// Extinguishes the campfire immediately.
        /// </summary>
        public void Extinguish()
        {
            if (!_state.IsLit)
            {
                return;
            }

            CancelExtinguishWarning();
            _state.ApplyRuntimeState(_state.Level, _state.CurrentFuel, false);
        }

        /// <summary>
        /// Attempts to upgrade the campfire to the next available tier.
        /// </summary>
        /// <returns>True when the upgrade succeeded.</returns>
        public bool TryUpgrade()
        {
            if (!_config.TryGetNextLevel(_state.Level, out var nextLevel))
            {
                return false;
            }

            var nextLevelData = _config.GetLevelData(nextLevel);
            if (nextLevelData.FuelCapacity <= 0f)
            {
                return false;
            }

            var normalizedFuel = _state.CurrentFuelNormalized;
            var nextFuel = Mathf.Clamp(normalizedFuel * nextLevelData.FuelCapacity, 0f, nextLevelData.FuelCapacity);

            _state.ApplyRuntimeState(nextLevel, nextFuel, _state.IsLit && nextFuel > 0f);
            return true;
        }

        private async UniTaskVoid RunBurnLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_state.IsLit)
                    {
                        TickFuelBurn(Time.deltaTime);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Container disposal stops the loop through cancellation.
            }
        }

        private void TickFuelBurn(float deltaTime)
        {
            if (deltaTime <= 0f || _state.Level == CampfireLevel.None || _state.CurrentFuel <= 0f)
            {
                if (_state.IsLit && _state.CurrentFuel <= 0f)
                {
                    BeginExtinguishWarning();
                }

                return;
            }

            var burnRate = ResolveBurnRate();
            if (burnRate <= 0f)
            {
                return;
            }

            var nextFuel = Mathf.Max(0f, _state.CurrentFuel - (burnRate * deltaTime));
            _state.ApplyRuntimeState(_state.Level, nextFuel, true);

            if (nextFuel <= 0f)
            {
                BeginExtinguishWarning();
            }
        }

        private float ResolveBurnRate()
        {
            var burnRate = _config.GetLevelData(_state.Level).BurnRate;

            if (_weatherService != null && _weatherService.IsRaining)
            {
                burnRate *= _config.RainBurnMultiplier;
            }

            if (_weatherService != null && _weatherService.IsWindy)
            {
                burnRate *= _config.WindBurnMultiplier;
            }

            return burnRate;
        }

        private void BeginExtinguishWarning()
        {
            if (_extinguishWarningCancellation != null || !_state.IsLit)
            {
                return;
            }

            _extinguishWarningCancellation = new CancellationTokenSource();
            RunExtinguishWarningAsync(_extinguishWarningCancellation.Token).Forget();
        }

        private async UniTaskVoid RunExtinguishWarningAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.WarningBeforeExtinguish),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);

                _extinguishWarningCancellation?.Dispose();
                _extinguishWarningCancellation = null;

                if (_state.IsLit && _state.CurrentFuel <= 0f)
                {
                    _state.ApplyRuntimeState(_state.Level, 0f, false);
                }
            }
            catch (OperationCanceledException)
            {
                _extinguishWarningCancellation?.Dispose();
                _extinguishWarningCancellation = null;
            }
        }

        private void CancelExtinguishWarning()
        {
            if (_extinguishWarningCancellation == null)
            {
                return;
            }

            _extinguishWarningCancellation.Cancel();
            _extinguishWarningCancellation.Dispose();
            _extinguishWarningCancellation = null;
        }
    }
}
