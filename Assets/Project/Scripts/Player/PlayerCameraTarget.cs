// Path: Assets/Project/Scpripts/Player/PlayerCameraTarget.cs
// Purpose: Provides the camera follow anchor for the third-person camera system.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.ThirdPersonCamera
{
    /// <summary>
    /// Scene anchor that provides the camera follow target position.
    /// </summary>
    public sealed class PlayerCameraTarget : MonoBehaviour
    {
        /// <summary>
        /// Gets the world-space follow position.
        /// </summary>
        public Vector3 FollowPosition => transform.position;
    }
}
