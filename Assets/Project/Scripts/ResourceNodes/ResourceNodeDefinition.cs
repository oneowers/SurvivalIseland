// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeDefinition.cs
// Purpose: Defines reusable authored data for any breakable resource node and also serves as the dropped inventory item asset.
// Dependencies: UnityEngine, ProjectResonance.Inventory.

using ProjectResonance.Inventory;
using UnityEngine;

namespace ProjectResonance.ResourceNodes
{
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
    /// Unified authored asset that describes both a breakable resource node in the world and the inventory item gained from harvesting it.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceNodeDefinition", menuName = "Project Resonance/Resource Nodes/Resource Node Definition")]
    public sealed class ResourceNodeDefinition : ItemDefinition
    {
        [SerializeField]
        private string _resourceId;

        [SerializeField]
        private string _nodeDisplayName;

        [SerializeField]
        private ResourceNodeType _resourceType = ResourceNodeType.Custom;

        [Header("Health")]
        [SerializeField]
        [Min(1)]
        private int _maxHealth = 4;

        [SerializeField]
        [Min(0)]
        private int _dropCount = 3;

        [Header("Decals")]
        [SerializeField]
        private GameObject _decalPrefab;

        [SerializeField]
        private Vector3 _decalSize = new Vector3(0.32f, 0.32f, 0.32f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _minDecalOpacity = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _maxDecalOpacity = 1f;

        [SerializeField]
        [Min(1)]
        private int _decalPoolCapacity = 12;

        [Header("Audio")]
        [SerializeField]
        private AudioClip[] _hitSounds;

        [SerializeField]
        private AudioClip _breakSound;

        /// <summary>
        /// Gets the stable resource identifier used by content and save data.
        /// </summary>
        public string ResourceId => _resourceId;

        /// <summary>
        /// Gets the player-facing display name for the world resource node.
        /// </summary>
        public string NodeDisplayName => string.IsNullOrWhiteSpace(_nodeDisplayName) ? name : _nodeDisplayName;

        /// <summary>
        /// Gets the authored resource category.
        /// </summary>
        public ResourceNodeType ResourceType => _resourceType;

        /// <summary>
        /// Gets the maximum health authored for this resource node.
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// Gets the amount of pickup instances spawned when this node breaks.
        /// </summary>
        public int DropCount => _dropCount;

        /// <summary>
        /// Gets the prefab used for hit decals.
        /// </summary>
        public GameObject DecalPrefab => _decalPrefab;

        /// <summary>
        /// Gets the size applied to the hit decal projector or fallback mesh.
        /// </summary>
        public Vector3 DecalSize => _decalSize;

        /// <summary>
        /// Gets the minimum opacity used on the first visible hit mark.
        /// </summary>
        public float MinDecalOpacity => _minDecalOpacity;

        /// <summary>
        /// Gets the maximum opacity used when the node is close to destruction.
        /// </summary>
        public float MaxDecalOpacity => _maxDecalOpacity;

        /// <summary>
        /// Gets the initial pool capacity for decals using this definition.
        /// </summary>
        public int DecalPoolCapacity => _decalPoolCapacity;

        /// <summary>
        /// Gets the clip played when the node breaks.
        /// </summary>
        public AudioClip BreakSound => _breakSound;

        /// <summary>
        /// Resolves a deterministic hit sound from the authored hit sound bank.
        /// </summary>
        /// <param name="strikeIndex">Zero-based strike index.</param>
        /// <returns>Configured hit sound, or null when none is authored.</returns>
        public AudioClip GetHitSound(int strikeIndex)
        {
            if (_hitSounds == null || _hitSounds.Length == 0)
            {
                return null;
            }

            var hitSoundIndex = Mathf.Abs(strikeIndex) % _hitSounds.Length;
            return _hitSounds[hitSoundIndex];
        }

        /// <summary>
        /// Validates serialized resource-node constraints after editor changes.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            _maxHealth = Mathf.Max(1, _maxHealth);
            _dropCount = Mathf.Max(0, _dropCount);
            _decalPoolCapacity = Mathf.Max(1, _decalPoolCapacity);
            _minDecalOpacity = Mathf.Clamp01(_minDecalOpacity);
            _maxDecalOpacity = Mathf.Clamp01(_maxDecalOpacity);

            if (_maxDecalOpacity < _minDecalOpacity)
            {
                _maxDecalOpacity = _minDecalOpacity;
            }
        }
    }
}
