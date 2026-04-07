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
        public TimeOfDayEventsSystem(
            ISubscriber<TimeOfDayChangedEvent> timeOfDayChangedSubscriber,
            IPublisher<BirdsStartSingingEvent> birdsStartSingingPublisher,
            IPublisher<BirdsStopSingingEvent> birdsStopSingingPublisher,
            IPublisher<SunsetWarningEvent> sunsetWarningPublisher,
            IPublisher<GhostsActivateEvent> ghostsActivatePublisher,
            IPublisher<LordWraithSpawnRequestEvent> lordWraithSpawnRequestPublisher)
        {
            _timeOfDayChangedSubscriber = timeOfDayChangedSubscriber;
            _birdsStartSingingPublisher = birdsStartSingingPublisher;
            _birdsStopSingingPublisher = birdsStopSingingPublisher;
            _sunsetWarningPublisher = sunsetWarningPublisher;
            _ghostsActivatePublisher = ghostsActivatePublisher;
            _lordWraithSpawnRequestPublisher = lordWraithSpawnRequestPublisher;
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
                    _birdsStartSingingPublisher.Publish(new BirdsStartSingingEvent());
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
