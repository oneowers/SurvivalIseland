// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeHealth.cs
// Purpose: Owns health, destroy flow, and universal pickup spawning for any authored resource node.
// Dependencies: System, UnityEngine, VContainer, ProjectResonance.Inventory, ProjectResonance.TreeDrops, ProjectResonance.ResourceNodes.

using System;
using ProjectResonance.Inventory;
using ProjectResonance.TreeDrops;
using UnityEngine;
using VContainer;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Immutable result of applying damage to a resource node.
    /// </summary>
    public readonly struct ResourceNodeDamageResult
    {
        /// <summary>
        /// Creates a new resource-node damage result.
        /// </summary>
        /// <param name="currentHealth">Remaining health after the hit.</param>
        /// <param name="maxHealth">Maximum health of the node.</param>
        /// <param name="wasDestroyed">True when the node entered its destroy flow.</param>
        public ResourceNodeDamageResult(int currentHealth, int maxHealth, bool wasDestroyed)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            WasDestroyed = wasDestroyed;
        }

        /// <summary>
        /// Gets the remaining node health.
        /// </summary>
        public int CurrentHealth { get; }

        /// <summary>
        /// Gets the maximum node health.
        /// </summary>
        public int MaxHealth { get; }

        /// <summary>
        /// Gets whether the node was destroyed by the hit.
        /// </summary>
        public bool WasDestroyed { get; }
    }

    /// <summary>
    /// Health controller for a breakable resource node.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Health")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceNodeAuthoring))]
    public sealed class ResourceNodeHealth : MonoBehaviour
    {
        [SerializeField]
        private ItemPickup _itemPickupPrefab;

        [SerializeField]
        private Transform _playerTarget;

        [SerializeField]
        [Min(0.1f)]
        private float _dropRadius = 0.75f;

        [SerializeField]
        private float _spawnHeightOffset = 0.15f;

        private int _currentHealth;
        private bool _isDestroyed;
        private ItemPickupPoolService _itemPickupPoolService;
        private ResourceNodeAuthoring _authoring;

        /// <summary>
        /// Fired when the node reaches zero health.
        /// </summary>
        public event Action<ResourceNodeHealth> Destroyed;

        /// <summary>
        /// Gets the current remaining health.
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// Gets the configured maximum health.
        /// </summary>
        public int MaxHealth => ResolveConfiguredMaxHealth();

        /// <summary>
        /// Gets the configured drop amount.
        /// </summary>
        public int DropCount => ResolveConfiguredDropCount();

        /// <summary>
        /// Gets the configured drop item.
        /// </summary>
        public ItemDefinition DropItemDefinition => ResolveDropItemDefinition();

        /// <summary>
        /// Gets whether the node has already been destroyed.
        /// </summary>
        public bool IsDestroyed => _isDestroyed;

        [Inject]
        private void Construct(ItemPickupPoolService itemPickupPoolService)
        {
            _itemPickupPoolService = itemPickupPoolService;
        }

        private void Awake()
        {
            EnsureReferences();
            ResetHealth();
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
            _dropRadius = Mathf.Max(0.1f, _dropRadius);
        }

        /// <summary>
        /// Applies damage to the node and runs destroy flow when health reaches zero.
        /// </summary>
        /// <param name="damage">Incoming damage amount.</param>
        /// <returns>Immutable result of the applied damage.</returns>
        public ResourceNodeDamageResult TakeDamage(int damage)
        {
            if (_isDestroyed || damage <= 0)
            {
                return new ResourceNodeDamageResult(_currentHealth, MaxHealth, _isDestroyed);
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            if (_currentHealth > 0)
            {
                return new ResourceNodeDamageResult(_currentHealth, MaxHealth, false);
            }

            HandleNodeDestroyed();
            return new ResourceNodeDamageResult(_currentHealth, MaxHealth, true);
        }

        /// <summary>
        /// Restores this health component to its configured maximum value.
        /// </summary>
        public void ResetHealth()
        {
            _isDestroyed = false;
            _currentHealth = Mathf.Max(1, ResolveConfiguredMaxHealth());
        }

        /// <summary>
        /// Forces the node to enter its destroy flow immediately.
        /// </summary>
        public void DestroyNodeImmediately()
        {
            if (_isDestroyed)
            {
                return;
            }

            _currentHealth = 0;
            HandleNodeDestroyed();
        }

        private void HandleNodeDestroyed()
        {
            _isDestroyed = true;

            SpawnDrops();
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }

        private void SpawnDrops()
        {
            var dropItemDefinition = ResolveDropItemDefinition();
            var dropCount = ResolveConfiguredDropCount();
            var hasPickupSource = _itemPickupPoolService != null || _itemPickupPrefab != null;
            if (!hasPickupSource || dropItemDefinition == null || dropCount <= 0)
            {
                return;
            }

            var target = _playerTarget != null ? _playerTarget : ResolvePlayerTarget();

            for (var index = 0; index < dropCount; index++)
            {
                var spawnPosition = CalculateSpawnPosition();
                var itemPickup = SpawnPickupInstance(spawnPosition);
                if (itemPickup == null)
                {
                    continue;
                }

                itemPickup.Configure(dropItemDefinition, 1, target);
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            var randomOffset = UnityEngine.Random.insideUnitCircle * _dropRadius;
            return transform.position + new Vector3(randomOffset.x, _spawnHeightOffset, randomOffset.y);
        }

        private Transform ResolvePlayerTarget()
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            return playerObject != null ? playerObject.transform : null;
        }

        private int ResolveConfiguredMaxHealth()
        {
            return _authoring != null && _authoring.Definition != null
                ? Mathf.Max(1, _authoring.MaxHealth)
                : 1;
        }

        private int ResolveConfiguredDropCount()
        {
            return _authoring != null && _authoring.Definition != null
                ? Mathf.Max(0, _authoring.DropCount)
                : 0;
        }

        private ItemDefinition ResolveDropItemDefinition()
        {
            return _authoring != null && _authoring.Definition != null
                ? _authoring.DropItemDefinition
                : null;
        }

        private void EnsureReferences()
        {
            if (_authoring == null)
            {
                _authoring = GetComponent<ResourceNodeAuthoring>();
            }
        }

        private ItemPickup SpawnPickupInstance(Vector3 spawnPosition)
        {
            if (_itemPickupPoolService != null)
            {
                return _itemPickupPoolService.Get(spawnPosition, Quaternion.identity, _itemPickupPrefab);
            }

            return _itemPickupPrefab != null
                ? Instantiate(_itemPickupPrefab, spawnPosition, Quaternion.identity)
                : null;
        }
    }
}
