// Path: Assets/Project/Scripts/Player/PlayerAimModeQuery.cs
// Purpose: Exposes the currently active right-stick interaction mode to other player systems.
// Dependencies: none.

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Read-only contract used by player systems to know whether right-stick input is currently reserved for planting.
    /// </summary>
    public interface IPlayerAimModeQuery
    {
        /// <summary>
        /// Gets whether the active right-stick mode is planting instead of combat.
        /// </summary>
        bool IsPlantingModeActive { get; }
    }
}
