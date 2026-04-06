// Path: Assets/Project/Scpripts/Campfire/CampfireSystem.cs
// Purpose: Owns campfire fuel state, visuals and protection radius.
// Dependencies: CampfireConfig, CampfireAnchor, MessagePipe, UnityEngine, VContainer.

using MessagePipe;
using ProjectResonance.Common.Messages;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Stores and updates the runtime campfire state.
    /// </summary>
    public sealed class CampfireSystem : ICampfireService, IStartable, ITickable
    {
        private readonly CampfireConfig _config;
        private readonly CampfireAnchor _anchor;
        private readonly IBufferedPublisher<CampfireStateChangedMessage> _stateChangedPublisher;

        private float _fuelSeconds;
        private bool _isLit;

        /// <summary>
        /// Initializes the campfire system.
        /// </summary>
        /// <param name="config">Campfire configuration.</param>
        /// <param name="anchor">Scene anchor with visuals and transform.</param>
        /// <param name="stateChangedPublisher">Buffered campfire state publisher.</param>
        public CampfireSystem(
            CampfireConfig config,
            CampfireAnchor anchor,
            IBufferedPublisher<CampfireStateChangedMessage> stateChangedPublisher)
        {
            _config = config;
            _anchor = anchor;
            _stateChangedPublisher = stateChangedPublisher;
            _fuelSeconds = Mathf.Clamp(config.StartFuelSeconds, 0f, config.MaxFuelSeconds);
            _isLit = _fuelSeconds > 0f;
        }

        /// <summary>
        /// Gets whether the campfire is currently lit.
        /// </summary>
        public bool IsLit => _isLit;

        /// <summary>
        /// Gets the world position of the campfire.
        /// </summary>
        public Vector3 Position => _anchor.FirePoint.position;

        /// <summary>
        /// Gets the protection radius in world units.
        /// </summary>
        public float ProtectionRadius => _config.ProtectionRadius;

        /// <summary>
        /// Publishes the initial campfire state when the container starts.
        /// </summary>
        public void Start()
        {
            PublishCurrentState();
        }

        /// <summary>
        /// Ticks the campfire fuel and visuals during the Unity player loop.
        /// </summary>
        public void Tick()
        {
            if (!_isLit || _config.FuelConsumptionPerSecond <= 0f)
            {
                return;
            }

            _fuelSeconds = Mathf.Max(0f, _fuelSeconds - (_config.FuelConsumptionPerSecond * Time.deltaTime));
            var nextIsLit = _fuelSeconds > 0f;

            // Publishing every tick keeps visuals and late subscribers synchronized with the latest fuel value.
            _isLit = nextIsLit;
            PublishCurrentState();
        }

        /// <summary>
        /// Adds fuel to the campfire.
        /// </summary>
        /// <param name="fuelSeconds">Fuel amount in seconds.</param>
        public void AddFuel(float fuelSeconds)
        {
            if (fuelSeconds <= 0f)
            {
                return;
            }

            _fuelSeconds = Mathf.Clamp(_fuelSeconds + fuelSeconds, 0f, _config.MaxFuelSeconds);
            _isLit = _fuelSeconds > 0f;
            PublishCurrentState();
        }

        private void PublishCurrentState()
        {
            var normalizedFuel = _config.MaxFuelSeconds > 0f
                ? Mathf.Clamp01(_fuelSeconds / _config.MaxFuelSeconds)
                : 0f;

            _anchor.ApplyState(_isLit, normalizedFuel, _config.MinLightIntensity, _config.MaxLightIntensity);
            _stateChangedPublisher.Publish(new CampfireStateChangedMessage(_isLit, _fuelSeconds, _config.MaxFuelSeconds, _config.ProtectionRadius));
        }
    }
}
