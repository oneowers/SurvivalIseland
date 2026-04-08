// Path: Assets/Project/Scripts/Player/PlayerHitDamageResolver.cs
// Purpose: Resolves outgoing hit damage from the currently equipped player tool tier.
// Dependencies: ProjectResonance.TreeFelling, ProjectResonance.PlayerCombat.

using ProjectResonance.TreeFelling;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Resolves player hit damage from the currently equipped axe tier.
    /// </summary>
    public sealed class PlayerHitDamageResolver
    {
        private readonly IInventoryQuery _inventoryQuery;

        /// <summary>
        /// Creates the player hit damage resolver.
        /// </summary>
        /// <param name="inventoryQuery">Read-only inventory query used to inspect the equipped axe.</param>
        public PlayerHitDamageResolver(IInventoryQuery inventoryQuery)
        {
            _inventoryQuery = inventoryQuery;
        }

        /// <summary>
        /// Resolves the current player hit profile.
        /// </summary>
        /// <returns>Resolved hit payload for the current equipment.</returns>
        public ResolvedPlayerHit ResolveCurrentHit()
        {
            var axeTier = _inventoryQuery != null ? _inventoryQuery.GetEquippedAxeTier() : AxeTier.None;
            return new ResolvedPlayerHit(axeTier, ResolveDamage(axeTier));
        }

        private static int ResolveDamage(AxeTier axeTier)
        {
            switch (axeTier)
            {
                case AxeTier.Stone:
                    return 2;
                case AxeTier.Iron:
                    return 3;
                default:
                    return 1;
            }
        }
    }
}
