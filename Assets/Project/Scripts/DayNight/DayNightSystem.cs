// Path: Assets/Project/Scpripts/DayNight/DayNightSystem.cs
// Purpose: Owns the 20-minute global day cycle, exposes time events, and supports sleeping to morning.
// Dependencies: UniTask, VContainer, DayNightConfig.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Supported global time-of-day phases.
    /// </summary>
    public enum TimeOfDay
    {
        /// <summary>
        /// Early sunrise transition.
        /// </summary>
        Dawn = 0,

        /// <summary>
        /// Stable daylight morning window.
        /// </summary>
        Morning = 1,

        /// <summary>
        /// Highest daylight phase.
        /// </summary>
        Noon = 2,

        /// <summary>
        /// Late-day bright phase.
        /// </summary>
        Afternoon = 3,

        /// <summary>
        /// Orange sunset transition.
        /// </summary>
        Sunset = 4,

        /// <summary>
        /// Post-sunset blue hour.
        /// </summary>
        Dusk = 5,

        /// <summary>
        /// Full darkness.
        /// </summary>
        Night = 6,

        /// <summary>
        /// Final dark phase before dawn.
        /// </summary>
        PreDawn = 7,
    }

    /// <summary>
    /// Periodic clock event emitted every ten in-game seconds.
    /// </summary>
    public readonly struct TimeTickEvent
    {
        /// <summary>
        /// Creates a new clock tick event.
        /// </summary>
        /// <param name="currentTimeNormalized">Current normalized time.</param>
        /// <param name="currentGameSeconds">Current in-game seconds since dawn.</param>
        /// <param name="currentTimeOfDay">Current time-of-day phase.</param>
        public TimeTickEvent(float currentTimeNormalized, float currentGameSeconds, TimeOfDay currentTimeOfDay)
        {
            CurrentTimeNormalized = currentTimeNormalized;
            CurrentGameSeconds = currentGameSeconds;
            CurrentTimeOfDay = currentTimeOfDay;
        }

        /// <summary>
        /// Gets the current normalized time.
        /// </summary>
        public float CurrentTimeNormalized { get; }

        /// <summary>
        /// Gets the current in-game seconds since dawn.
        /// </summary>
        public float CurrentGameSeconds { get; }

        /// <summary>
        /// Gets the current time-of-day phase.
        /// </summary>
        public TimeOfDay CurrentTimeOfDay { get; }
    }

    /// <summary>
    /// Event emitted whenever the authored time-of-day phase changes.
    /// </summary>
    public readonly struct TimeOfDayChangedEvent
    {
        /// <summary>
        /// Creates a new phase-change event.
        /// </summary>
        /// <param name="previousTimeOfDay">Previous phase.</param>
        /// <param name="currentTimeOfDay">Current phase.</param>
        /// <param name="currentTimeNormalized">Current normalized time.</param>
        public TimeOfDayChangedEvent(TimeOfDay previousTimeOfDay, TimeOfDay currentTimeOfDay, float currentTimeNormalized)
        {
            PreviousTimeOfDay = previousTimeOfDay;
            CurrentTimeOfDay = currentTimeOfDay;
            CurrentTimeNormalized = currentTimeNormalized;
        }

        /// <summary>
        /// Gets the previous phase.
        /// </summary>
        public TimeOfDay PreviousTimeOfDay { get; }

        /// <summary>
        /// Gets the current phase.
        /// </summary>
        public TimeOfDay CurrentTimeOfDay { get; }

        /// <summary>
        /// Gets the current normalized time.
        /// </summary>
        public float CurrentTimeNormalized { get; }
    }

    /// <summary>
    /// Event emitted when night is approaching.
    /// </summary>
    public readonly struct SunsetWarningEvent
    {
        /// <summary>
        /// Creates a new sunset warning event.
        /// </summary>
        /// <param name="currentTimeNormalized">Current normalized time.</param>
        /// <param name="remainingGameSecondsUntilNight">Remaining in-game seconds until night.</param>
        public SunsetWarningEvent(float currentTimeNormalized, float remainingGameSecondsUntilNight)
        {
            CurrentTimeNormalized = currentTimeNormalized;
            RemainingGameSecondsUntilNight = remainingGameSecondsUntilNight;
        }

        /// <summary>
        /// Gets the current normalized time.
        /// </summary>
        public float CurrentTimeNormalized { get; }

        /// <summary>
        /// Gets the remaining in-game seconds until the Night phase starts.
        /// </summary>
        public float RemainingGameSecondsUntilNight { get; }
    }

    /// <summary>
    /// Runtime day/night owner.
    /// </summary>
    public sealed class DayNightSystem : IDayNightService, IStartable, IDisposable
    {
        private const float FullGameDaySeconds = 24f * 60f * 60f;
        private const float TickIntervalGameSeconds = 10f;
        private const float SunsetWarningLeadGameSeconds = 4f * 60f;

        private readonly DayNightConfig _config;

        private readonly TimeOfDayPhase[] _phases;

        private CancellationTokenSource _clockLoopCancellation;
        private float _currentGameSeconds;
        private float _nextTickGameSeconds;
        private float _nightStartGameSeconds;
        private float _sunsetWarningGameSeconds;
        private bool _hasPublishedSunsetWarningThisCycle;

        /// <summary>
        /// Creates the runtime day/night system.
        /// </summary>
        /// <param name="config">Authored day/night configuration.</param>
        public DayNightSystem(DayNightConfig config)
        {
            _config = config;
            _phases = CloneAndSortPhases(config != null ? config.Phases : null);

            _currentGameSeconds = ResolvePhaseStart(TimeOfDay.Dawn) * FullGameDaySeconds;
            CurrentTimeOfDay = EvaluateTimeOfDay(CurrentTimeNormalized);
            _nextTickGameSeconds = ResolveNextTickGameSeconds(_currentGameSeconds);
            _nightStartGameSeconds = ResolvePhaseStart(TimeOfDay.Night) * FullGameDaySeconds;
            _sunsetWarningGameSeconds = Mathf.Repeat(_nightStartGameSeconds - SunsetWarningLeadGameSeconds, FullGameDaySeconds);
        }

        /// <summary>
        /// Gets the current normalized time in the range [0..1].
        /// </summary>
        public float CurrentTimeNormalized => Mathf.Repeat(_currentGameSeconds / FullGameDaySeconds, 1f);

        /// <summary>
        /// Gets the configured real-time duration of a full game day.
        /// </summary>
        public float GameDayDuration => _config != null ? _config.GameDayDuration : 0f;

        /// <summary>
        /// Gets the current phase.
        /// </summary>
        public TimeOfDay CurrentTimeOfDay { get; private set; }

        /// <summary>
        /// Raised on each clock tick snapshot.
        /// </summary>
        public event Action<TimeTickEvent> TimeTicked;

        /// <summary>
        /// Raised whenever the time-of-day phase changes.
        /// </summary>
        public event Action<TimeOfDayChangedEvent> TimeOfDayChanged;

        /// <summary>
        /// Raised when sunset is approaching.
        /// </summary>
        public event Action<SunsetWarningEvent> SunsetWarningRaised;

        /// <summary>
        /// Starts the background clock loop.
        /// </summary>
        public void Start()
        {
            PublishCurrentSnapshot(CurrentTimeOfDay);

            _clockLoopCancellation = new CancellationTokenSource();
            RunClockLoopAsync(_clockLoopCancellation.Token).Forget();
        }

        /// <summary>
        /// Disposes the running clock loop.
        /// </summary>
        public void Dispose()
        {
            if (_clockLoopCancellation == null)
            {
                return;
            }

            _clockLoopCancellation.Cancel();
            _clockLoopCancellation.Dispose();
            _clockLoopCancellation = null;
        }

        /// <summary>
        /// Skips the runtime clock to the Morning phase.
        /// </summary>
        public void SkipToMorning()
        {
            var previousTimeOfDay = CurrentTimeOfDay;
            _currentGameSeconds = ResolvePhaseStart(TimeOfDay.Morning) * FullGameDaySeconds;
            _nextTickGameSeconds = ResolveNextTickGameSeconds(_currentGameSeconds);
            _hasPublishedSunsetWarningThisCycle = false;

            CurrentTimeOfDay = EvaluateTimeOfDay(CurrentTimeNormalized);

            // Sleep hard-sets the clock so lighting and gameplay immediately re-sample the new morning state.
            PublishCurrentSnapshot(previousTimeOfDay);
        }

        private async UniTaskVoid RunClockLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TickClock(Time.deltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Lifetime scope disposal stops the clock through cancellation.
            }
        }

        private void TickClock(float deltaTime)
        {
            if (deltaTime <= 0f || GameDayDuration <= 0f)
            {
                return;
            }

            var previousGameSeconds = _currentGameSeconds;
            var deltaGameSeconds = deltaTime * (FullGameDaySeconds / GameDayDuration);
            _currentGameSeconds = Mathf.Repeat(_currentGameSeconds + deltaGameSeconds, FullGameDaySeconds);

            if (_currentGameSeconds < previousGameSeconds)
            {
                _hasPublishedSunsetWarningThisCycle = false;
            }

            PublishCrossedTimeTicks(previousGameSeconds, _currentGameSeconds);
            PublishCrossedTimeOfDayChanges(previousGameSeconds, _currentGameSeconds);
            PublishSunsetWarningIfNeeded(previousGameSeconds, _currentGameSeconds);
        }

        private void PublishCrossedTimeTicks(float previousGameSeconds, float currentGameSeconds)
        {
            var guard = 0;

            while (HasClockPointBeenReached(previousGameSeconds, currentGameSeconds, _nextTickGameSeconds))
            {
                var tickNormalized = Mathf.Repeat(_nextTickGameSeconds / FullGameDaySeconds, 1f);
                var tickPhase = EvaluateTimeOfDay(tickNormalized);
                TimeTicked?.Invoke(new TimeTickEvent(tickNormalized, _nextTickGameSeconds, tickPhase));

                _nextTickGameSeconds = Mathf.Repeat(_nextTickGameSeconds + TickIntervalGameSeconds, FullGameDaySeconds);
                guard++;

                if (guard > FullGameDaySeconds / TickIntervalGameSeconds)
                {
                    break;
                }
            }
        }

        private void PublishCrossedTimeOfDayChanges(float previousGameSeconds, float currentGameSeconds)
        {
            for (var index = 0; index < _phases.Length; index++)
            {
                var phase = _phases[index];
                var phaseStartGameSeconds = phase.StartNormalized * FullGameDaySeconds;
                if (!HasClockPointBeenReached(previousGameSeconds, currentGameSeconds, phaseStartGameSeconds))
                {
                    continue;
                }

                var previousTimeOfDay = CurrentTimeOfDay;
                CurrentTimeOfDay = phase.Label;
                TimeOfDayChanged?.Invoke(new TimeOfDayChangedEvent(previousTimeOfDay, CurrentTimeOfDay, phase.StartNormalized));
            }
        }

        private void PublishSunsetWarningIfNeeded(float previousGameSeconds, float currentGameSeconds)
        {
            if (_hasPublishedSunsetWarningThisCycle)
            {
                return;
            }

            if (!HasClockPointBeenReached(previousGameSeconds, currentGameSeconds, _sunsetWarningGameSeconds))
            {
                return;
            }

            _hasPublishedSunsetWarningThisCycle = true;
            SunsetWarningRaised?.Invoke(new SunsetWarningEvent(CurrentTimeNormalized, SunsetWarningLeadGameSeconds));
        }

        private void PublishCurrentSnapshot(TimeOfDay previousTimeOfDay)
        {
            TimeTicked?.Invoke(new TimeTickEvent(CurrentTimeNormalized, _currentGameSeconds, CurrentTimeOfDay));
            TimeOfDayChanged?.Invoke(new TimeOfDayChangedEvent(previousTimeOfDay, CurrentTimeOfDay, CurrentTimeNormalized));
        }

        private TimeOfDay EvaluateTimeOfDay(float normalizedTime)
        {
            if (_phases.Length == 0)
            {
                return TimeOfDay.Dawn;
            }

            for (var index = _phases.Length - 1; index >= 0; index--)
            {
                if (normalizedTime >= _phases[index].StartNormalized)
                {
                    return _phases[index].Label;
                }
            }

            return _phases[_phases.Length - 1].Label;
        }

        private float ResolvePhaseStart(TimeOfDay targetTimeOfDay)
        {
            for (var index = 0; index < _phases.Length; index++)
            {
                if (_phases[index].Label == targetTimeOfDay)
                {
                    return _phases[index].StartNormalized;
                }
            }

            return 0f;
        }

        private static TimeOfDayPhase[] CloneAndSortPhases(TimeOfDayPhase[] phases)
        {
            if (phases == null || phases.Length == 0)
            {
                return Array.Empty<TimeOfDayPhase>();
            }

            var copy = new TimeOfDayPhase[phases.Length];
            Array.Copy(phases, copy, phases.Length);
            Array.Sort(copy, (left, right) => left.StartNormalized.CompareTo(right.StartNormalized));
            return copy;
        }

        private static float ResolveNextTickGameSeconds(float currentGameSeconds)
        {
            var nextTick = (Mathf.Floor(currentGameSeconds / TickIntervalGameSeconds) + 1f) * TickIntervalGameSeconds;
            return Mathf.Repeat(nextTick, FullGameDaySeconds);
        }

        private static bool HasClockPointBeenReached(float previousGameSeconds, float currentGameSeconds, float targetGameSeconds)
        {
            if (Mathf.Approximately(previousGameSeconds, currentGameSeconds))
            {
                return false;
            }

            if (currentGameSeconds > previousGameSeconds)
            {
                return targetGameSeconds > previousGameSeconds && targetGameSeconds <= currentGameSeconds;
            }

            return targetGameSeconds > previousGameSeconds || targetGameSeconds <= currentGameSeconds;
        }
    }
}
