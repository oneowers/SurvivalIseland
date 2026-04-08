// Path: Assets/Project/Scripts/Ghosts/GhostHitReceiver.cs
// Purpose: Adapts generic player-hit requests onto pooled ghost combat health.
// Dependencies: UnityEngine, ProjectResonance.Ghosts, ProjectResonance.PlayerCombat.

using ProjectResonance.PlayerCombat;
using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Receives generic player hits and applies them to a ghost combat-health component.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Ghost Hit Receiver")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GhostCombatHealth))]
    public sealed class GhostHitReceiver : MonoBehaviour, IPlayerHitReceiver
    {
        [SerializeField]
        private GhostCombatHealth _combatHealth;

        /// <summary>
        /// Gets whether the ghost can currently receive hits.
        /// </summary>
        public bool CanReceiveHit => _combatHealth != null && _combatHealth.IsAlive;

        /// <summary>
        /// Applies the current player hit to the ghost.
        /// </summary>
        /// <param name="context">Resolved player-hit payload.</param>
        public void ReceiveHit(in PlayerHitContext context)
        {
            if (!CanReceiveHit)
            {
                return;
            }

            _combatHealth.ApplyDamage(context.Damage);
        }

        private void Reset()
        {
            _combatHealth = GetComponent<GhostCombatHealth>();
        }

        private void OnValidate()
        {
            if (_combatHealth == null)
            {
                _combatHealth = GetComponent<GhostCombatHealth>();
            }
        }
    }
}
