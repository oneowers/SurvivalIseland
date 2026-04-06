// Path: Assets/Project/Scpripts/DayNight/DayNightCycleSystem.cs
// Purpose: Advances the global day and night cycle and publishes phase changes.
// Dependencies: DayNightCycleConfig, MessagePipe, UnityEngine, VContainer.

using MessagePipe;
using ProjectResonance.Common.Messages;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Owns the global day and night state.
    /// </summary>
    public sealed class DayNightCycleSystem : IDayNightService, IStartable, ITickable
    {
        private readonly DayNightCycleConfig _config;
        private readonly IBufferedPublisher<DayPhaseChangedMessage> _phaseChangedPublisher;

        private float _normalizedTime;
        private DayPhase _currentPhase;

        /// <summary>
        /// Initializes the day and night system.
        /// </summary>
        /// <param name="config">Cycle configuration.</param>
        /// <param name="phaseChangedPublisher">Buffered phase publisher.</param>
        public DayNightCycleSystem(
            DayNightCycleConfig config,
            IBufferedPublisher<DayPhaseChangedMessage> phaseChangedPublisher)
        {
            _config = config;
            _phaseChangedPublisher = phaseChangedPublisher;
            _normalizedTime = Mathf.Repeat(config.InitialNormalizedTime, 1f);
            _currentPhase = EvaluatePhase(_normalizedTime);
        }

        /// <summary>
        /// Gets the current normalized cycle time in the range [0..1].
        /// </summary>
        public float NormalizedTime => _normalizedTime;

        /// <summary>
        /// Gets the current global day phase.
        /// </summary>
        public DayPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Publishes the initial cycle state when the container starts.
        /// </summary>
        public void Start()
        {
            _phaseChangedPublisher.Publish(new DayPhaseChangedMessage(_currentPhase, _currentPhase, _normalizedTime));
        }

        /// <summary>
        /// Advances the cycle during the Unity player loop.
        /// </summary>
        public void Tick()
        {
            if (_config.FullCycleDurationSeconds <= 0f)
            {
                return;
            }

            _normalizedTime = Mathf.Repeat(_normalizedTime + (Time.deltaTime / _config.FullCycleDurationSeconds), 1f);

            var nextPhase = EvaluatePhase(_normalizedTime);
            if (nextPhase == _currentPhase)
            {
                return;
            }

            var previousPhase = _currentPhase;
            _currentPhase = nextPhase;
            _phaseChangedPublisher.Publish(new DayPhaseChangedMessage(previousPhase, _currentPhase, _normalizedTime));
        }

        private DayPhase EvaluatePhase(float normalizedTime)
        {
            // The cycle wraps across 1.0, so night can span the end and start of the range.
            var isNight = normalizedTime >= _config.NightStartsAtNormalizedTime ||
                          normalizedTime < _config.DayStartsAtNormalizedTime;

            return isNight ? DayPhase.Night : DayPhase.Day;
        }
    }
}
