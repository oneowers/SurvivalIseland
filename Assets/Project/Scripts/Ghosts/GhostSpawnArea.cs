// Path: Assets/Project/Scpripts/Ghosts/GhostSpawnArea.cs
// Purpose: Defines the world area where ghosts can appear.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Scene anchor that defines the ghost spawn radius.
    /// </summary>
    public sealed class GhostSpawnArea : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        private float _radius = 25f;

        /// <summary>
        /// Gets the world-space center position of the spawn area.
        /// </summary>
        public Vector3 Center => transform.position;

        /// <summary>
        /// Gets the spawn radius in world units.
        /// </summary>
        public float Radius => _radius;
    }
}
