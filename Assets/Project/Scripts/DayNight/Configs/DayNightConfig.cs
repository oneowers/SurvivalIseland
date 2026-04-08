// Path: Assets/Project/Scpripts/DayNight/Configs/DayNightConfig.cs
// Purpose: Stores the authored phases and lighting curves for the global day/night cycle.
// Dependencies: UnityEngine, ProjectResonance.DayNight.

using System;
using UnityEngine;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Authoring data for a single time-of-day phase boundary.
    /// </summary>
    [Serializable]
    public struct TimeOfDayPhase
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float _startNormalized;

        [SerializeField]
        private TimeOfDay _label;

        /// <summary>
        /// Creates a new authored phase boundary.
        /// </summary>
        /// <param name="startNormalized">Normalized phase start.</param>
        /// <param name="label">Phase label.</param>
        public TimeOfDayPhase(float startNormalized, TimeOfDay label)
        {
            _startNormalized = startNormalized;
            _label = label;
        }

        /// <summary>
        /// Gets the normalized time where the phase begins.
        /// </summary>
        public float StartNormalized => _startNormalized;

        /// <summary>
        /// Gets the authored phase label.
        /// </summary>
        public TimeOfDay Label => _label;
    }

    /// <summary>
    /// ScriptableObject with runtime clock and lighting curves.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "Project Resonance/DayNight/Day Night Config")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1f)]
        private float _gameDayDuration = 1200f;

        [SerializeField]
        private TimeOfDayPhase[] _phases =
        {
            CreatePhase(0f, TimeOfDay.Dawn),
            CreatePhase(0.08333334f, TimeOfDay.Morning),
            CreatePhase(0.25f, TimeOfDay.Noon),
            CreatePhase(0.33333334f, TimeOfDay.Afternoon),
            CreatePhase(0.5f, TimeOfDay.Sunset),
            CreatePhase(0.58333334f, TimeOfDay.Dusk),
            CreatePhase(0.625f, TimeOfDay.Night),
            CreatePhase(0.875f, TimeOfDay.PreDawn),
        };

        [SerializeField]
        private AnimationCurve _sunIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.08333334f, 0.65f),
            new Keyframe(0.25f, 1.25f),
            new Keyframe(0.5f, 0.55f),
            new Keyframe(0.58333334f, 0.18f),
            new Keyframe(0.625f, 0.03f),
            new Keyframe(0.875f, 0.02f),
            new Keyframe(1f, 0.25f));

        [SerializeField]
        private AnimationCurve _sunColorTemperatureCurve = new AnimationCurve(
            new Keyframe(0f, 4500f),
            new Keyframe(0.08333334f, 5600f),
            new Keyframe(0.25f, 6500f),
            new Keyframe(0.5f, 3200f),
            new Keyframe(0.58333334f, 2200f),
            new Keyframe(0.625f, 1800f),
            new Keyframe(0.875f, 2000f),
            new Keyframe(1f, 4500f));

        [SerializeField]
        private AnimationCurve _ambientIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.08333334f, 0.45f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.35f),
            new Keyframe(0.58333334f, 0.08f),
            new Keyframe(0.625f, 0.02f),
            new Keyframe(0.875f, 0.02f),
            new Keyframe(1f, 0.2f));

        /// <summary>
        /// Gets the real-time duration of a full in-game day in seconds.
        /// </summary>
        public float GameDayDuration => _gameDayDuration;

        /// <summary>
        /// Gets the authored phase boundaries.
        /// </summary>
        public TimeOfDayPhase[] Phases => _phases;

        /// <summary>
        /// Gets the sun intensity curve.
        /// </summary>
        public AnimationCurve SunIntensityCurve => _sunIntensityCurve;

        /// <summary>
        /// Gets the sun color-temperature curve in Kelvin.
        /// </summary>
        public AnimationCurve SunColorTemperatureCurve => _sunColorTemperatureCurve;

        /// <summary>
        /// Gets the ambient intensity curve.
        /// </summary>
        public AnimationCurve AmbientIntensityCurve => _ambientIntensityCurve;

        private void OnValidate()
        {
            if (_phases == null || _phases.Length == 0)
            {
                _phases = new[]
                {
                    CreatePhase(0f, TimeOfDay.Dawn),
                    CreatePhase(0.08333334f, TimeOfDay.Morning),
                    CreatePhase(0.25f, TimeOfDay.Noon),
                    CreatePhase(0.33333334f, TimeOfDay.Afternoon),
                    CreatePhase(0.5f, TimeOfDay.Sunset),
                    CreatePhase(0.58333334f, TimeOfDay.Dusk),
                    CreatePhase(0.625f, TimeOfDay.Night),
                    CreatePhase(0.875f, TimeOfDay.PreDawn),
                };
            }

            Array.Sort(_phases, (left, right) => left.StartNormalized.CompareTo(right.StartNormalized));
        }

        private static TimeOfDayPhase CreatePhase(float startNormalized, TimeOfDay label)
        {
            return new TimeOfDayPhase(startNormalized, label);
        }
    }
}
