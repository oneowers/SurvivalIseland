// Path: Assets/Project/Scripts/Inventory/ItemDefinition.cs
// Purpose: Defines a reusable runtime item asset base used by inventory, held-item visuals, crafting outputs, and resource-node definitions.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Supported high-level item categories.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// Raw gathering resource.
        /// </summary>
        Resource = 0,

        /// <summary>
        /// Player-usable tool item.
        /// </summary>
        Tool = 1,

        /// <summary>
        /// Construction material item.
        /// </summary>
        BuildingMaterial = 2,
    }

    /// <summary>
    /// Static authoring asset for an item type.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Project Resonance/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField]
        private string _itemId;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        [Min(0f)]
        private float _weight = 1f;

        [SerializeField]
        private bool _isStackable = true;

        [SerializeField]
        [Min(1)]
        private int _maxStackSize = 10;

        [SerializeField]
        private ItemType _itemType = ItemType.Resource;

        [SerializeField]
        private GameObject _worldPrefab;

        /// <summary>
        /// Gets the stable item identifier used in data and saves.
        /// </summary>
        public string ItemId => _itemId;

        /// <summary>
        /// Gets the player-facing display name.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <summary>
        /// Gets the inventory icon sprite.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Gets the item carry weight contribution.
        /// </summary>
        public float Weight => _weight;

        /// <summary>
        /// Gets whether identical items can stack in one slot.
        /// </summary>
        public bool IsStackable => _isStackable;

        /// <summary>
        /// Gets the maximum number of items that fit in one stack.
        /// </summary>
        public int MaxStackSize => _isStackable ? Mathf.Max(1, _maxStackSize) : 1;

        /// <summary>
        /// Gets the high-level item category.
        /// </summary>
        public ItemType ItemType => _itemType;

        /// <summary>
        /// Gets the prefab used for held visuals, drops, and crafted previews.
        /// </summary>
        public GameObject WorldPrefab => _worldPrefab;

        /// <summary>
        /// Safely resolves the prefab used for held visuals, world drops, and crafted previews.
        /// </summary>
        /// <param name="worldPrefab">Resolved prefab when available.</param>
        /// <returns>True when the item currently has a valid prefab reference.</returns>
        public bool TryGetWorldPrefab(out GameObject worldPrefab)
        {
            try
            {
                worldPrefab = _worldPrefab;

                if (worldPrefab == null)
                {
                    return false;
                }

                // Touch the asset name so Unity validates the prefab reference instead of leaving a stale missing wrapper around.
                _ = worldPrefab.name;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning($"[ItemDefinition] WorldPrefab reference is missing on item '{DisplayName}'. Reassign '_worldPrefab' in the inspector.", this);
                worldPrefab = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates serialized item constraints after editor changes.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (!_isStackable)
            {
                _maxStackSize = 1;
                return;
            }

            _maxStackSize = Mathf.Max(1, _maxStackSize);
        }
    }
}
