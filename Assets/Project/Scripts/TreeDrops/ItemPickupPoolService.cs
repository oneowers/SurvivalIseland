// Path: Assets/Project/Scripts/TreeDrops/ItemPickupPoolService.cs
// Purpose: Owns pooled universal pickup instances and provides DI-safe spawn/release APIs for all resource drops.
// Dependencies: System, Collections.Generic, UnityEngine.Pool, VContainer, ItemPickup, WorldItemPickupConfig.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.TreeDrops
{
    /// <summary>
    /// Shared DI service that pools universal world item pickups.
    /// </summary>
    public sealed class ItemPickupPoolService : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly WorldItemPickupConfig _pickupConfig;
        private readonly Dictionary<ItemPickup, ObjectPool<ItemPickup>> _poolByPrefab;
        private readonly Dictionary<ItemPickup, ObjectPool<ItemPickup>> _ownerPoolByInstance;

        /// <summary>
        /// Creates a new pickup pool service.
        /// </summary>
        /// <param name="resolver">Resolver used for DI-aware prefab instantiation.</param>
        /// <param name="pickupConfig">Optional shared pickup config.</param>
        public ItemPickupPoolService(IObjectResolver resolver, WorldItemPickupConfig pickupConfig = null)
        {
            _resolver = resolver;
            _pickupConfig = pickupConfig;
            _poolByPrefab = new Dictionary<ItemPickup, ObjectPool<ItemPickup>>();
            _ownerPoolByInstance = new Dictionary<ItemPickup, ObjectPool<ItemPickup>>();
        }

        /// <summary>
        /// Gets a pooled pickup instance ready to be configured and spawned.
        /// </summary>
        /// <param name="position">Spawn position.</param>
        /// <param name="rotation">Spawn rotation.</param>
        /// <param name="prefabOverride">Optional prefab override used when the shared config has no prefab assigned.</param>
        /// <returns>Pooled pickup instance, or null when no pickup prefab could be resolved.</returns>
        public ItemPickup Get(Vector3 position, Quaternion rotation, ItemPickup prefabOverride = null)
        {
            var pickupPrefab = ResolvePickupPrefab(prefabOverride);
            if (pickupPrefab == null)
            {
                Debug.LogWarning("[ItemPickupPoolService] Failed to resolve a pickup prefab. Assign WorldItemPickupConfig.PickupPrefab or provide a prefab override.");
                return null;
            }

            var pool = ResolvePool(pickupPrefab);
            var pickup = pool.Get();
            _ownerPoolByInstance[pickup] = pool;
            pickup.transform.SetPositionAndRotation(position, rotation);
            pickup.ApplyConfig(_pickupConfig);
            return pickup;
        }

        /// <summary>
        /// Releases a pickup instance back into its owning pool.
        /// </summary>
        /// <param name="pickup">Pickup instance to release.</param>
        public void Release(ItemPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            if (_ownerPoolByInstance.TryGetValue(pickup, out var ownerPool))
            {
                ownerPool.Release(pickup);
                return;
            }

            pickup.ResetStateForPool();
            pickup.gameObject.SetActive(false);
        }

        /// <summary>
        /// Disposes all pooled pickups.
        /// </summary>
        public void Dispose()
        {
            foreach (var pool in _poolByPrefab.Values)
            {
                pool.Dispose();
            }

            _poolByPrefab.Clear();
            _ownerPoolByInstance.Clear();
        }

        private ObjectPool<ItemPickup> ResolvePool(ItemPickup pickupPrefab)
        {
            if (_poolByPrefab.TryGetValue(pickupPrefab, out var existingPool))
            {
                return existingPool;
            }

            var initialCapacity = _pickupConfig != null ? _pickupConfig.InitialPoolCapacity : 1;
            var maxPoolSize = _pickupConfig != null ? _pickupConfig.MaxPoolSize : initialCapacity;

            var pool = new ObjectPool<ItemPickup>(
                () => CreatePooledPickup(pickupPrefab),
                OnTakeFromPool,
                OnReturnToPool,
                OnDestroyPooledPickup,
                false,
                initialCapacity,
                maxPoolSize);

            _poolByPrefab.Add(pickupPrefab, pool);
            return pool;
        }

        private ItemPickup CreatePooledPickup(ItemPickup pickupPrefab)
        {
            var instance = _resolver != null
                ? _resolver.Instantiate(pickupPrefab, (Transform)null)
                : UnityEngine.Object.Instantiate(pickupPrefab);

            instance.ApplyConfig(_pickupConfig);
            instance.ResetStateForPool();
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnTakeFromPool(ItemPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            pickup.gameObject.SetActive(true);
            pickup.ApplyConfig(_pickupConfig);
            pickup.ResetStateForSpawn();
        }

        private void OnReturnToPool(ItemPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            _ownerPoolByInstance.Remove(pickup);
            pickup.ResetStateForPool();
            pickup.transform.SetParent(null, false);
            pickup.gameObject.SetActive(false);
        }

        private void OnDestroyPooledPickup(ItemPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            _ownerPoolByInstance.Remove(pickup);
            UnityEngine.Object.Destroy(pickup.gameObject);
        }

        private ItemPickup ResolvePickupPrefab(ItemPickup prefabOverride)
        {
            if (_pickupConfig != null && _pickupConfig.PickupPrefab != null)
            {
                return _pickupConfig.PickupPrefab;
            }

            return prefabOverride;
        }
    }
}
