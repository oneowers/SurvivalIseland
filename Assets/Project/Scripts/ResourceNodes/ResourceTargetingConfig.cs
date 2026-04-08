// Path: Assets/Project/Scripts/ResourceNodes/ResourceTargetingConfig.cs
// Purpose: Stores tunable settings for smart nearby resource-node detection around the player.
// Dependencies: UnityEngine.

using UnityEngine.Serialization;
using UnityEngine;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// ScriptableObject with settings for selecting the best nearby resource target.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceTargetingConfig", menuName = "Project Resonance/Resource Nodes/Resource Targeting Config")]
    public sealed class ResourceTargetingConfig : ScriptableObject
    {
        [SerializeField]
        [Min(0.5f)]
        private float _detectionRadius = 3f;

        [SerializeField]
        [Min(0.02f)]
        private float _checkFrequencySeconds = 0.1f;

        [SerializeField]
        [FormerlySerializedAs("_treeLayerMask")]
        private LayerMask _resourceLayerMask = 0;

        [SerializeField]
        [Min(1)]
        private int _maxDetectedColliders = 16;

        [SerializeField]
        [Range(-1f, 1f)]
        private float _minimumViewDot = 0f;

        /// <summary>
        /// Gets the nearby detection radius around the player.
        /// </summary>
        public float DetectionRadius => _detectionRadius;

        /// <summary>
        /// Gets how often the detector refreshes its selected target.
        /// </summary>
        public float CheckFrequencySeconds => _checkFrequencySeconds;

        /// <summary>
        /// Gets the optional broadphase layer mask used before component filtering.
        /// </summary>
        public LayerMask BroadphaseLayerMask => _resourceLayerMask.value == 0 ? ~0 : _resourceLayerMask;

        /// <summary>
        /// Gets the maximum amount of colliders evaluated per scan.
        /// </summary>
        public int MaxDetectedColliders => _maxDetectedColliders;

        /// <summary>
        /// Gets the minimum dot product required for a target to count as visible.
        /// </summary>
        public float MinimumViewDot => _minimumViewDot;
    }
}
