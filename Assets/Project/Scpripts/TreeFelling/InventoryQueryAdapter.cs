// Path: Assets/Project/Scpripts/TreeFelling/InventoryQueryAdapter.cs
// Purpose: Provides a simple editor-driven inventory query adapter for equipped axe state.
// Dependencies: UnityEngine, TreeFelling.

using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Inspector-configurable adapter that exposes the currently equipped axe tier.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Inventory Query Adapter")]
    [DisallowMultipleComponent]
    public sealed class InventoryQueryAdapter : MonoBehaviour, IInventoryQuery
    {
        [SerializeField]
        private AxeTier _equippedAxeTier = AxeTier.Stone;

        /// <summary>
        /// Returns the currently equipped axe tier.
        /// </summary>
        /// <returns>Configured axe tier.</returns>
        public AxeTier GetEquippedAxeTier()
        {
            return _equippedAxeTier;
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
