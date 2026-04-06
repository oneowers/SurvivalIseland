// Path: Assets/Project/Scpripts/Common/Messages/CampfireStateChangedMessage.cs
// Purpose: Broadcasts the current campfire state and fuel values.
// Dependencies: None.

namespace ProjectResonance.Common.Messages
{
    /// <summary>
    /// Carries the latest campfire runtime state.
    /// </summary>
    public readonly struct CampfireStateChangedMessage
    {
        /// <summary>
        /// Creates a new campfire state message.
        /// </summary>
        /// <param name="isLit">Whether the campfire is currently lit.</param>
        /// <param name="fuelSeconds">Remaining fuel in seconds.</param>
        /// <param name="maxFuelSeconds">Maximum fuel capacity in seconds.</param>
        /// <param name="protectionRadius">Active protection radius around the campfire.</param>
        public CampfireStateChangedMessage(bool isLit, float fuelSeconds, float maxFuelSeconds, float protectionRadius)
        {
            IsLit = isLit;
            FuelSeconds = fuelSeconds;
            MaxFuelSeconds = maxFuelSeconds;
            ProtectionRadius = protectionRadius;
        }

        /// <summary>
        /// Gets whether the campfire is currently lit.
        /// </summary>
        public bool IsLit { get; }

        /// <summary>
        /// Gets the remaining fuel in seconds.
        /// </summary>
        public float FuelSeconds { get; }

        /// <summary>
        /// Gets the maximum fuel capacity in seconds.
        /// </summary>
        public float MaxFuelSeconds { get; }

        /// <summary>
        /// Gets the active protection radius in world units.
        /// </summary>
        public float ProtectionRadius { get; }
    }
}
