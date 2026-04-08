// Path: Assets/Project/Scpripts/Campfire/Configs/CampfireConfig.cs
// Purpose: Stores progression, burn and interaction tuning for the campfire module.
// Dependencies: UnityEngine, CampfireState.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Per-level configuration data for a campfire upgrade tier.
    /// </summary>
    [System.Serializable]
    public struct CampfireLevelData
    {
        [SerializeField]
        private CampfireLevel _level;

        [SerializeField]
        [Min(0f)]
        private float _protectionRadius;

        [SerializeField]
        [Min(0f)]
        private float _fuelCapacity;

        [SerializeField]
        [Min(0f)]
        private float _burnRate;

        [SerializeField]
        [Min(0f)]
        private float _lightRadius;

        /// <summary>
        /// Gets the upgrade tier this data belongs to.
        /// </summary>
        public CampfireLevel Level => _level;

        /// <summary>
        /// Gets the safe-zone radius at this tier.
        /// </summary>
        public float ProtectionRadius => _protectionRadius;

        /// <summary>
        /// Gets the maximum fuel capacity at this tier.
        /// </summary>
        public float FuelCapacity => _fuelCapacity;

        /// <summary>
        /// Gets the base fuel burn rate at this tier.
        /// </summary>
        public float BurnRate => _burnRate;

        /// <summary>
        /// Gets the effective light radius at this tier.
        /// </summary>
        public float LightRadius => _lightRadius;
    }

    /// <summary>
    /// ScriptableObject that defines all tunable campfire parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "CampfireConfig", menuName = "Project Resonance/Campfire/Campfire Config")]
    public sealed class CampfireConfig : ScriptableObject
    {
        [SerializeField]
        private CampfireLevelData[] _levels;

        [SerializeField]
        [Range(0.01f, 1f)]
        private float _dyingThreshold = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float _warningBeforeExtinguish = 30f;

        [SerializeField]
        [Min(1f)]
        private float _rainBurnMultiplier = 2f;

        [SerializeField]
        [Min(1f)]
        private float _windBurnMultiplier = 1.5f;

        [SerializeField]
        [Min(0f)]
        private float _fuelPerLog = 45f;

        /// <summary>
        /// Gets the configured level data array.
        /// </summary>
        public CampfireLevelData[] Levels => _levels;

        /// <summary>
        /// Gets the normalized fuel threshold that marks the fire as dying.
        /// </summary>
        public float DyingThreshold => _dyingThreshold;

        /// <summary>
        /// Gets the warning duration before an empty fire is extinguished.
        /// </summary>
        public float WarningBeforeExtinguish => _warningBeforeExtinguish;

        /// <summary>
        /// Gets the burn multiplier used while rain is active.
        /// </summary>
        public float RainBurnMultiplier => _rainBurnMultiplier;

        /// <summary>
        /// Gets the burn multiplier used while strong wind is active.
        /// </summary>
        public float WindBurnMultiplier => _windBurnMultiplier;

        /// <summary>
        /// Gets how much fuel a single carried log contributes.
        /// </summary>
        public float FuelPerLog => _fuelPerLog;

        /// <summary>
        /// Returns the level data for the requested campfire tier.
        /// </summary>
        /// <param name="level">Campfire tier to resolve.</param>
        /// <returns>Resolved level data, or a zeroed fallback when the tier is missing.</returns>
        public CampfireLevelData GetLevelData(CampfireLevel level)
        {
            if (_levels != null)
            {
                for (var index = 0; index < _levels.Length; index++)
                {
                    if (_levels[index].Level == level)
                    {
                        return _levels[index];
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Tries to resolve the next available upgrade tier.
        /// </summary>
        /// <param name="currentLevel">Current campfire tier.</param>
        /// <param name="nextLevel">Resolved next tier.</param>
        /// <returns>True when another tier exists.</returns>
        public bool TryGetNextLevel(CampfireLevel currentLevel, out CampfireLevel nextLevel)
        {
            switch (currentLevel)
            {
                case CampfireLevel.None:
                    nextLevel = CampfireLevel.Basic;
                    return HasLevel(nextLevel);
                case CampfireLevel.Basic:
                    nextLevel = CampfireLevel.Reinforced;
                    return HasLevel(nextLevel);
                case CampfireLevel.Reinforced:
                    nextLevel = CampfireLevel.Signal;
                    return HasLevel(nextLevel);
                default:
                    nextLevel = currentLevel;
                    return false;
            }
        }

        /// <summary>
        /// Returns whether a configuration entry exists for the provided tier.
        /// </summary>
        /// <param name="level">Campfire tier to query.</param>
        /// <returns>True when the tier is configured.</returns>
        public bool HasLevel(CampfireLevel level)
        {
            if (_levels == null)
            {
                return false;
            }

            for (var index = 0; index < _levels.Length; index++)
            {
                if (_levels[index].Level == level)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
