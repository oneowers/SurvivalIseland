// Path: Assets/Project/Scripts/Campfire/CampfireRuntimeState.cs
// Purpose: Stores mutable campfire runtime state and exposes explicit C# events.
// Dependencies: UnityEngine.

using System;
using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Mutable runtime state for the active campfire instance.
    /// </summary>
    public sealed class CampfireRuntimeState
    {
        private readonly CampfireConfig _config;

        /// <summary>
        /// Creates the runtime campfire state from authored assets.
        /// </summary>
        /// <param name="config">Campfire config asset.</param>
        /// <param name="template">Campfire template asset.</param>
        public CampfireRuntimeState(CampfireConfig config, CampfireState template)
        {
            _config = config;

            var initialLevel = template != null ? template.InitialLevel : CampfireLevel.Basic;
            var initialFuelRatio = template != null ? template.ResolveInitialFuelRatio() : 1f;
            var startsLit = template == null || template.StartsLit;

            Level = initialLevel;
            RefreshLevelData();
            CurrentFuel = Mathf.Clamp(initialFuelRatio * MaxFuel, 0f, MaxFuel);
            IsLit = Level != CampfireLevel.None && startsLit && CurrentFuel > 0f;
        }

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level { get; private set; }

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        public float CurrentFuel { get; private set; }

        /// <summary>
        /// Gets the current max fuel amount.
        /// </summary>
        public float MaxFuel { get; private set; }

        /// <summary>
        /// Gets whether the campfire is currently lit.
        /// </summary>
        public bool IsLit { get; private set; }

        /// <summary>
        /// Gets whether the campfire is currently in the dying state.
        /// </summary>
        public bool IsDying => IsLit && _config != null && MaxFuel > 0f && CurrentFuelNormalized < _config.DyingThreshold;

        /// <summary>
        /// Gets the normalized current fuel ratio.
        /// </summary>
        public float CurrentFuelNormalized => MaxFuel > 0f ? Mathf.Clamp01(CurrentFuel / MaxFuel) : 0f;

        /// <summary>
        /// Gets the current protection radius.
        /// </summary>
        public float ProtectionRadius => ResolveLevelData().ProtectionRadius;

        /// <summary>
        /// Gets the current light radius.
        /// </summary>
        public float LightRadius => ResolveLevelData().LightRadius;

        /// <summary>
        /// Raised whenever the fuel snapshot changes.
        /// </summary>
        public event Action<FuelChangedEvent> FuelChanged;

        /// <summary>
        /// Raised when the campfire is lit.
        /// </summary>
        public event Action<CampfireLitEvent> Lit;

        /// <summary>
        /// Raised when the campfire enters the dying state.
        /// </summary>
        public event Action<CampfireDyingEvent> Dying;

        /// <summary>
        /// Raised when the campfire extinguishes.
        /// </summary>
        public event Action<CampfireExtinguishedEvent> Extinguished;

        /// <summary>
        /// Raised when the campfire levels up.
        /// </summary>
        public event Action<CampfireLevelUpEvent> LevelUp;

        /// <summary>
        /// Publishes the current runtime snapshot.
        /// </summary>
        public void PublishSnapshot()
        {
            FuelChanged?.Invoke(new FuelChangedEvent(CurrentFuel, MaxFuel, Level, IsLit, IsDying, ProtectionRadius));
        }

        /// <summary>
        /// Applies a new runtime campfire state.
        /// </summary>
        /// <param name="level">Next campfire level.</param>
        /// <param name="currentFuel">Next current fuel value.</param>
        /// <param name="isLit">Next lit flag.</param>
        public void ApplyRuntimeState(CampfireLevel level, float currentFuel, bool isLit)
        {
            var previousLevel = Level;
            var previousLit = IsLit;
            var previousDying = IsDying;

            Level = level;
            RefreshLevelData();

            CurrentFuel = Mathf.Clamp(currentFuel, 0f, MaxFuel);
            IsLit = Level != CampfireLevel.None && isLit && CurrentFuel > 0f;

            var currentDying = IsDying;

            if (previousLevel != Level && (int)Level > (int)previousLevel)
            {
                LevelUp?.Invoke(new CampfireLevelUpEvent(previousLevel, Level));
            }

            if (!previousLit && IsLit)
            {
                Lit?.Invoke(new CampfireLitEvent(Level));
            }

            if (!previousDying && currentDying)
            {
                Dying?.Invoke(new CampfireDyingEvent(CurrentFuel, MaxFuel, Level));
            }

            if (previousLit && !IsLit)
            {
                Extinguished?.Invoke(new CampfireExtinguishedEvent(Level));
            }

            PublishSnapshot();
        }

        private void RefreshLevelData()
        {
            if (_config == null || Level == CampfireLevel.None)
            {
                MaxFuel = 0f;
                return;
            }

            MaxFuel = Mathf.Max(0f, _config.GetLevelData(Level).FuelCapacity);
        }

        private CampfireLevelData ResolveLevelData()
        {
            if (_config == null || Level == CampfireLevel.None)
            {
                return default;
            }

            return _config.GetLevelData(Level);
        }
    }
}
