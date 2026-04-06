// Path: Assets/Project/Scpripts/DayNight/Configs/DayNightCycleConfig.cs
// Purpose: Stores tunable settings for the global day and night cycle.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// ScriptableObject with global day and night cycle settings.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightCycleConfig", menuName = "Project Resonance/DayNight/Day Night Cycle Config")]
    public sealed class DayNightCycleConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1f)]
        private float _fullCycleDurationSeconds = 600f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _initialNormalizedTime = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _nightStartsAtNormalizedTime = 0.75f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _dayStartsAtNormalizedTime = 0.25f;

        /// <summary>
        /// Gets the full day and night cycle duration in seconds.
        /// </summary>
        public float FullCycleDurationSeconds => _fullCycleDurationSeconds;

        /// <summary>
        /// Gets the initial normalized cycle time.
        /// </summary>
        public float InitialNormalizedTime => _initialNormalizedTime;

        /// <summary>
        /// Gets the normalized time at which night starts.
        /// </summary>
        public float NightStartsAtNormalizedTime => _nightStartsAtNormalizedTime;

        /// <summary>
        /// Gets the normalized time at which day starts.
        /// </summary>
        public float DayStartsAtNormalizedTime => _dayStartsAtNormalizedTime;
    }
}
