// Path: Assets/Project/Scpripts/DayNight/IDayNightService.cs
// Purpose: Exposes the runtime day and night clock for gameplay systems.
// Dependencies: ProjectResonance.DayNight.

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Provides access to the runtime day and night clock state.
    /// </summary>
    public interface IDayNightService
    {
        /// <summary>
        /// Gets the current normalized cycle time in the range [0..1].
        /// </summary>
        float CurrentTimeNormalized { get; }

        /// <summary>
        /// Gets the configured real-time duration of one in-game day.
        /// </summary>
        float GameDayDuration { get; }

        /// <summary>
        /// Gets the current time-of-day phase.
        /// </summary>
        TimeOfDay CurrentTimeOfDay { get; }

        /// <summary>
        /// Skips the clock directly to the next morning.
        /// </summary>
        void SkipToMorning();
    }
}
