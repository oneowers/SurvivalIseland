// Path: Assets/Project/Scpripts/TreeFelling/InventoryQueryAdapter.cs
// Purpose: Provides a simple editor-driven inventory query adapter for axe state and campfire ignition items.
// Dependencies: UnityEngine, TreeFelling, Campfire.

using ProjectResonance.Campfire;
using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Inspector-configurable adapter that exposes the currently equipped axe tier.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Inventory Query Adapter")]
    [DisallowMultipleComponent]
    public sealed class InventoryQueryAdapter : MonoBehaviour, IInventoryQuery, ICampfireInventoryQuery
    {
        [SerializeField]
        private AxeTier _equippedAxeTier = AxeTier.Stone;

        [SerializeField]
        private bool _hasFlint = true;

        [SerializeField]
        private bool _hasFiresteel;

        /// <summary>
        /// Returns the currently equipped axe tier.
        /// </summary>
        /// <returns>Configured axe tier.</returns>
        public AxeTier GetEquippedAxeTier()
        {
            return _equippedAxeTier;
        }

        /// <summary>
        /// Returns whether the inventory currently contains flint.
        /// </summary>
        /// <returns>True when flint is available.</returns>
        public bool HasFlint()
        {
            return _hasFlint;
        }

        /// <summary>
        /// Returns whether the inventory currently contains firesteel.
        /// </summary>
        /// <returns>True when firesteel is available.</returns>
        public bool HasFiresteel()
        {
            return _hasFiresteel;
        }

        /// <summary>
        /// Returns whether any ignition source is available.
        /// </summary>
        /// <returns>True when the player can ignite the campfire.</returns>
        public bool HasIgnitionSource()
        {
            return _hasFlint || _hasFiresteel;
        }

        /// <summary>
        /// Sets the equipped axe tier at runtime.
        /// </summary>
        /// <param name="axeTier">New equipped axe tier.</param>
        public void SetEquippedAxeTier(AxeTier axeTier)
        {
            _equippedAxeTier = axeTier;
        }
    }
}
