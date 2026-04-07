// Path: Assets/Project/Scpripts/TreeFelling/ChopInteraction.cs
// Purpose: Converts player interaction input into chop events targeted at a tree.
// Dependencies: UniTask, MessagePipe, UnityEngine, VContainer, TreeConfig.

using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Interaction entry point for chopping a tree.
    /// </summary>
    public sealed class ChopInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private bool _enableDebugLogs = true;

        [SerializeField]
        private ChoppableTree _targetTree;

        [SerializeField]
        private Transform _hitOrigin;

        private IInventoryQuery _inventoryQuery;
        private IPublisher<ChopEvent> _chopPublisher;

        [Inject]
        private void Construct(
            IInventoryQuery inventoryQuery,
            IPublisher<ChopEvent> chopPublisher)
        {
            _inventoryQuery = inventoryQuery;
            _chopPublisher = chopPublisher;
        }

        /// <summary>
        /// Executes the light interaction, typically bound to the E key.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public UniTask InteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            PublishChop(context, allowBareHands: true);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Executes the heavy interaction, typically bound to LMB while an axe is equipped.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public UniTask HeavyInteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            PublishChop(context, allowBareHands: false);
            return UniTask.CompletedTask;
        }

        private void Reset()
        {
            _targetTree = GetComponent<ChoppableTree>();
            _hitOrigin = transform;
        }

        private void PublishChop(InteractionContext context, bool allowBareHands)
        {
            if (_targetTree == null || _targetTree.IsFalling)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[ChopInteraction] Chop ignored. TargetTreeAssigned={_targetTree != null}, IsFalling={(_targetTree != null && _targetTree.IsFalling)}", this);
                }

                return;
            }

            var axeTier = _inventoryQuery != null ? _inventoryQuery.GetEquippedAxeTier() : AxeTier.None;
            if (!allowBareHands && axeTier == AxeTier.None)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[ChopInteraction] Heavy interact ignored because no axe is equipped.", this);
                }

                return;
            }

            var damage = ResolveDamage(axeTier);
            var origin = ResolveHitOrigin(context);
            var hitDirection = _targetTree.transform.position - origin;
            hitDirection.y = 0f;

            // Falling back to the tree forward vector keeps the fall deterministic when the origin overlaps the trunk.
            if (hitDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                hitDirection = _targetTree.transform.forward;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[ChopInteraction] Publishing ChopEvent. AxeTier={axeTier}, Damage={damage}, Direction={hitDirection.normalized}", this);
            }

            _chopPublisher.Publish(new ChopEvent(_targetTree, hitDirection.normalized, damage));
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
    }
}
