// Path: Assets/Project/Scpripts/TreeFelling/TreeTargetingConfig.cs
// Purpose: Stores tunable settings for smart nearby tree detection around the player.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// ScriptableObject with settings for selecting the best nearby tree target.
    /// </summary>
    [CreateAssetMenu(fileName = "TreeTargetingConfig", menuName = "Project Resonance/Tree Felling/Tree Targeting Config")]
    public sealed class TreeTargetingConfig : ScriptableObject
    {
        [SerializeField]
        [Min(0.5f)]
        private float _detectionRadius = 3f;

        [SerializeField]
        [Min(0.02f)]
        private float _checkFrequencySeconds = 0.1f;

        [SerializeField]
        private LayerMask _treeLayerMask = 0;

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
        /// Gets the layer mask used to detect trees.
        /// </summary>
        public LayerMask TreeLayerMask => _treeLayerMask;

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
