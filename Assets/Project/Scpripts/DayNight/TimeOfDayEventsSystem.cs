// Path: Assets/Project/Scpripts/DayNight/TimeOfDayEventsSystem.cs
// Purpose: Translates time-of-day phase changes into authored ambient gameplay events.
// Dependencies: MessagePipe, VContainer, TimeOfDayChangedEvent.

using System;
using MessagePipe;
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
        private readonly ISubscriber<TimeOfDayChangedEvent> _timeOfDayChangedSubscriber;
        private readonly IPublisher<BirdsStartSingingEvent> _birdsStartSingingPublisher;
        private readonly IPublisher<BirdsStopSingingEvent> _birdsStopSingingPublisher;
        private readonly IPublisher<SunsetWarningEvent> _sunsetWarningPublisher;
        private readonly IPublisher<GhostsActivateEvent> _ghostsActivatePublisher;
        private readonly IPublisher<LordWraithSpawnRequestEvent> _lordWraithSpawnRequestPublisher;
        private readonly IPublisher<GhostsDeactivateEvent> _ghostsDeactivatePublisher;

        private IDisposable _timeOfDaySubscription;

        /// <summary>
        /// Creates the runtime time-of-day events system.
        /// </summary>
        /// <param name="timeOfDayChangedSubscriber">Time-of-day phase subscriber.</param>
        /// <param name="birdsStartSingingPublisher">Birds-start publisher.</param>
        /// <param name="birdsStopSingingPublisher">Birds-stop publisher.</param>
        /// <param name="sunsetWarningPublisher">Sunset warning publisher.</param>
        /// <param name="ghostsActivatePublisher">Ghost-activation publisher.</param>
        /// <param name="lordWraithSpawnRequestPublisher">Lord-wraith request publisher.</param>
        /// <param name="ghostsDeactivatePublisher">Ghost-deactivation publisher.</param>
        public TimeOfDayEventsSystem(
            ISubscriber<TimeOfDayChangedEvent> timeOfDayChangedSubscriber,
            IPublisher<BirdsStartSingingEvent> birdsStartSingingPublisher,
            IPublisher<BirdsStopSingingEvent> birdsStopSingingPublisher,
            IPublisher<SunsetWarningEvent> sunsetWarningPublisher,
            IPublisher<GhostsActivateEvent> ghostsActivatePublisher,
            IPublisher<LordWraithSpawnRequestEvent> lordWraithSpawnRequestPublisher,
            IPublisher<GhostsDeactivateEvent> ghostsDeactivatePublisher)
        {
            _timeOfDayChangedSubscriber = timeOfDayChangedSubscriber;
            _birdsStartSingingPublisher = birdsStartSingingPublisher;
            _birdsStopSingingPublisher = birdsStopSingingPublisher;
            _sunsetWarningPublisher = sunsetWarningPublisher;
            _ghostsActivatePublisher = ghostsActivatePublisher;
            _lordWraithSpawnRequestPublisher = lordWraithSpawnRequestPublisher;
            _ghostsDeactivatePublisher = ghostsDeactivatePublisher;
        }

        /// <summary>
        /// Starts the phase subscription.
        /// </summary>
        public void Start()
        {
            _timeOfDaySubscription = _timeOfDayChangedSubscriber.Subscribe(OnTimeOfDayChanged);
        }

        /// <summary>
        /// Stops the phase subscription.
        /// </summary>
        public void Dispose()
        {
            _timeOfDaySubscription?.Dispose();
        }

        private void OnTimeOfDayChanged(TimeOfDayChangedEvent message)
        {
            switch (message.CurrentTimeOfDay)
            {
                case TimeOfDay.Dawn:
                    _ghostsDeactivatePublisher.Publish(new GhostsDeactivateEvent());
                    _birdsStartSingingPublisher.Publish(new BirdsStartSingingEvent());
                    break;
                case TimeOfDay.Morning:
                    // Sleeping skips directly to morning, so ghosts still need an explicit teardown event.
                    if (message.PreviousTimeOfDay == TimeOfDay.Night || message.PreviousTimeOfDay == TimeOfDay.PreDawn)
                    {
                        _ghostsDeactivatePublisher.Publish(new GhostsDeactivateEvent());
                    }

                    break;
                case TimeOfDay.Sunset:
                    _birdsStopSingingPublisher.Publish(new BirdsStopSingingEvent());
                    _sunsetWarningPublisher.Publish(new SunsetWarningEvent(message.CurrentTimeNormalized, 4f * 60f));
                    break;
                case TimeOfDay.Night:
                    _ghostsActivatePublisher.Publish(new GhostsActivateEvent());
                    break;
                case TimeOfDay.PreDawn:
                    _lordWraithSpawnRequestPublisher.Publish(new LordWraithSpawnRequestEvent());
                    break;
            }
        }
    }
}
