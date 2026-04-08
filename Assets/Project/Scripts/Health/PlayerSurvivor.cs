// Path: Assets/Project/Scpripts/Health/PlayerSurvivor.cs
// Purpose: Represents the player target used by gameplay systems in the scene.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Health
{
    /// <summary>
    /// Scene anchor for the player's world position.
    /// </summary>
    public sealed class PlayerSurvivor : MonoBehaviour
    {
        [SerializeField]
        private Transform _targetPoint;

        /// <summary>
        /// Gets the transform used by enemies as the attack target.
        /// </summary>
        public Transform TargetPoint => _targetPoint != null ? _targetPoint : transform;

        /// <summary>
        /// Gets the player's current world position.
        /// </summary>
        public Vector3 Position => TargetPoint.position;
    }
}
