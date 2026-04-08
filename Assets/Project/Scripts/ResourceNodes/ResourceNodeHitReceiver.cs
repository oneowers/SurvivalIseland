// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeHitReceiver.cs
// Purpose: Adapts the generic player-hit contract onto the existing resource-node hit request pipeline.
// Dependencies: MessagePipe, UnityEngine, VContainer, ProjectResonance.ResourceNodes, ProjectResonance.PlayerCombat.

using MessagePipe;
using ProjectResonance.PlayerCombat;
using UnityEngine;
using VContainer;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Receives generic player hits and forwards them into the resource-node message pipeline.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Hit Receiver")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceNodeRuntime))]
    public sealed class ResourceNodeHitReceiver : MonoBehaviour, IPlayerHitReceiver
    {
        [SerializeField]
        private ResourceNodeRuntime _runtime;

        private IPublisher<ResourceHitRequestEvent> _resourceHitPublisher;

        /// <summary>
        /// Gets whether the resource node can currently receive hits.
        /// </summary>
        public bool CanReceiveHit => _runtime != null && !_runtime.IsDestroyed;

        [Inject]
        private void Construct(IPublisher<ResourceHitRequestEvent> resourceHitPublisher)
        {
            _resourceHitPublisher = resourceHitPublisher;
        }

        /// <summary>
        /// Forwards the current hit to the resource-node runtime.
        /// </summary>
        /// <param name="context">Resolved player-hit payload.</param>
        public void ReceiveHit(in PlayerHitContext context)
        {
            if (!CanReceiveHit || _resourceHitPublisher == null)
            {
                return;
            }

            _resourceHitPublisher.Publish(new ResourceHitRequestEvent(_runtime, ResolvePlanarHitDirection(context.HitDirection), Mathf.Max(1, context.Damage)));
        }

        private void Reset()
        {
            _runtime = GetComponent<ResourceNodeRuntime>();
        }

        private void OnValidate()
        {
            if (_runtime == null)
            {
                _runtime = GetComponent<ResourceNodeRuntime>();
            }
        }

        private Vector3 ResolvePlanarHitDirection(Vector3 hitDirection)
        {
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return hitDirection.normalized;
            }

            var fallbackDirection = transform.forward;
            fallbackDirection.y = 0f;
            return fallbackDirection.sqrMagnitude > Mathf.Epsilon ? fallbackDirection.normalized : Vector3.forward;
        }
    }
}
