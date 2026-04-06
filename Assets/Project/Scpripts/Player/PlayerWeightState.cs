// Path: Assets/Project/Scpripts/Player/PlayerWeightState.cs
// Purpose: Stores runtime player carry weight as a ScriptableObject state and publishes weight changes.
// Dependencies: MessagePipe, UnityEngine.

using MessagePipe;
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
    /// ScriptableObject runtime state for the player's carry weight.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerWeightState", menuName = "Project Resonance/Player/Player Weight State")]
    public sealed class PlayerWeightState : ScriptableObject
    {
        [SerializeField]
        private PlayerWeightType _initialWeight = PlayerWeightType.Empty;

        private IBufferedPublisher<WeightChangedEvent> _publisher;
        private PlayerWeightType _currentWeight;
        private bool _isInitialized;

        /// <summary>
        /// Gets the current runtime weight state.
        /// </summary>
        public PlayerWeightType CurrentWeight => _currentWeight;

        /// <summary>
        /// Initializes the runtime state with its event publisher.
        /// </summary>
        /// <param name="publisher">Buffered publisher for weight changes.</param>
        public void Initialize(IBufferedPublisher<WeightChangedEvent> publisher)
        {
            _publisher = publisher;
            _currentWeight = _initialWeight;
            _isInitialized = true;

            // The initial event synchronizes systems that subscribe after the installer finishes building the scope.
            _publisher.Publish(new WeightChangedEvent(_currentWeight, _currentWeight));
        }

        /// <summary>
        /// Sets the current player weight state.
        /// </summary>
        /// <param name="weightType">New runtime weight state.</param>
        public void SetWeight(PlayerWeightType weightType)
        {
            if (!_isInitialized || _currentWeight == weightType)
            {
                return;
            }

            var previousWeight = _currentWeight;
            _currentWeight = weightType;
            _publisher.Publish(new WeightChangedEvent(previousWeight, _currentWeight));
        }

        /// <summary>
        /// Resets the current runtime state back to the initial asset value.
        /// </summary>
        public void ResetState()
        {
            if (!_isInitialized)
            {
                return;
            }

            SetWeight(_initialWeight);
        }
    }
}
