// Path: Assets/Project/Scpripts/TreeFelling/TreeDecalSystem.cs
// Purpose: Reuses URP decal projectors to render progressive chop marks on hit trees.
// Dependencies: MessagePipe, UnityEngine.Pool, VContainer.

using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePipe;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Places and strengthens chopping decals on tree trunks.
    /// </summary>
    public sealed class TreeDecalSystem : IStartable, IDisposable
    {
        private readonly ISubscriber<TreeHitEvent> _treeHitSubscriber;

        private readonly Dictionary<int, ObjectPool<GameObject>> _poolByPrefabId = new Dictionary<int, ObjectPool<GameObject>>();
        private readonly Dictionary<Transform, GameObject> _decalByChopPoint = new Dictionary<Transform, GameObject>();

        private IDisposable _treeHitSubscription;

        /// <summary>
        /// Initializes the decal system.
        /// </summary>
        /// <param name="treeHitSubscriber">Tree hit event subscriber.</param>
        public TreeDecalSystem(ISubscriber<TreeHitEvent> treeHitSubscriber)
        {
            _treeHitSubscriber = treeHitSubscriber;
        }

        /// <summary>
        /// Starts listening for tree hit events.
        /// </summary>
        public void Start()
        {
            _treeHitSubscription = _treeHitSubscriber.Subscribe(OnTreeHit);
        }

        /// <summary>
        /// Disposes subscriptions and active decals.
        /// </summary>
        public void Dispose()
        {
            _treeHitSubscription?.Dispose();
            _treeHitSubscription = null;

            foreach (var pair in _decalByChopPoint)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            _decalByChopPoint.Clear();

            foreach (var pool in _poolByPrefabId.Values)
            {
                pool.Dispose();
            }

            _poolByPrefabId.Clear();
        }

        private void OnTreeHit(TreeHitEvent message)
        {
            if (message.Tree == null || message.Tree.Config == null)
            {
                return;
            }

            var treeConfig = message.Tree.Config;
            var decalPrefab = treeConfig.DecalPrefab;
            var chopPoint = message.Tree.LastChopPoint;

            if (decalPrefab == null || chopPoint == null)
            {
                return;
            }

            if (!_decalByChopPoint.TryGetValue(chopPoint, out var decal) || decal == null)
            {
                decal = GetOrCreatePool(decalPrefab, treeConfig.DecalPoolCapacity).Get();
                _decalByChopPoint[chopPoint] = decal;
            }

            // Parenting to the chop point keeps the decal attached while the whole trunk rotates during the fall.
            decal.transform.SetParent(chopPoint, false);
            decal.transform.localPosition = Vector3.zero;
            decal.transform.localRotation = Quaternion.identity;
            ApplyDecalSize(decal, treeConfig.DecalSize);

            var damageProgress = message.Tree.MaxHits > 0
                ? 1f - (message.HitsRemaining / (float)message.Tree.MaxHits)
                : 1f;

            ApplyDecalOpacity(decal, Mathf.Lerp(treeConfig.MinDecalOpacity, treeConfig.MaxDecalOpacity, damageProgress));
            decal.SetActive(true);
        }

        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab, int poolCapacity)
        {
            var prefabId = prefab.GetInstanceID();
            if (_poolByPrefabId.TryGetValue(prefabId, out var existingPool))
            {
                return existingPool;
            }

            var capacity = Mathf.Max(1, poolCapacity);
            var createdPool = new ObjectPool<GameObject>(
                () => CreateDecalObject(prefab),
                OnTakeDecalObject,
                OnReturnDecalObject,
                OnDestroyDecalObject,
                false,
                capacity,
                capacity * 4);

            _poolByPrefabId.Add(prefabId, createdPool);
            return createdPool;
        }

        private static GameObject CreateDecalObject(GameObject prefab)
        {
            var decal = UnityEngine.Object.Instantiate(prefab);
            decal.SetActive(false);
            return decal;
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

            // Scale fallback keeps the system usable even before URP decal components are added to the prefab.
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
