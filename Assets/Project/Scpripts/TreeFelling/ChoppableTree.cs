// Path: Assets/Project/Scpripts/TreeFelling/ChoppableTree.cs
// Purpose: Receives chop damage, tracks strike state and publishes tree hit / fall messages.
// Dependencies: MessagePipe, UnityEngine, VContainer, TreeConfig.

using System;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Runtime tree state that can be chopped down by the player.
    /// </summary>
    public sealed class ChoppableTree : MonoBehaviour
    {
        [SerializeField]
        private bool _enableDebugLogs = true;

        [SerializeField]
        private TreeConfig _treeConfig;

        [SerializeField]
        private Transform[] _chopPoints;

        [SerializeField]
        private ChopInteraction _chopInteraction;

        private IPublisher<TreeHitEvent> _treeHitPublisher;
        private IPublisher<TreeFallStartEvent> _treeFallStartPublisher;
        private IPublisher<SoundEvent> _soundPublisher;
        private ISubscriber<ChopEvent> _chopSubscriber;

        private IDisposable _chopSubscription;
        private int _currentHits;
        private int _strikeCount;
        private int _lastChopPointIndex;
        private Vector3 _lastHitDirection;
        private bool _isFalling;

        /// <summary>
        /// Gets the configured maximum durability for this tree.
        /// </summary>
        public int MaxHits => _treeConfig != null ? _treeConfig.MaxHits : 0;

        /// <summary>
        /// Gets the current accumulated hit damage.
        /// </summary>
        public int CurrentHits => _currentHits;

        /// <summary>
        /// Gets the configured chop points used for decal placement.
        /// </summary>
        public Transform[] ChopPoints => _chopPoints;

        /// <summary>
        /// Gets the accumulated world-space hit direction.
        /// </summary>
        public Vector3 LastHitDirection => _lastHitDirection;

        /// <summary>
        /// Gets the tree config asset.
        /// </summary>
        public TreeConfig Config => _treeConfig;

        /// <summary>
        /// Gets the chop point used by the latest strike.
        /// </summary>
        public Transform LastChopPoint => ResolveChopPoint(_lastChopPointIndex);

        /// <summary>
        /// Gets whether the tree has already entered the fall sequence.
        /// </summary>
        public bool IsFalling => _isFalling;

        [Inject]
        private void Construct(
            IPublisher<TreeHitEvent> treeHitPublisher,
            IPublisher<TreeFallStartEvent> treeFallStartPublisher,
            IPublisher<SoundEvent> soundPublisher,
            ISubscriber<ChopEvent> chopSubscriber)
        {
            _treeHitPublisher = treeHitPublisher;
            _treeFallStartPublisher = treeFallStartPublisher;
            _soundPublisher = soundPublisher;
            _chopSubscriber = chopSubscriber;
        }

        /// <summary>
        /// Applies a chop hit to the tree.
        /// </summary>
        /// <param name="hitDirection">World-space hit direction.</param>
        /// <param name="damage">Damage dealt by this hit.</param>
        public void ReceiveHit(Vector3 hitDirection, int damage)
        {
            if (_isFalling || _treeConfig == null || damage <= 0)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[ChoppableTree] Hit ignored. IsFalling={_isFalling}, HasConfig={_treeConfig != null}, Damage={damage}", this);
                }

                return;
            }

            _strikeCount++;
            _lastChopPointIndex = Mathf.Max(0, _strikeCount - 1);

            // The fall direction uses only the ground plane so slopes do not twist the trunk upward.
            var planarDirection = hitDirection;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection = transform.forward;
                planarDirection.y = 0f;
            }

            _lastHitDirection += planarDirection.normalized * damage;
            _currentHits = Mathf.Min(MaxHits, _currentHits + damage);

            if (_enableDebugLogs)
            {
                Debug.Log($"[ChoppableTree] ReceiveHit. Damage={damage}, CurrentHits={_currentHits}/{MaxHits}, LastHitDirection={_lastHitDirection}", this);
            }

            var hitSound = _treeConfig.GetHitSound(_strikeCount - 1);
            if (hitSound != null)
            {
                _soundPublisher.Publish(new SoundEvent("tree_hit", transform.position, hitSound));
            }

            var hitsRemaining = Mathf.Max(0, MaxHits - _currentHits);
            _treeHitPublisher.Publish(new TreeHitEvent(this, hitsRemaining));

            if (_currentHits < MaxHits)
            {
                return;
            }

            _isFalling = true;

            if (_enableDebugLogs)
            {
                Debug.Log($"[ChoppableTree] Fall threshold reached. Publishing TreeFallStartEvent. FallDirection={ResolveFallDirection()}", this);
            }

            if (_chopInteraction != null)
            {
                _chopInteraction.enabled = false;
            }

            if (_treeConfig.CrackSound != null)
            {
                _soundPublisher.Publish(new SoundEvent("tree_crack", transform.position, _treeConfig.CrackSound));
            }

            _treeFallStartPublisher.Publish(new TreeFallStartEvent(this));
        }

        /// <summary>
        /// Resolves the normalized fall direction from accumulated hits.
        /// </summary>
        /// <returns>World-space planar fall direction.</returns>
        public Vector3 ResolveFallDirection()
        {
            var planarDirection = _lastHitDirection;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection = transform.forward;
                planarDirection.y = 0f;
            }

            return planarDirection.normalized;
        }

        /// <summary>
        /// Resets the runtime chopping state.
        /// </summary>
        public void ResetState()
        {
            _currentHits = 0;
            _strikeCount = 0;
            _lastChopPointIndex = 0;
            _lastHitDirection = Vector3.zero;
            _isFalling = false;

            if (_chopInteraction != null)
            {
                _chopInteraction.enabled = true;
            }
        }

        private void OnEnable()
        {
            if (_chopSubscriber != null)
            {
                _chopSubscription = _chopSubscriber.Subscribe(OnChopEvent);
            }
        }

        private void OnDisable()
        {
            _chopSubscription?.Dispose();
            _chopSubscription = null;
        }

        private void Reset()
        {
            _chopInteraction = GetComponent<ChopInteraction>();
        }

        private void OnChopEvent(ChopEvent message)
        {
            if (message.Tree != this)
            {
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[ChoppableTree] ChopEvent received. Damage={message.Damage}, Direction={message.HitDirection}", this);
            }

            ReceiveHit(message.HitDirection, message.Damage);
        }

        private Transform ResolveChopPoint(int index)
        {
            if (_chopPoints == null || _chopPoints.Length == 0)
            {
                return transform;
            }

            var clampedIndex = Mathf.Abs(index) % _chopPoints.Length;
            return _chopPoints[clampedIndex] != null ? _chopPoints[clampedIndex] : transform;
        }
    }
}
