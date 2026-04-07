// Path: Assets/Project/Scpripts/TreeFelling/PlayerCarryAnchorAdapter.cs
// Purpose: Exposes the follow transform used for carried logs.
// Dependencies: UnityEngine, TreeFelling.

using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Inspector-configurable carry anchor adapter for held logs.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Player Carry Anchor Adapter")]
    [DisallowMultipleComponent]
    public sealed class PlayerCarryAnchorAdapter : MonoBehaviour, IPlayerCarryAnchor
    {
        [SerializeField]
        private Transform _followTransform;

        /// <summary>
        /// Gets the transform used as the carry follow target.
        /// </summary>
        public Transform FollowTransform => _followTransform != null ? _followTransform : transform;

        private void Reset()
        {
            _followTransform = transform;
        }
    }
}
