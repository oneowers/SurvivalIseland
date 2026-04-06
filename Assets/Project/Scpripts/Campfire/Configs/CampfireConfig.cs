// Path: Assets/Project/Scpripts/Campfire/Configs/CampfireConfig.cs
// Purpose: Stores tunable settings for the campfire system.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// ScriptableObject with campfire settings.
    /// </summary>
    [CreateAssetMenu(fileName = "CampfireConfig", menuName = "Project Resonance/Campfire/Campfire Config")]
    public sealed class CampfireConfig : ScriptableObject
    {
        [SerializeField]
        [Min(0f)]
        private float _startFuelSeconds = 180f;

        [SerializeField]
        [Min(1f)]
        private float _maxFuelSeconds = 300f;

        [SerializeField]
        [Min(0f)]
        private float _fuelConsumptionPerSecond = 1f;

        [SerializeField]
        [Min(0.1f)]
        private float _protectionRadius = 8f;

        [SerializeField]
        [Min(0f)]
        private float _minLightIntensity = 0.35f;

        [SerializeField]
        [Min(0f)]
        private float _maxLightIntensity = 1.5f;

        /// <summary>
        /// Gets the starting fuel in seconds.
        /// </summary>
        public float StartFuelSeconds => _startFuelSeconds;

        /// <summary>
        /// Gets the maximum fuel capacity in seconds.
        /// </summary>
        public float MaxFuelSeconds => _maxFuelSeconds;

        /// <summary>
        /// Gets how much fuel is consumed every second.
        /// </summary>
        public float FuelConsumptionPerSecond => _fuelConsumptionPerSecond;

        /// <summary>
        /// Gets the campfire protection radius in world units.
        /// </summary>
        public float ProtectionRadius => _protectionRadius;

        /// <summary>
        /// Gets the minimum light intensity while the campfire is active.
        /// </summary>
        public float MinLightIntensity => _minLightIntensity;

        /// <summary>
        /// Gets the maximum light intensity while the campfire is active.
        /// </summary>
        public float MaxLightIntensity => _maxLightIntensity;
    }
}
