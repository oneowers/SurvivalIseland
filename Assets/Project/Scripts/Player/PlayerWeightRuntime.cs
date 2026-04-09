// Path: Assets/Project/Scripts/Player/PlayerWeightRuntime.cs
// Purpose: Stores mutable runtime carry weight and exposes explicit C# events for logic and UI consumers.
// Dependencies: UnityEngine, VContainer.

using System;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.PlayerWeight
{
    /// <summary>
    /// Runtime service that owns the player's current carry weight state.
    /// </summary>
    public sealed class PlayerWeightRuntime : IStartable
    {
        private readonly PlayerWeightState _config;

        /// <summary>
        /// Creates the runtime player weight service.
        /// </summary>
        /// <param name="config">Authored player weight config asset.</param>
        public PlayerWeightRuntime(PlayerWeightState config)
        {
            _config = config;
            CurrentWeight = _config != null ? _config.InitialWeight : PlayerWeightType.Empty;
        }

        /// <summary>
        /// Gets the current runtime player weight.
        /// </summary>
        public PlayerWeightType CurrentWeight { get; private set; }

        /// <summary>
        /// Raised whenever the runtime weight changes.
        /// </summary>
        public event Action<WeightChangedEvent> WeightChanged;

        /// <summary>
        /// Publishes the initial runtime snapshot after the container starts.
        /// </summary>
        public void Start()
        {
            WeightChanged?.Invoke(new WeightChangedEvent(CurrentWeight, CurrentWeight));
        }

        /// <summary>
        /// Sets the current runtime player weight.
        /// </summary>
        /// <param name="weightType">New runtime player weight.</param>
        public void SetWeight(PlayerWeightType weightType)
        {
            if (CurrentWeight == weightType)
            {
                return;
            }

            var previousWeight = CurrentWeight;
            CurrentWeight = weightType;
            WeightChanged?.Invoke(new WeightChangedEvent(previousWeight, CurrentWeight));
        }

        /// <summary>
        /// Resets the runtime player weight back to the authored initial value.
        /// </summary>
        public void ResetState()
        {
            SetWeight(_config != null ? _config.InitialWeight : PlayerWeightType.Empty);
        }
    }
}
