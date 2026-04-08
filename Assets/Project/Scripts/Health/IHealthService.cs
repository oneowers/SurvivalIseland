// Path: Assets/Project/Scpripts/Health/IHealthService.cs
// Purpose: Exposes runtime player health operations and state.
// Dependencies: None.

namespace ProjectResonance.Health
{
    /// <summary>
    /// Provides access to runtime player health.
    /// </summary>
    public interface IHealthService
    {
        /// <summary>
        /// Gets the current player health.
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Gets the maximum player health.
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Gets whether the player is still alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Applies incoming damage to the player.
        /// </summary>
        /// <param name="amount">Damage amount to subtract from health.</param>
        void ApplyDamage(float amount);

        /// <summary>
        /// Applies incoming healing to the player.
        /// </summary>
        /// <param name="amount">Healing amount to add to health.</param>
        void ApplyHealing(float amount);
    }
}
