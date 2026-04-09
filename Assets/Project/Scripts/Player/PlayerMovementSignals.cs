// Path: Assets/Project/Scripts/Player/PlayerMovementSignals.cs
// Purpose: Provides explicit runtime movement-related signals without a global message bus.
// Dependencies: UnityEngine.

using System;
using UnityEngine;

namespace ProjectResonance.PlayerMovement
{
    /// <summary>
    /// Shared runtime signal service for movement-related events.
    /// </summary>
    public sealed class PlayerMovementSignals
    {
        /// <summary>
        /// Raised when an external system requests a gravity pull on the player.
        /// </summary>
        public event Action<PlayerGravityPullEvent> GravityPullRequested;

        /// <summary>
        /// Raised when a footstep has been emitted.
        /// </summary>
        public event Action<FootstepEvent> FootstepEmitted;

        /// <summary>
        /// Requests a gravity pull on the player.
        /// </summary>
        /// <param name="message">Pull payload.</param>
        public void RequestGravityPull(PlayerGravityPullEvent message)
        {
            GravityPullRequested?.Invoke(message);
        }

        /// <summary>
        /// Notifies listeners that the player emitted a footstep.
        /// </summary>
        /// <param name="message">Footstep payload.</param>
        public void NotifyFootstep(FootstepEvent message)
        {
            FootstepEmitted?.Invoke(message);
        }
    }
}
