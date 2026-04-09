// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeInteraction.cs
// Purpose: Converts player interaction input into direct resource hits for any authored resource node.
// Dependencies: UniTask, UnityEngine, VContainer, ProjectResonance.TreeFelling, ProjectResonance.ResourceNodes.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.Inventory;
using ProjectResonance.TreeFelling;
using UnityEngine;
using VContainer;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Interaction entry point for damaging a resource node.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Interaction")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceNodeRuntime))]
    public sealed class ResourceNodeInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Transform _hitOrigin;

        private IInventoryQuery _inventoryQuery;
        private EquippedToolDurabilityService _equippedToolDurabilityService;
        private ResourceNodeRuntime _targetNode;

        [Inject]
        private void Construct(
            IInventoryQuery inventoryQuery,
            EquippedToolDurabilityService equippedToolDurabilityService)
        {
            _inventoryQuery = inventoryQuery;
            _equippedToolDurabilityService = equippedToolDurabilityService;
        }

        /// <summary>
        /// Executes the default interaction.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed async handle.</returns>
        public UniTask InteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            PublishHitRequest(context, allowBareHands: true);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Executes the heavy interaction variant.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed async handle.</returns>
        public UniTask HeavyInteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            PublishHitRequest(context, allowBareHands: false);
            return UniTask.CompletedTask;
        }

        private void Reset()
        {
            EnsureReferences();
            _hitOrigin = transform;
        }

        private void PublishHitRequest(InteractionContext context, bool allowBareHands)
        {
            EnsureReferences();

            if (_targetNode == null || _targetNode.IsDestroyed)
            {
                return;
            }

            var axeTier = _inventoryQuery != null ? _inventoryQuery.GetEquippedAxeTier() : AxeTier.None;
            if (!allowBareHands && axeTier == AxeTier.None)
            {
                return;
            }

            var damage = ResolveDamage(axeTier);
            var origin = ResolveHitOrigin(context);
            var hitDirection = _targetNode.transform.position - origin;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                hitDirection = _targetNode.transform.forward;
            }

            _targetNode.ReceiveHit(hitDirection.normalized, damage);
            _equippedToolDurabilityService?.TryConsumeEquippedToolDurability(_targetNode.name, 1);
        }

        private Vector3 ResolveHitOrigin(InteractionContext context)
        {
            if (context.Interactor != null)
            {
                return context.Interactor.position;
            }

            if (_hitOrigin != null)
            {
                return _hitOrigin.position;
            }

            return context.Origin;
        }

        private static int ResolveDamage(AxeTier axeTier)
        {
            switch (axeTier)
            {
                case AxeTier.Stone:
                    return 2;
                case AxeTier.Iron:
                    return 3;
                default:
                    return 1;
            }
        }

        private void EnsureReferences()
        {
            if (_targetNode == null)
            {
                _targetNode = GetComponent<ResourceNodeRuntime>();
            }
        }
    }
}
