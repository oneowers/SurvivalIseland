// Path: Assets/Project/Scpripts/Campfire/ICampfireService.cs
// Purpose: Exposes runtime campfire state and operations.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Provides access to the runtime campfire state.
    /// </summary>
    public interface ICampfireService
    {
        /// <summary>
        /// Gets whether the campfire is currently lit.
        /// </summary>
        bool IsLit { get; }

        /// <summary>
        /// Gets the world position of the campfire.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the active campfire protection radius in world units.
        /// </summary>
        float ProtectionRadius { get; }

        /// <summary>
        /// Adds fuel to the campfire.
        /// </summary>
        /// <param name="fuelSeconds">Fuel amount in seconds.</param>
        void AddFuel(float fuelSeconds);
    }
}
