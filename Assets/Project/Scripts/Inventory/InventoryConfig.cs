// Path: Assets/Project/Scripts/Inventory/InventoryConfig.cs
// Purpose: Defines the shared runtime inventory capacities and pool sizing used across gameplay and UI.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Shared authoring config for the runtime inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Project Resonance/Inventory/Inventory Config")]
    public sealed class InventoryConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1)]
        private int _maxSlots = 10;

        [SerializeField]
        [Min(1)]
        private int _heldVisualPoolCapacityPerItem = 2;

        [SerializeField]
        [Min(1)]
        private int _craftedPreviewPoolCapacityPerItem = 2;

        [SerializeField]
        [Min(1)]
        private int _maxPooledVisualsPerItem = 20;

        /// <summary>
        /// Gets the maximum number of runtime inventory slots.
        /// </summary>
        public int MaxSlots => Mathf.Max(1, _maxSlots);

        /// <summary>
        /// Gets the default held-item pool size created per unique item definition.
        /// </summary>
        public int HeldVisualPoolCapacityPerItem => Mathf.Max(1, _heldVisualPoolCapacityPerItem);

        /// <summary>
        /// Gets the default craft-preview pool size created per unique item definition.
        /// </summary>
        public int CraftedPreviewPoolCapacityPerItem => Mathf.Max(1, _craftedPreviewPoolCapacityPerItem);

        /// <summary>
        /// Gets the maximum pooled visual instances allowed per unique item definition.
        /// </summary>
        public int MaxPooledVisualsPerItem => Mathf.Max(1, _maxPooledVisualsPerItem);

        private void OnValidate()
        {
            _maxSlots = Mathf.Max(1, _maxSlots);
            _heldVisualPoolCapacityPerItem = Mathf.Max(1, _heldVisualPoolCapacityPerItem);
            _craftedPreviewPoolCapacityPerItem = Mathf.Max(1, _craftedPreviewPoolCapacityPerItem);
            _maxPooledVisualsPerItem = Mathf.Max(1, _maxPooledVisualsPerItem);
        }
    }
}
