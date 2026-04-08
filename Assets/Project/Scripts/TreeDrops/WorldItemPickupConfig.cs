// Path: Assets/Project/Scripts/TreeDrops/WorldItemPickupConfig.cs
// Purpose: Defines the shared pooled universal-pickup prefab and all movement settings used by world item drops.
// Dependencies: UnityEngine, ItemPickup.

using UnityEngine;

namespace ProjectResonance.TreeDrops
{
    /// <summary>
    /// Shared authoring config for pooled world item pickups.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldItemPickupConfig", menuName = "Project Resonance/Tree Drops/World Item Pickup Config")]
    public sealed class WorldItemPickupConfig : ScriptableObject
    {
        [Header("Pool")]
        [SerializeField]
        private ItemPickup _pickupPrefab;

        [SerializeField]
        [Min(1)]
        private int _initialPoolCapacity = 8;

        [SerializeField]
        [Min(1)]
        private int _maxPoolSize = 32;

        [Header("Movement")]
        [SerializeField]
        private PickupMovementMode _movementMode = PickupMovementMode.ImpulseThenLerp;

        [SerializeField]
        [Min(0.05f)]
        private float _pickupDuration = 0.45f;

        [SerializeField]
        [Min(0f)]
        private float _arcHeight = 1f;

        [SerializeField]
        [Min(0.1f)]
        private float _pickupTimeout = 3f;

        [SerializeField]
        [Min(0.05f)]
        private float _pickupDistance = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float _closeLerpSpeed = 8f;

        [Header("Impulse")]
        [SerializeField]
        [Min(0f)]
        private float _impulseForce = 4.5f;

        [SerializeField]
        [Min(0f)]
        private float _impulseUpwardForce = 2f;

        [SerializeField]
        [Min(0.1f)]
        private float _switchToLerpDistance = 1.2f;

        [SerializeField]
        [Min(0.05f)]
        private float _impulseDuration = 0.35f;

        /// <summary>
        /// Gets the shared universal pickup prefab.
        /// </summary>
        public ItemPickup PickupPrefab => _pickupPrefab;

        /// <summary>
        /// Gets the initial pool capacity used per pickup prefab.
        /// </summary>
        public int InitialPoolCapacity => Mathf.Max(1, _initialPoolCapacity);

        /// <summary>
        /// Gets the maximum pooled pickup count per pickup prefab.
        /// </summary>
        public int MaxPoolSize => Mathf.Max(1, _maxPoolSize);

        /// <summary>
        /// Gets the configured movement mode.
        /// </summary>
        public PickupMovementMode MovementMode => _movementMode;

        /// <summary>
        /// Gets the duration of the default pickup lerp.
        /// </summary>
        public float PickupDuration => Mathf.Max(0.05f, _pickupDuration);

        /// <summary>
        /// Gets the vertical arc height used at pickup start.
        /// </summary>
        public float ArcHeight => Mathf.Max(0f, _arcHeight);

        /// <summary>
        /// Gets the timeout before forced pickup collection.
        /// </summary>
        public float PickupTimeout => Mathf.Max(0.1f, _pickupTimeout);

        /// <summary>
        /// Gets the distance at which the pickup is considered collected.
        /// </summary>
        public float PickupDistance => Mathf.Max(0.05f, _pickupDistance);

        /// <summary>
        /// Gets the minimum close-range lerp speed.
        /// </summary>
        public float CloseLerpSpeed => Mathf.Max(0f, _closeLerpSpeed);

        /// <summary>
        /// Gets the forward impulse force.
        /// </summary>
        public float ImpulseForce => Mathf.Max(0f, _impulseForce);

        /// <summary>
        /// Gets the upward impulse force.
        /// </summary>
        public float ImpulseUpwardForce => Mathf.Max(0f, _impulseUpwardForce);

        /// <summary>
        /// Gets the distance at which impulse mode switches to lerp mode.
        /// </summary>
        public float SwitchToLerpDistance => Mathf.Max(0.1f, _switchToLerpDistance);

        /// <summary>
        /// Gets the duration of the initial impulse window.
        /// </summary>
        public float ImpulseDuration => Mathf.Max(0.05f, _impulseDuration);

        private void OnValidate()
        {
            _initialPoolCapacity = Mathf.Max(1, _initialPoolCapacity);
            _maxPoolSize = Mathf.Max(_initialPoolCapacity, _maxPoolSize);
            _pickupDuration = Mathf.Max(0.05f, _pickupDuration);
            _arcHeight = Mathf.Max(0f, _arcHeight);
            _pickupTimeout = Mathf.Max(0.1f, _pickupTimeout);
            _pickupDistance = Mathf.Max(0.05f, _pickupDistance);
            _closeLerpSpeed = Mathf.Max(0f, _closeLerpSpeed);
            _impulseForce = Mathf.Max(0f, _impulseForce);
            _impulseUpwardForce = Mathf.Max(0f, _impulseUpwardForce);
            _switchToLerpDistance = Mathf.Max(0.1f, _switchToLerpDistance);
            _impulseDuration = Mathf.Max(0.05f, _impulseDuration);
        }
    }
}
