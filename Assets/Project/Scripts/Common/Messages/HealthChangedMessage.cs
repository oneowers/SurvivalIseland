// Path: Assets/Project/Scpripts/Common/Messages/HealthChangedMessage.cs
// Purpose: Broadcasts the player's current health state.
// Dependencies: None.

namespace ProjectResonance.Common.Messages
{
    /// <summary>
    /// Carries the player's health state after a mutation.
    /// </summary>
    public readonly struct HealthChangedMessage
    {
        /// <summary>
        /// Creates a new health changed message.
        /// </summary>
        /// <param name="currentHealth">Current health after the change.</param>
        /// <param name="maxHealth">Maximum available health.</param>
        /// <param name="delta">Signed health delta that produced the new state.</param>
        public HealthChangedMessage(float currentHealth, float maxHealth, float delta)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Delta = delta;
        }

        /// <summary>
        /// Gets the current health value.
        /// </summary>
        public float CurrentHealth { get; }

        /// <summary>
        /// Gets the maximum health value.
        /// </summary>
        public float MaxHealth { get; }

        /// <summary>
        /// Gets the signed delta applied to health.
        /// </summary>
        public float Delta { get; }
    }
}
