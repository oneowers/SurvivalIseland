// Path: Assets/Project/Scpripts/DayNight/IDayNightService.cs
// Purpose: Exposes the current global day and night state.
// Dependencies: ProjectResonance.Common.Messages.

using ProjectResonance.Common.Messages;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Provides access to the runtime day and night state.
    /// </summary>
    public interface IDayNightService
    {
        /// <summary>
        /// Gets the current normalized cycle time in the range [0..1].
        /// </summary>
        float NormalizedTime { get; }

        /// <summary>
        /// Gets the current global day phase.
        /// </summary>
        DayPhase CurrentPhase { get; }
    }
}
