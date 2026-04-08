// Path: Assets/Project/Scpripts/ResourceNodes/ResourceDecalSystem.cs
// Purpose: Reuses pooled decal prefabs to render hit marks on any resource node type, including trees, rocks, and coal.
// Dependencies: MessagePipe, UnityEngine.Pool, VContainer.

using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePipe;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Event used to request a hit decal update for a resource node.
    /// </summary>
    public readonly struct ResourceDecalEvent
    {
        /// <summary>
        /// Creates a new resource decal event.
        /// </summary>
        /// <param name="hitAnchor">Transform the decal should follow.</param>
        /// <param name="decalPrefab">Prefab used to render the decal.</param>
        /// <param name="decalSize">Size applied to the decal.</param>
        /// <param name="minOpacity">Minimum opacity at the start of damage.</param>
        /// <param name="maxOpacity">Maximum opacity near destruction.</param>
        /// <param name="poolCapacity">Initial pool capacity for this decal prefab.</param>
        /// <param name="hitsRemaining">Remaining durability after the hit.</param>
        /// <param name="maxHits">Total durability of the node.</param>
        public ResourceDecalEvent(
            Transform hitAnchor,
            GameObject decalPrefab,
            Vector3 decalSize,
            float minOpacity,
            float maxOpacity,
            int poolCapacity,
            int hitsRemaining,
            int maxHits)
        {
            HitAnchor = hitAnchor;
            DecalPrefab = decalPrefab;
            DecalSize = decalSize;
            MinOpacity = minOpacity;
            MaxOpacity = maxOpacity;
            PoolCapacity = poolCapacity;
            HitsRemaining = hitsRemaining;
            MaxHits = maxHits;
        }

        /// <summary>
        /// Gets the transform the decal should follow.
        /// </summary>
        public Transform HitAnchor { get; }

        /// <summary>
        /// Gets the decal prefab to render.
        /// </summary>
        public GameObject DecalPrefab { get; }

        /// <summary>
        /// Gets the size applied to the decal instance.
        /// </summary>
        public Vector3 DecalSize { get; }

        /// <summary>
        /// Gets the minimum opacity for the decal.
        /// </summary>
        public float MinOpacity { get; }

        /// <summary>
        /// Gets the maximum opacity for the decal.
        /// </summary>
        public float MaxOpacity { get; }

        /// <summary>
        /// Gets the initial pool capacity for this decal prefab.
        /// </summary>
        public int PoolCapacity { get; }

        /// <summary>
        /// Gets the remaining durability after the hit.
        /// </summary>
        public int HitsRemaining { get; }

        /// <summary>
        /// Gets the total durability of the node.
        /// </summary>
        public int MaxHits { get; }
    }

    /// <summary>
    /// Handles pooled hit decals for authored resource nodes.
    /// </summary>
    public sealed class ResourceDecalSystem : IStartable, IDisposable
    {
        private readonly ISubscriber<ResourceDecalEvent> _resourceDecalSubscriber;

        private readonly Dictionary<int, ObjectPool<GameObject>> _poolByPrefabId = new Dictionary<int, ObjectPool<GameObject>>();
        private readonly Dictionary<Transform, ActiveDecal> _activeDecalByAnchor = new Dictionary<Transform, ActiveDecal>();

        private IDisposable _resourceDecalSubscription;

        private struct ActiveDecal
        {
            public int PrefabId;
            public GameObject Instance;
        }

        /// <summary>
        /// Creates the resource decal system.
        /// </summary>
        /// <param name="resourceDecalSubscriber">Subscriber for resource decal events.</param>
        public ResourceDecalSystem(ISubscriber<ResourceDecalEvent> resourceDecalSubscriber)
        {
            _resourceDecalSubscriber = resourceDecalSubscriber;
        }

        /// <summary>
        /// Starts listening for resource decal events.
        /// </summary>
        public void Start()
        {
            _resourceDecalSubscription = _resourceDecalSubscriber.Subscribe(OnResourceDecal);
        }

        /// <summary>
        /// Releases active decals and pooled instances.
        /// </summary>
        public void Dispose()
        {
            _resourceDecalSubscription?.Dispose();
            _resourceDecalSubscription = null;

            foreach (var pair in _activeDecalByAnchor)
            {
                ReleaseActiveDecal(pair.Value);
            }

            _activeDecalByAnchor.Clear();

            foreach (var pool in _poolByPrefabId.Values)
            {
                pool.Dispose();
            }

            _poolByPrefabId.Clear();
        }

        private void OnResourceDecal(ResourceDecalEvent message)
        {
            if (message.HitAnchor == null || message.DecalPrefab == null)
            {
                return;
            }

            var prefabId = message.DecalPrefab.GetInstanceID();
            if (!_activeDecalByAnchor.TryGetValue(message.HitAnchor, out var activeDecal) ||
                activeDecal.Instance == null ||
                activeDecal.PrefabId != prefabId)
            {
                if (activeDecal.Instance != null)
                {
                    ReleaseActiveDecal(activeDecal);
                }

                activeDecal = new ActiveDecal
                {
                    PrefabId = prefabId,
                    Instance = GetOrCreatePool(message.DecalPrefab, message.PoolCapacity).Get(),
                };

                _activeDecalByAnchor[message.HitAnchor] = activeDecal;
            }

            var decalInstance = activeDecal.Instance;
            if (decalInstance == null)
            {
                return;
            }

            // Parenting the decal to the authored hit anchor keeps the mark aligned while the resource mesh moves or falls.
            decalInstance.transform.SetParent(message.HitAnchor, false);
            decalInstance.transform.localPosition = Vector3.zero;
            decalInstance.transform.localRotation = Quaternion.identity;
            ApplyDecalSize(decalInstance, message.DecalSize);

            var normalizedDamage = message.MaxHits > 0
                ? 1f - (Mathf.Clamp(message.HitsRemaining, 0, message.MaxHits) / (float)message.MaxHits)
                : 1f;

            ApplyDecalOpacity(decalInstance, Mathf.Lerp(message.MinOpacity, message.MaxOpacity, normalizedDamage));
            decalInstance.SetActive(true);
        }

        private void ReleaseActiveDecal(ActiveDecal activeDecal)
        {
            if (activeDecal.Instance == null)
            {
                return;
            }

            if (_poolByPrefabId.TryGetValue(activeDecal.PrefabId, out var pool))
            {
                pool.Release(activeDecal.Instance);
                return;
            }

            UnityEngine.Object.Destroy(activeDecal.Instance);
        }

        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab, int poolCapacity)
        {
            var prefabId = prefab.GetInstanceID();
            if (_poolByPrefabId.TryGetValue(prefabId, out var existingPool))
            {
                return existingPool;
            }

            var clampedCapacity = Mathf.Max(1, poolCapacity);
            var createdPool = new ObjectPool<GameObject>(
                () => CreateDecalObject(prefab),
                OnTakeDecalObject,
                OnReturnDecalObject,
                OnDestroyDecalObject,
                false,
                clampedCapacity,
                clampedCapacity * 4);

            _poolByPrefabId.Add(prefabId, createdPool);
            return createdPool;
        }

        private static GameObject CreateDecalObject(GameObject prefab)
        {
            var decalObject = UnityEngine.Object.Instantiate(prefab);
            decalObject.SetActive(false);
            return decalObject;
        }

        private static void OnTakeDecalObject(GameObject decalObject)
        {
            if (decalObject != null)
            {
                decalObject.SetActive(true);
            }
        }

        private static void OnReturnDecalObject(GameObject decalObject)
        {
            if (decalObject != null)
            {
                decalObject.transform.SetParent(null, false);
                decalObject.SetActive(false);
            }
        }

        private static void OnDestroyDecalObject(GameObject decalObject)
        {
            if (decalObject != null)
            {
                UnityEngine.Object.Destroy(decalObject);
            }
        }

        private static void ApplyDecalSize(GameObject decalObject, Vector3 size)
        {
            var projector = FindDecalProjectorComponent(decalObject);
            if (projector != null && TrySetProperty(projector, "size", size))
            {
                return;
            }

            // The mesh-scale fallback keeps authored decal prefabs usable even when URP/HDRP projector components are absent.
            decalObject.transform.localScale = size;
        }

        private static void ApplyDecalOpacity(GameObject decalObject, float opacity)
        {
            var projector = FindDecalProjectorComponent(decalObject);
            if (projector != null && TrySetProperty(projector, "fadeFactor", opacity))
            {
                return;
            }

            var renderers = decalObject.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);

                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    var color = renderer.sharedMaterial.color;
                    color.a = opacity;
                    propertyBlock.SetColor("_BaseColor", color);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private static Component FindDecalProjectorComponent(GameObject decalObject)
        {
            var decalProjectorType = Type.GetType("UnityEngine.Rendering.Universal.DecalProjector, Unity.RenderPipelines.Universal.Runtime");
            if (decalProjectorType == null)
            {
                decalProjectorType = Type.GetType("UnityEngine.Rendering.HighDefinition.DecalProjector, Unity.RenderPipelines.HighDefinition.Runtime");
            }

            return decalProjectorType != null ? decalObject.GetComponentInChildren(decalProjectorType, true) : null;
        }

        private static bool TrySetProperty(Component target, string propertyName, object value)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(target, value);
            return true;
        }
    }
}
