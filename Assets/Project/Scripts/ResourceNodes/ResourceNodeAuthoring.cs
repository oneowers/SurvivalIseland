// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeAuthoring.cs
// Purpose: Exposes authored reusable data for any breakable resource node in scenes and prefabs.
// Dependencies: UnityEngine, ProjectResonance.Inventory, ProjectResonance.ResourceNodes.

using ProjectResonance.Inventory;
using UnityEngine;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Exposes a resource node definition to runtime systems on a GameObject.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Authoring")]
    [DisallowMultipleComponent]
    public sealed class ResourceNodeAuthoring : MonoBehaviour
    {
        [SerializeField]
        private ItemDefinition _definition;

        [Header("Feedback")]
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

        [SerializeField]
        private AudioClip[] _hitSounds;

        [SerializeField]
        private AudioClip _breakSound;

        /// <summary>
        /// Gets the authored resource node definition assigned to this object.
        /// </summary>
        public ItemDefinition Definition => ResolveResourceDefinition();

        /// <summary>
        /// Gets the authored resource type, or <see cref="ResourceNodeType.Custom"/> when no definition is assigned.
        /// </summary>
        public ResourceNodeType ResourceType => ResolveResourceDefinition() != null ? ResolveResourceDefinition().ResourceType : ResourceNodeType.Custom;

        /// <summary>
        /// Gets the authored resource display name, or the GameObject name when no definition is assigned.
        /// </summary>
        public string DisplayName => ResolveResourceDefinition() != null ? ResolveResourceDefinition().NodeDisplayName : gameObject.name;

        /// <summary>
        /// Gets the inventory item definition authored for this resource node.
        /// </summary>
        public ItemDefinition DropItemDefinition => ResolveResourceDefinition();

        /// <summary>
        /// Gets the maximum health authored for this resource node.
        /// </summary>
        public int MaxHealth => ResolveResourceDefinition() != null ? ResolveResourceDefinition().MaxHealth : 1;

        /// <summary>
        /// Gets the drop count authored for this resource node.
        /// </summary>
        public int DropCount => ResolveResourceDefinition() != null ? ResolveResourceDefinition().DropCount : 0;

        /// <summary>
        /// Gets the break sound authored for this resource node.
        /// </summary>
        public AudioClip BreakSound => _breakSound;

        /// <summary>
        /// Resolves a deterministic hit sound from the authored resource definition.
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
        /// Builds a resource decal event from the authored definition.
        /// </summary>
        /// <param name="hitAnchor">Transform the decal should follow.</param>
        /// <param name="hitsRemaining">Remaining durability after the hit.</param>
        /// <param name="maxHits">Maximum durability of the node.</param>
        /// <param name="resourceDecalEvent">Created decal event when successful.</param>
        /// <returns>True when the current definition can emit a decal event.</returns>
        public bool TryCreateDecalEvent(Transform hitAnchor, int hitsRemaining, int maxHits, out ResourceDecalEvent resourceDecalEvent)
        {
            var definition = ResolveResourceDefinition();
            if (definition == null || _decalPrefab == null || hitAnchor == null)
            {
                resourceDecalEvent = default;
                return false;
            }

            resourceDecalEvent = new ResourceDecalEvent(
                hitAnchor,
                _decalPrefab,
                _decalSize,
                _minDecalOpacity,
                _maxDecalOpacity,
                Mathf.Max(1, _decalPoolCapacity),
                hitsRemaining,
                maxHits);

            return true;
        }

        private void OnValidate()
        {
            if (_definition != null && !_definition.IsResourceNode)
            {
                Debug.LogWarning($"[ResourceNodeAuthoring] '{name}' expects an ItemDefinition with ItemType=Resource. Assigned '{_definition.DisplayName}' is {_definition.ItemType}.", this);
            }

            _minDecalOpacity = Mathf.Clamp01(_minDecalOpacity);
            _maxDecalOpacity = Mathf.Clamp01(_maxDecalOpacity);
            _decalPoolCapacity = Mathf.Max(1, _decalPoolCapacity);

            if (_maxDecalOpacity < _minDecalOpacity)
            {
                _maxDecalOpacity = _minDecalOpacity;
            }
        }

        private ItemDefinition ResolveResourceDefinition()
        {
            return _definition != null && _definition.IsResourceNode
                ? _definition
                : null;
        }
    }
}
