// Path: Assets/Project/Scpripts/TreeDrops/ItemData.cs
// Purpose: Defines the ScriptableObject data used by tree-drop pickups and the inventory.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.TreeDrops
{
    /// <summary>
    /// Authoring asset describing a collectible inventory item.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "Project Resonance/Tree Drops/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField]
        private string _itemName;

        [SerializeField]
        private Sprite _itemSprite;

        [SerializeField]
        [Min(1)]
        private int _quantity = 1;

        /// <summary>
        /// Gets the display name shown for this item.
        /// </summary>
        public string ItemName => string.IsNullOrWhiteSpace(_itemName) ? name : _itemName;

        /// <summary>
        /// Gets the sprite used by UI to represent this item.
        /// </summary>
        public Sprite ItemSprite => _itemSprite;

        /// <summary>
        /// Gets the default quantity granted by one pickup.
        /// </summary>
        public int Quantity => Mathf.Max(1, _quantity);

        private void OnValidate()
        {
            _quantity = Mathf.Max(1, _quantity);
        }
    }
}
