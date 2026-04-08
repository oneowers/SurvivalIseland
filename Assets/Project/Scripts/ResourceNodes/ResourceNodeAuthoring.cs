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
        private ResourceNodeDefinition _definition;

        /// <summary>
        /// Gets the authored resource node definition assigned to this object.
        /// </summary>
        public ResourceNodeDefinition Definition => _definition;

        /// <summary>
        /// Gets the authored resource type, or <see cref="ResourceNodeType.Custom"/> when no definition is assigned.
        /// </summary>
        public ResourceNodeType ResourceType => _definition != null ? _definition.ResourceType : ResourceNodeType.Custom;

        /// <summary>
        /// Gets the authored resource display name, or the GameObject name when no definition is assigned.
        /// </summary>
        public string DisplayName => _definition != null ? _definition.NodeDisplayName : gameObject.name;

        /// <summary>
        /// Gets the inventory item definition authored for this resource node.
        /// </summary>
        public ItemDefinition DropItemDefinition => _definition;

        /// <summary>
        /// Gets the maximum health authored for this resource node.
        /// </summary>
        public int MaxHealth => _definition != null ? _definition.MaxHealth : 1;

        /// <summary>
        /// Gets the drop count authored for this resource node.
        /// </summary>
        public int DropCount => _definition != null ? _definition.DropCount : 0;

        /// <summary>
        /// Gets the break sound authored for this resource node.
        /// </summary>
        public AudioClip BreakSound => _definition != null ? _definition.BreakSound : null;

        /// <summary>
        /// Resolves a deterministic hit sound from the authored resource definition.
        /// </summary>
        /// <param name="strikeIndex">Zero-based strike index.</param>
        /// <returns>Configured hit sound, or null when none is authored.</returns>
        public AudioClip GetHitSound(int strikeIndex)
        {
            return _definition != null ? _definition.GetHitSound(strikeIndex) : null;
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
            if (_definition == null || _definition.DecalPrefab == null || hitAnchor == null)
            {
                resourceDecalEvent = default;
                return false;
            }

            resourceDecalEvent = new ResourceDecalEvent(
                hitAnchor,
                _definition.DecalPrefab,
                _definition.DecalSize,
                _definition.MinDecalOpacity,
                _definition.MaxDecalOpacity,
                _definition.DecalPoolCapacity,
                hitsRemaining,
                maxHits);

            return true;
        }
    }
}
