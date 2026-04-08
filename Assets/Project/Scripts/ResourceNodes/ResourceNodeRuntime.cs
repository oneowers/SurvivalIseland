// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeRuntime.cs
// Purpose: Tracks runtime hit state for any authored resource node and coordinates generic damage, audio, and destroy events.
// Dependencies: MessagePipe, UnityEngine, VContainer, ProjectResonance.TreeFelling, ProjectResonance.ResourceNodes.

using System;
using MessagePipe;
using ProjectResonance.TreeFelling;
using UnityEngine;
using VContainer;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Runtime state for an authored breakable resource node.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Runtime")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceNodeAuthoring))]
    [RequireComponent(typeof(ResourceNodeHealth))]
    [RequireComponent(typeof(ResourceNodeInteraction))]
    public sealed class ResourceNodeRuntime : MonoBehaviour
    {
        [SerializeField]
        private bool _enableDebugLogs = true;

        [SerializeField]
        private Transform[] _hitPoints;

        private IPublisher<ResourceHitEvent> _resourceHitPublisher;
        private IPublisher<ResourceDestroyedEvent> _resourceDestroyedPublisher;
        private IPublisher<SoundEvent> _soundPublisher;
        private ISubscriber<ResourceHitRequestEvent> _hitRequestSubscriber;

        private IDisposable _hitRequestSubscription;
        private int _strikeCount;
        private int _lastHitPointIndex;
        private Vector3 _lastHitDirection;
        private bool _isDestroyed;
        private ResourceNodeInteraction _interaction;
        private ResourceNodeHealth _health;
        private ResourceNodeAuthoring _authoring;

        /// <summary>
        /// Gets the authored resource node data.
        /// </summary>
        public ResourceNodeAuthoring Authoring => _authoring;

        /// <summary>
        /// Gets the maximum health resolved for this node.
        /// </summary>
        public int MaxHealth => _health != null ? _health.MaxHealth : (_authoring != null ? _authoring.MaxHealth : 1);

        /// <summary>
        /// Gets the current remaining health.
        /// </summary>
        public int CurrentHealth => _health != null ? _health.CurrentHealth : MaxHealth;

        /// <summary>
        /// Gets the total amount of applied damage.
        /// </summary>
        public int CurrentHits => Mathf.Clamp(MaxHealth - CurrentHealth, 0, MaxHealth);

        /// <summary>
        /// Gets the accumulated planar hit direction.
        /// </summary>
        public Vector3 LastHitDirection => _lastHitDirection;

        /// <summary>
        /// Gets the hit point selected by the latest strike.
        /// </summary>
        public Transform LastHitPoint => ResolveHitPoint(_lastHitPointIndex);

        /// <summary>
        /// Gets whether this node has already entered its destroy flow.
        /// </summary>
        public bool IsDestroyed => _isDestroyed || (_health != null && _health.IsDestroyed);

        [Inject]
        private void Construct(
            IPublisher<ResourceHitEvent> resourceHitPublisher,
            IPublisher<ResourceDestroyedEvent> resourceDestroyedPublisher,
            IPublisher<SoundEvent> soundPublisher,
            ISubscriber<ResourceHitRequestEvent> hitRequestSubscriber)
        {
            _resourceHitPublisher = resourceHitPublisher;
            _resourceDestroyedPublisher = resourceDestroyedPublisher;
            _soundPublisher = soundPublisher;
            _hitRequestSubscriber = hitRequestSubscriber;
        }

        /// <summary>
        /// Applies incoming hit damage to the resource node.
        /// </summary>
        /// <param name="hitDirection">World-space hit direction.</param>
        /// <param name="damage">Incoming damage amount.</param>
        public void ReceiveHit(Vector3 hitDirection, int damage)
        {
            if (IsDestroyed || damage <= 0 || _health == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[ResourceNodeRuntime] Hit ignored. IsDestroyed={IsDestroyed}, HasHealth={_health != null}, Damage={damage}", this);
                }

                return;
            }

            _strikeCount++;
            _lastHitPointIndex = Mathf.Max(0, _strikeCount - 1);
            _lastHitDirection += ResolvePlanarHitDirection(hitDirection).normalized * damage;

            var hitSound = ResolveHitSound(_strikeCount - 1);
            if (hitSound != null)
            {
                _soundPublisher.Publish(new SoundEvent("resource_hit", transform.position, hitSound));
            }

            var damageResult = _health.TakeDamage(damage);

            if (_enableDebugLogs)
            {
                Debug.Log($"[ResourceNodeRuntime] ReceiveHit. Damage={damage}, CurrentHealth={damageResult.CurrentHealth}/{damageResult.MaxHealth}, StrikeCount={_strikeCount}, LastHitDirection={_lastHitDirection}", this);
            }

            _resourceHitPublisher.Publish(new ResourceHitEvent(
                this,
                damageResult.CurrentHealth,
                damageResult.MaxHealth,
                _strikeCount,
                _lastHitDirection,
                LastHitPoint));

            if (!damageResult.WasDestroyed)
            {
                return;
            }

            _isDestroyed = true;

            if (_interaction != null)
            {
                _interaction.enabled = false;
            }

            var breakSound = ResolveBreakSound();
            if (breakSound != null)
            {
                _soundPublisher.Publish(new SoundEvent("resource_break", transform.position, breakSound));
            }

            _resourceDestroyedPublisher.Publish(new ResourceDestroyedEvent(this, _health.DropItemDefinition, _health.DropCount));
        }

        /// <summary>
        /// Resets runtime strike state and restores node health.
        /// </summary>
        public void ResetState()
        {
            _strikeCount = 0;
            _lastHitPointIndex = 0;
            _lastHitDirection = Vector3.zero;
            _isDestroyed = false;

            if (_health != null)
            {
                _health.ResetHealth();
            }

            if (_interaction != null)
            {
                _interaction.enabled = true;
            }
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (_hitRequestSubscriber != null)
            {
                _hitRequestSubscription = _hitRequestSubscriber.Subscribe(OnResourceHitRequested);
            }
        }

        private void OnDisable()
        {
            _hitRequestSubscription?.Dispose();
            _hitRequestSubscription = null;
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnResourceHitRequested(ResourceHitRequestEvent message)
        {
            if (message.Target != this)
            {
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[ResourceNodeRuntime] ResourceHitRequestEvent received. Damage={message.Damage}, Direction={message.HitDirection}", this);
            }

            ReceiveHit(message.HitDirection, message.Damage);
        }

        private Transform ResolveHitPoint(int index)
        {
            if (_hitPoints == null || _hitPoints.Length == 0)
            {
                return transform;
            }

            var clampedIndex = Mathf.Abs(index) % _hitPoints.Length;
            return _hitPoints[clampedIndex] != null ? _hitPoints[clampedIndex] : transform;
        }

        private AudioClip ResolveHitSound(int strikeIndex)
        {
            return _authoring != null ? _authoring.GetHitSound(strikeIndex) : null;
        }

        private AudioClip ResolveBreakSound()
        {
            return _authoring != null ? _authoring.BreakSound : null;
        }

        private void EnsureReferences()
        {
            if (_interaction == null)
            {
                _interaction = GetComponent<ResourceNodeInteraction>();
            }

            if (_health == null)
            {
                _health = GetComponent<ResourceNodeHealth>();
            }

            if (_authoring == null)
            {
                _authoring = GetComponent<ResourceNodeAuthoring>();
            }
        }

        private Vector3 ResolvePlanarHitDirection(Vector3 hitDirection)
        {
            var planarDirection = hitDirection;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return planarDirection;
            }

            var fallbackDirection = transform.forward;
            fallbackDirection.y = 0f;
            return fallbackDirection.sqrMagnitude > Mathf.Epsilon ? fallbackDirection : Vector3.forward;
        }
    }
}
