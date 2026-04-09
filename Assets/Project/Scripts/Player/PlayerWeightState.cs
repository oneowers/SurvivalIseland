// Path: Assets/Project/Scpripts/Player/PlayerWeightState.cs
// Purpose: Stores authored initial player carry weight data.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.PlayerWeight
{
    /// <summary>
    /// Represents the player's current carry weight class.
    /// </summary>
    public enum PlayerWeightType
    {
        /// <summary>
        /// The player carries nothing.
        /// </summary>
        Empty = 0,

        /// <summary>
        /// The player carries a light item such as a branch.
        /// </summary>
        LightItem = 1,

        /// <summary>
        /// The player carries one heavy log.
        /// </summary>
        HeavyLog = 2,

        /// <summary>
        /// The player carries two logs.
        /// </summary>
        TwoLogs = 3,
    }

    /// <summary>
    /// Publishes player carry weight changes.
    /// </summary>
    public readonly struct WeightChangedEvent
    {
        /// <summary>
        /// Creates a new weight changed event.
        /// </summary>
        /// <param name="previousWeight">Previous weight state.</param>
        /// <param name="currentWeight">Current weight state.</param>
        public WeightChangedEvent(PlayerWeightType previousWeight, PlayerWeightType currentWeight)
        {
            PreviousWeight = previousWeight;
            CurrentWeight = currentWeight;
        }

        /// <summary>
        /// Gets the previous weight state.
        /// </summary>
        public PlayerWeightType PreviousWeight { get; }

        /// <summary>
        /// Gets the current weight state.
        /// </summary>
        public PlayerWeightType CurrentWeight { get; }
    }

    /// <summary>
    /// ScriptableObject authoring data for the player's carry weight runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerWeightState", menuName = "Project Resonance/Player/Player Weight State")]
    public sealed class PlayerWeightState : ScriptableObject
    {
        [SerializeField]
        private PlayerWeightType _initialWeight = PlayerWeightType.Empty;

        /// <summary>
        /// Gets the authored initial player carry weight.
        /// </summary>
        public PlayerWeightType InitialWeight => _initialWeight;
    }
}
