// Path: Assets/Project/Scpripts/Campfire/ICampfireService.cs
// Purpose: Exposes the runtime campfire state and supported gameplay actions.
// Dependencies: UnityEngine, CampfireState.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Provides access to the active runtime campfire.
    /// </summary>
    public interface ICampfireService
    {
        /// <summary>
        /// Gets the shared runtime state asset.
        /// </summary>
        CampfireState State { get; }

        /// <summary>
        /// Gets whether the campfire is currently lit.
        /// </summary>
        bool IsLit { get; }

        /// <summary>
        /// Gets whether the campfire is currently in the dying state.
        /// </summary>
        bool IsDying { get; }

        /// <summary>
        /// Gets the current campfire upgrade level.
        /// </summary>
        CampfireLevel Level { get; }

        /// <summary>
        /// Gets the world position of the campfire.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the current active protection radius.
        /// </summary>
        float ProtectionRadius { get; }

        /// <summary>
        /// Gets the current active light radius.
        /// </summary>
        float LightRadius { get; }

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        float CurrentFuel { get; }

        /// <summary>
        /// Gets the current maximum fuel capacity.
        /// </summary>
        float MaxFuel { get; }

        /// <summary>
        /// Adds fuel to the campfire.
        /// </summary>
        /// <param name="amount">Fuel amount to add.</param>
        void AddFuel(float amount);

        /// <summary>
        /// Attempts to ignite the campfire.
        /// </summary>
        /// <returns>True when ignition succeeded.</returns>
        bool Ignite();

        /// <summary>
        /// Extinguishes the campfire immediately.
        /// </summary>
        void Extinguish();

        /// <summary>
        /// Attempts to upgrade the campfire to the next tier.
        /// </summary>
        /// <returns>True when the upgrade succeeded.</returns>
        bool TryUpgrade();
    }
}
