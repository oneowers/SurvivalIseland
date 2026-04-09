// Path: Assets/Project/Scpripts/DayNight/TimeOfDayEventsSystem.cs
// Purpose: Translates time-of-day phase changes into authored ambient gameplay events.
// Dependencies: VContainer, TimeOfDayChangedEvent.

using System;
using VContainer.Unity;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Event emitted when dawn ambience should start.
    /// </summary>
    public readonly struct BirdsStartSingingEvent
    {
    }

    /// <summary>
    /// Event emitted when bird ambience should stop.
    /// </summary>
    public readonly struct BirdsStopSingingEvent
    {
    }

    /// <summary>
    /// Event emitted when ghosts should become active for the night.
    /// </summary>
    public readonly struct GhostsActivateEvent
    {
    }

    /// <summary>
    /// Event emitted when the lord wraith should be requested before dawn.
    /// </summary>
    public readonly struct LordWraithSpawnRequestEvent
    {
    }

    /// <summary>
    /// Event emitted when ghosts should leave the scene at daybreak.
    /// </summary>
    public readonly struct GhostsDeactivateEvent
    {
    }

    /// <summary>
    /// Runtime translator from time-of-day phases to authored gameplay events.
    /// </summary>
    public sealed class TimeOfDayEventsSystem : IStartable, IDisposable
    {
        private readonly DayNightSystem _dayNightSystem;

        /// <summary>
        /// Creates the runtime time-of-day events system.
        /// </summary>
        public TimeOfDayEventsSystem(DayNightSystem dayNightSystem)
        {
            _dayNightSystem = dayNightSystem;
        }

        /// <summary>
        /// Raised when dawn ambience should start.
        /// </summary>
        public event Action<BirdsStartSingingEvent> BirdsStartedSinging;

        /// <summary>
        /// Raised when bird ambience should stop.
        /// </summary>
        public event Action<BirdsStopSingingEvent> BirdsStoppedSinging;

        /// <summary>
        /// Raised when ghosts should activate.
        /// </summary>
        public event Action<GhostsActivateEvent> GhostsActivated;

        /// <summary>
        /// Raised when the Lord Wraith should spawn.
        /// </summary>
        public event Action<LordWraithSpawnRequestEvent> LordWraithSpawnRequested;

        /// <summary>
        /// Raised when ghosts should deactivate.
        /// </summary>
        public event Action<GhostsDeactivateEvent> GhostsDeactivated;

        /// <summary>
        /// Starts the phase subscription.
        /// </summary>
        public void Start()
        {
            if (_dayNightSystem != null)
            {
                _dayNightSystem.TimeOfDayChanged += OnTimeOfDayChanged;
            }
        }

        /// <summary>
        /// Stops the phase subscription.
        /// </summary>
        public void Dispose()
        {
            if (_dayNightSystem != null)
            {
                _dayNightSystem.TimeOfDayChanged -= OnTimeOfDayChanged;
            }
        }

        private void OnTimeOfDayChanged(TimeOfDayChangedEvent message)
        {
            switch (message.CurrentTimeOfDay)
            {
                case TimeOfDay.Dawn:
                    GhostsDeactivated?.Invoke(new GhostsDeactivateEvent());
                    BirdsStartedSinging?.Invoke(new BirdsStartSingingEvent());
                    break;
                case TimeOfDay.Morning:
                    // Sleeping skips directly to morning, so ghosts still need an explicit teardown event.
                    if (message.PreviousTimeOfDay == TimeOfDay.Night || message.PreviousTimeOfDay == TimeOfDay.PreDawn)
                    {
                        GhostsDeactivated?.Invoke(new GhostsDeactivateEvent());
                    }

                    break;
                case TimeOfDay.Sunset:
                    BirdsStoppedSinging?.Invoke(new BirdsStopSingingEvent());
                    break;
                case TimeOfDay.Night:
                    GhostsActivated?.Invoke(new GhostsActivateEvent());
                    break;
                case TimeOfDay.PreDawn:
                    LordWraithSpawnRequested?.Invoke(new LordWraithSpawnRequestEvent());
                    break;
            }
        }
    }
}
