// Path: Assets/Project/Scripts/Inventory/ItemDefinition.cs
// Purpose: Defines the single unified authored item asset used by inventory, tools, resources, planting, and world node data.
// Dependencies: UnityEngine, ProjectResonance.TreeFelling.

using ProjectResonance.TreeFelling;
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

        /// <summary>
        /// Utility item used by supporting gameplay systems.
        /// </summary>
        Utility = 3,
    }

    /// <summary>
    /// High-level authored resource node categories.
    /// </summary>
    public enum ResourceNodeType
    {
        /// <summary>
        /// A wood-producing tree.
        /// </summary>
        Tree = 0,

        /// <summary>
        /// A generic stone-producing rock.
        /// </summary>
        Rock = 1,

        /// <summary>
        /// A coal-producing deposit.
        /// </summary>
        Coal = 2,

        /// <summary>
        /// A generic ore-producing node.
        /// </summary>
        Ore = 3,

        /// <summary>
        /// A custom resource type defined by content authors.
        /// </summary>
        Custom = 4,
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

        [Header("Tool")]
        [SerializeField]
        private AxeTier _axeTier = AxeTier.None;

        [SerializeField]
        private bool _usesDurability;

        [SerializeField]
        [Min(1)]
        private int _maxDurability = 20;

        [SerializeField]
        private bool _breaksOnZeroDurability = true;

        [Header("Resource Node")]
        [SerializeField]
        private string _resourceId;

        [SerializeField]
        private string _nodeDisplayName;

        [SerializeField]
        private ResourceNodeType _resourceNodeType = ResourceNodeType.Custom;

        [SerializeField]
        [Min(1)]
        private int _maxHealth = 4;

        [SerializeField]
        [Min(0)]
        private int _dropCount = 1;

        [Header("Planting")]
        [SerializeField]
        private bool _canPlantFromInventory;

        [SerializeField]
        private GameObject _plantSpawnPrefab;

        [SerializeField]
        private bool _snapPlantingToIntegerGrid = true;

        [SerializeField]
        private LayerMask _plantingGroundMask = 1;

        [SerializeField]
        [Min(0.1f)]
        private float _plantingProbeHeight = 3f;

        [SerializeField]
        [Min(0.1f)]
        private float _plantingProbeDistance = 8f;

        [SerializeField]
        [Min(0.05f)]
        private float _plantingClearRadius = 0.75f;

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
        /// Gets whether this item can behave as a tool.
        /// </summary>
        public bool IsTool => _itemType == ItemType.Tool;

        /// <summary>
        /// Gets whether this item can behave as a breakable resource node.
        /// </summary>
        public bool IsResourceNode => _itemType == ItemType.Resource;

        /// <summary>
        /// Gets the axe tier provided by this item when used as a tool.
        /// </summary>
        public AxeTier AxeTier => IsTool ? _axeTier : AxeTier.None;

        /// <summary>
        /// Gets whether this tool consumes durability on successful hits.
        /// </summary>
        public bool UsesDurability => IsTool && _usesDurability;

        /// <summary>
        /// Gets the maximum authored durability for this tool item.
        /// </summary>
        public int MaxDurability => Mathf.Max(1, _maxDurability);

        /// <summary>
        /// Gets whether this tool should be removed when its durability reaches zero.
        /// </summary>
        public bool BreaksOnZeroDurability => _breaksOnZeroDurability;

        /// <summary>
        /// Gets the stable resource identifier used by content and save data.
        /// </summary>
        public string ResourceId => _resourceId;

        /// <summary>
        /// Gets the player-facing display name for the world resource node.
        /// </summary>
        public string NodeDisplayName => string.IsNullOrWhiteSpace(_nodeDisplayName) ? DisplayName : _nodeDisplayName;

        /// <summary>
        /// Gets the authored resource category.
        /// </summary>
        public ResourceNodeType ResourceNodeType => _resourceNodeType;

        /// <summary>
        /// Gets the authored resource category.
        /// </summary>
        public ResourceNodeType ResourceType => _resourceNodeType;

        /// <summary>
        /// Gets the maximum health authored for this resource node.
        /// </summary>
        public int MaxHealth => Mathf.Max(1, _maxHealth);

        /// <summary>
        /// Gets the amount of pickup instances spawned when this node breaks.
        /// </summary>
        public int DropCount => Mathf.Max(0, _dropCount);

        /// <summary>
        /// Gets whether this resource item can be planted from the inventory.
        /// </summary>
        public bool CanPlantFromInventory => IsResourceNode && _canPlantFromInventory;

        /// <summary>
        /// Gets the prefab spawned when this item is planted back into the world.
        /// </summary>
        public GameObject PlantSpawnPrefab => _plantSpawnPrefab;

        /// <summary>
        /// Safely resolves the prefab used when planting this item back into the world.
        /// </summary>
        /// <param name="plantSpawnPrefab">Resolved prefab when available.</param>
        /// <returns>True when the item currently has a valid planting prefab reference.</returns>
        public bool TryGetPlantSpawnPrefab(out GameObject plantSpawnPrefab)
        {
            try
            {
                plantSpawnPrefab = _plantSpawnPrefab;

                if (plantSpawnPrefab == null)
                {
                    return false;
                }

                // Touch the asset name so Unity validates the prefab reference instead of returning a stale missing wrapper.
                _ = plantSpawnPrefab.name;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning($"[ItemDefinition] PlantSpawnPrefab reference is missing on item '{DisplayName}'. Reassign '_plantSpawnPrefab' in the inspector.", this);
                plantSpawnPrefab = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets whether planting snaps X/Z coordinates to integer grid positions.
        /// </summary>
        public bool SnapPlantingToIntegerGrid => _snapPlantingToIntegerGrid;

        /// <summary>
        /// Gets the ground mask used by planting validation raycasts.
        /// </summary>
        public LayerMask PlantingGroundMask => _plantingGroundMask;

        /// <summary>
        /// Gets the upward probe offset used before casting downward onto the ground.
        /// </summary>
        public float PlantingProbeHeight => Mathf.Max(0.1f, _plantingProbeHeight);

        /// <summary>
        /// Gets the downward probe distance used while validating a planting point.
        /// </summary>
        public float PlantingProbeDistance => Mathf.Max(0.1f, _plantingProbeDistance);

        /// <summary>
        /// Gets the radius used to check whether the planting point is free.
        /// </summary>
        public float PlantingClearRadius => Mathf.Max(0.05f, _plantingClearRadius);

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
            }

            _maxStackSize = Mathf.Max(1, _maxStackSize);
            _maxDurability = Mathf.Max(1, _maxDurability);
            _maxHealth = Mathf.Max(1, _maxHealth);
            _dropCount = Mathf.Max(0, _dropCount);
            _plantingProbeHeight = Mathf.Max(0.1f, _plantingProbeHeight);
            _plantingProbeDistance = Mathf.Max(0.1f, _plantingProbeDistance);
            _plantingClearRadius = Mathf.Max(0.05f, _plantingClearRadius);
        }
    }
}
