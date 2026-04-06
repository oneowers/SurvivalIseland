// Path: Assets/Project/Scpripts/Common/Messages/DayPhaseChangedMessage.cs
// Purpose: Broadcasts a change in the global day and night state.
// Dependencies: ProjectResonance.Common.Messages.DayPhase.

namespace ProjectResonance.Common.Messages
{
    /// <summary>
    /// Carries the current and previous day phase values.
    /// </summary>
    public readonly struct DayPhaseChangedMessage
    {
        /// <summary>
        /// Creates a new day phase change message.
        /// </summary>
        /// <param name="previousPhase">Phase before the change.</param>
        /// <param name="currentPhase">Phase after the change.</param>
        /// <param name="normalizedTime">Current normalized cycle time.</param>
        public DayPhaseChangedMessage(DayPhase previousPhase, DayPhase currentPhase, float normalizedTime)
        {
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
            NormalizedTime = normalizedTime;
        }

        /// <summary>
        /// Gets the phase before the change.
        /// </summary>
        public DayPhase PreviousPhase { get; }

        /// <summary>
        /// Gets the phase after the change.
        /// </summary>
        public DayPhase CurrentPhase { get; }

        /// <summary>
        /// Gets the normalized cycle time in the range [0..1].
        /// </summary>
        public float NormalizedTime { get; }
    }
}
