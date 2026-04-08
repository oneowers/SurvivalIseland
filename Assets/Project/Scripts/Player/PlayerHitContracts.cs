// Path: Assets/Project/Scripts/Player/PlayerHitContracts.cs
// Purpose: Defines generic player-hit contracts shared by resources, ghosts, and aim targeting.
// Dependencies: UnityEngine, ProjectResonance.TreeFelling.

using ProjectResonance.TreeFelling;
using UnityEngine;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Immutable combat payload sent from the player to the selected target receiver.
    /// </summary>
    public readonly struct PlayerHitContext
    {
        /// <summary>
        /// Creates a new player-hit context.
        /// </summary>
        /// <param name="attacker">Player transform performing the hit.</param>
        /// <param name="origin">World origin of the hit.</param>
        /// <param name="hitDirection">World-space hit direction.</param>
        /// <param name="axeTier">Currently equipped axe tier.</param>
        /// <param name="damage">Resolved outgoing damage.</param>
        public PlayerHitContext(Transform attacker, Vector3 origin, Vector3 hitDirection, AxeTier axeTier, int damage)
        {
            Attacker = attacker;
            Origin = origin;
            HitDirection = hitDirection;
            AxeTier = axeTier;
            Damage = damage;
        }

        /// <summary>
        /// Gets the player transform that issued the hit.
        /// </summary>
        public Transform Attacker { get; }

        /// <summary>
        /// Gets the world origin of the hit.
        /// </summary>
        public Vector3 Origin { get; }

        /// <summary>
        /// Gets the world-space direction of the hit.
        /// </summary>
        public Vector3 HitDirection { get; }

        /// <summary>
        /// Gets the equipped axe tier used to resolve damage.
        /// </summary>
        public AxeTier AxeTier { get; }

        /// <summary>
        /// Gets the resolved damage value.
        /// </summary>
        public int Damage { get; }
    }

    /// <summary>
    /// Immutable resolved damage profile for the current player equipment.
    /// </summary>
    public readonly struct ResolvedPlayerHit
    {
        /// <summary>
        /// Creates a new resolved player-hit profile.
        /// </summary>
        /// <param name="axeTier">Resolved axe tier.</param>
        /// <param name="damage">Resolved damage.</param>
        public ResolvedPlayerHit(AxeTier axeTier, int damage)
        {
            AxeTier = axeTier;
            Damage = damage;
        }

        /// <summary>
        /// Gets the resolved axe tier.
        /// </summary>
        public AxeTier AxeTier { get; }

        /// <summary>
        /// Gets the resolved damage amount.
        /// </summary>
        public int Damage { get; }
    }

    /// <summary>
    /// Contract implemented by any world object that can receive a player hit.
    /// </summary>
    public interface IPlayerHitReceiver
    {
        /// <summary>
        /// Gets whether the target can currently receive hits.
        /// </summary>
        bool CanReceiveHit { get; }

        /// <summary>
        /// Applies a player hit to the receiver.
        /// </summary>
        /// <param name="context">Resolved hit payload.</param>
        void ReceiveHit(in PlayerHitContext context);
    }
}
