// Path: Assets/Project/Scpripts/TreeFelling/LogPickup.cs
// Purpose: Represents a pooled fallen log that can be added to inventory and visually carried by the player.
// Dependencies: UniTask, UnityEngine.Pool, VContainer, PlayerWeight.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.PlayerWeight;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Interactable log fragment spawned after a tree falls.
    /// </summary>
    public sealed class LogPickup : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Collider[] _interactionColliders;

        [SerializeField]
        private Renderer[] _renderers;

        [SerializeField]
        private Vector3 _primaryCarryLocalPosition = new Vector3(0.45f, -0.25f, 0.75f);

        [SerializeField]
        private Vector3 _secondaryCarryLocalPosition = new Vector3(-0.45f, -0.25f, 0.75f);

        [SerializeField]
        [Min(1f)]
        private float _followLerpSpeed = 16f;

        private IInventoryWriteService _inventoryWriteService;
        private PlayerWeightRuntime _playerWeightRuntime;
        private IPlayerCarryAnchor _playerCarryAnchor;
        private ObjectPool<LogPickup> _ownerPool;

        private bool _isAvailable;
        private bool _isFollowingPlayer;
        private Vector3 _targetCarryLocalPosition;

        [Inject]
        private void Construct(
            IInventoryWriteService inventoryWriteService,
            PlayerWeightRuntime playerWeightRuntime,
            IPlayerCarryAnchor playerCarryAnchor)
        {
            _inventoryWriteService = inventoryWriteService;
            _playerWeightRuntime = playerWeightRuntime;
            _playerCarryAnchor = playerCarryAnchor;
        }

        /// <summary>
        /// Binds the owner pool used to recycle this log pickup.
        /// </summary>
        /// <param name="ownerPool">Owning pool.</param>
        public void BindPool(ObjectPool<LogPickup> ownerPool)
        {
            _ownerPool = ownerPool;
        }

        /// <summary>
        /// Activates the log pickup at the specified world transform.
        /// </summary>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);

            _isAvailable = true;
            _isFollowingPlayer = false;
            _targetCarryLocalPosition = Vector3.zero;

            SetCollisionEnabled(true);
            SetRenderersEnabled(true);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns the log pickup back to its pool.
        /// </summary>
        public void ReleaseToPool()
        {
            _isAvailable = false;
            _isFollowingPlayer = false;
            transform.SetParent(null, true);

            if (_ownerPool != null)
            {
                _ownerPool.Release(this);
                return;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Picks up the log and attaches it to the player's carry anchor.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public UniTask InteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            if (!_isAvailable || _isFollowingPlayer || _inventoryWriteService == null || !_inventoryWriteService.TryAddLog(1))
            {
                return UniTask.CompletedTask;
            }

            _isAvailable = false;
            _isFollowingPlayer = true;
            _targetCarryLocalPosition = ResolveCarryOffset();

            if (_playerCarryAnchor != null && _playerCarryAnchor.FollowTransform != null)
            {
                transform.SetParent(_playerCarryAnchor.FollowTransform, true);
            }

            SetCollisionEnabled(false);
            UpdateWeightState();

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Heavy interaction resolves to the same pickup behavior.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public UniTask HeavyInteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            return InteractAsync(context, cancellationToken);
        }

        private void LateUpdate()
        {
            if (!_isFollowingPlayer || _playerCarryAnchor == null || _playerCarryAnchor.FollowTransform == null)
            {
                return;
            }

            // Smooth local follow keeps the feedback tactile without requiring a dedicated animation rig.
            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetCarryLocalPosition, Time.deltaTime * _followLerpSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * _followLerpSpeed);
        }

        private Vector3 ResolveCarryOffset()
        {
            var currentWeight = _playerWeightRuntime != null ? _playerWeightRuntime.CurrentWeight : PlayerWeightType.Empty;
            return currentWeight == PlayerWeightType.HeavyLog ? _secondaryCarryLocalPosition : _primaryCarryLocalPosition;
        }

        private void UpdateWeightState()
        {
            if (_playerWeightRuntime == null)
            {
                return;
            }

            switch (_playerWeightRuntime.CurrentWeight)
            {
                case PlayerWeightType.Empty:
                case PlayerWeightType.LightItem:
                    _playerWeightRuntime.SetWeight(PlayerWeightType.HeavyLog);
                    break;
                case PlayerWeightType.HeavyLog:
                    _playerWeightRuntime.SetWeight(PlayerWeightType.TwoLogs);
                    break;
            }
        }

        private void SetCollisionEnabled(bool isEnabled)
        {
            if (_interactionColliders == null)
            {
                return;
            }

            for (var index = 0; index < _interactionColliders.Length; index++)
            {
                if (_interactionColliders[index] != null)
                {
                    _interactionColliders[index].enabled = isEnabled;
                }
            }
        }

        private void SetRenderersEnabled(bool isEnabled)
        {
            if (_renderers == null)
            {
                return;
            }

            for (var index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].enabled = isEnabled;
                }
            }
        }
    }
}
