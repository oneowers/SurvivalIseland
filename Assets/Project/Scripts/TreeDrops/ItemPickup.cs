// Path: Assets/Project/Scripts/TreeDrops/ItemPickup.cs
// Purpose: Pooled universal world pickup that flies toward the player and deposits inventory items into the shared runtime inventory.
// Dependencies: UniTask, UnityEngine, VContainer, ProjectResonance.Inventory, WorldItemPickupConfig, ItemPickupPoolService.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.Inventory;
using UnityEngine;
using VContainer;

namespace ProjectResonance.TreeDrops
{
    /// <summary>
    /// Supported movement styles for a dropped pickup.
    /// </summary>
    public enum PickupMovementMode
    {
        /// <summary>
        /// Uses a scripted arc and MoveTowards motion the entire way.
        /// </summary>
        LerpToPlayer = 0,

        /// <summary>
        /// Starts with a physics impulse, then switches to MoveTowards near the target.
        /// </summary>
        ImpulseThenLerp = 1,
    }

    /// <summary>
    /// Runtime pickup that flies toward the player and adds itself to inventory.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Drops/Item Pickup")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField]
        private ItemDefinition _itemDefinition;

        [SerializeField]
        [Min(1)]
        private int _quantity = 1;

        [Header("Movement")]
        [SerializeField]
        private PickupMovementMode _movementMode = PickupMovementMode.LerpToPlayer;

        [SerializeField]
        [Min(0.05f)]
        private float _pickupDuration = 0.45f;

        [SerializeField]
        [Min(0f)]
        private float _arcHeight = 1f;

        [SerializeField]
        [Min(0.1f)]
        private float _pickupTimeout = 3f;

        [SerializeField]
        [Min(0.05f)]
        private float _pickupDistance = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float _closeLerpSpeed = 8f;

        [Header("Impulse Mode")]
        [SerializeField]
        [Min(0f)]
        private float _impulseForce = 4.5f;

        [SerializeField]
        [Min(0f)]
        private float _impulseUpwardForce = 2f;

        [SerializeField]
        [Min(0.1f)]
        private float _switchToLerpDistance = 1.2f;

        [SerializeField]
        [Min(0.05f)]
        private float _impulseDuration = 0.35f;

        [Header("References")]
        [SerializeField]
        private Transform _visualRoot;

        [SerializeField]
        private SpriteRenderer _iconRenderer;

        [SerializeField]
        private Renderer[] _fallbackRenderers;

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private Collider _pickupCollider;

        [SerializeField]
        private string _playerTag = "Player";

        [Header("Icon Visual")]
        [SerializeField]
        private Color _iconTint = Color.white;

        [SerializeField]
        [Min(0.1f)]
        private float _iconWorldSize = 0.85f;

        [SerializeField]
        private float _iconVerticalOffset = 0.45f;

        [SerializeField]
        private bool _billboardToCamera = true;

        private bool _collected;
        private InventorySystem _inventorySystem;
        private IItemVisualFactory _itemVisualFactory;
        private ItemPickupPoolService _itemPickupPoolService;
        private Transform _target;
        private GameObject _spawnedVisualInstance;
        private Camera _billboardCamera;
        private CancellationTokenSource _pickupCancellationSource;

        [Inject]
        private void Construct(
            InventorySystem inventorySystem,
            IItemVisualFactory itemVisualFactory,
            ItemPickupPoolService itemPickupPoolService)
        {
            _inventorySystem = inventorySystem;
            _itemVisualFactory = itemVisualFactory;
            _itemPickupPoolService = itemPickupPoolService;
        }

        /// <summary>
        /// Applies the shared pickup config values to this pooled pickup instance.
        /// </summary>
        /// <param name="pickupConfig">Shared pickup config to apply.</param>
        public void ApplyConfig(WorldItemPickupConfig pickupConfig)
        {
            if (pickupConfig == null)
            {
                return;
            }

            _movementMode = pickupConfig.MovementMode;
            _pickupDuration = pickupConfig.PickupDuration;
            _arcHeight = pickupConfig.ArcHeight;
            _pickupTimeout = pickupConfig.PickupTimeout;
            _pickupDistance = pickupConfig.PickupDistance;
            _closeLerpSpeed = pickupConfig.CloseLerpSpeed;
            _impulseForce = pickupConfig.ImpulseForce;
            _impulseUpwardForce = pickupConfig.ImpulseUpwardForce;
            _switchToLerpDistance = pickupConfig.SwitchToLerpDistance;
            _impulseDuration = pickupConfig.ImpulseDuration;
        }

        /// <summary>
        /// Configures the pickup item data, quantity and target, then starts the collection motion.
        /// </summary>
        /// <param name="itemDefinition">Item definition assigned to this pickup.</param>
        /// <param name="quantity">Quantity granted on collect.</param>
        /// <param name="target">Player target transform.</param>
        public void Configure(ItemDefinition itemDefinition, int quantity, Transform target)
        {
            _itemDefinition = itemDefinition;
            _quantity = Mathf.Max(1, quantity);
            _target = target != null ? target : ResolvePlayerTarget();
            RefreshVisual();
            BeginPickup();
        }

        /// <summary>
        /// Starts the movement routine toward the player.
        /// </summary>
        public void BeginPickup()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            CancelPickupRoutine();
            _collected = false;
            _pickupCancellationSource = new CancellationTokenSource();
            RunPickupAsync(_pickupCancellationSource.Token).Forget();
        }

        /// <summary>
        /// Resets transient runtime state before the pickup is shown again from the pool.
        /// </summary>
        public void ResetStateForSpawn()
        {
            CancelPickupRoutine();
            _collected = false;
            _target = null;
            EnsureTriggerCollider();
            SetKinematicState(true);
        }

        /// <summary>
        /// Clears all transient runtime state before returning the pickup to its pool.
        /// </summary>
        public void ResetStateForPool()
        {
            CancelPickupRoutine();
            _collected = false;
            _target = null;
            _itemDefinition = null;
            _quantity = 1;
            CleanupVisual();
            EnsureTriggerCollider();
            SetKinematicState(true);
        }

        private void Awake()
        {
            ResolveReferences();
            SetKinematicState(true);
            RefreshVisual();
        }

        private void Reset()
        {
            ResolveReferences();
            SetKinematicState(true);
        }

        private void OnDisable()
        {
            CancelPickupRoutine();
        }

        private void LateUpdate()
        {
            UpdateIconBillboard();
        }

        private void OnDestroy()
        {
            CancelPickupRoutine();
            CleanupVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected || other == null)
            {
                return;
            }

            if (_target != null)
            {
                if (other.transform == _target || other.transform.root == _target.root)
                {
                    Collect();
                }

                return;
            }

            if (other.CompareTag(_playerTag) || other.transform.root.CompareTag(_playerTag))
            {
                Collect();
            }
        }

        private async UniTaskVoid RunPickupAsync(CancellationToken cancellationToken)
        {
            _target = _target != null ? _target : ResolvePlayerTarget();

            try
            {
                await AnimateArcStartAsync(cancellationToken);
                if (_collected)
                {
                    return;
                }

                var timeoutAt = Time.time + _pickupTimeout;
                if (_movementMode == PickupMovementMode.ImpulseThenLerp)
                {
                    await ImpulseThenLerpAsync(timeoutAt, cancellationToken);
                    return;
                }

                await MoveToTargetAsync(timeoutAt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Pool returns and scene disables intentionally cancel in-flight pickup motion.
            }
        }

        private async UniTask AnimateArcStartAsync(CancellationToken cancellationToken)
        {
            SetKinematicState(true);

            var startPosition = transform.position;
            var apexPosition = startPosition + Vector3.up * _arcHeight;
            var arcDuration = Mathf.Min(0.2f, _pickupDuration * 0.5f);
            var elapsed = 0f;

            while (elapsed < arcDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, arcDuration));

                // A short scripted rise makes the drop feel light and readable before homing starts.
                transform.position = Vector3.Lerp(startPosition, apexPosition, Mathf.SmoothStep(0f, 1f, t));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.position = apexPosition;
        }

        private async UniTask ImpulseThenLerpAsync(float timeoutAt, CancellationToken cancellationToken)
        {
            var targetPosition = ResolveTargetPosition();
            var planarDirection = targetPosition - transform.position;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection = transform.forward;
            }

            SetKinematicState(false);
            ZeroDynamicRigidbodyVelocities();
            _rigidbody.AddForce(planarDirection.normalized * _impulseForce + Vector3.up * _impulseUpwardForce, ForceMode.Impulse);

            var impulseFinishedAt = Time.time + _impulseDuration;
            while (Time.time < impulseFinishedAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldCollect(timeoutAt))
                {
                    return;
                }

                if (_target != null && Vector3.Distance(transform.position, ResolveTargetPosition()) <= _switchToLerpDistance)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            ZeroDynamicRigidbodyVelocities();
            SetKinematicState(true);

            await MoveToTargetAsync(timeoutAt, cancellationToken);
        }

        private async UniTask MoveToTargetAsync(float timeoutAt, CancellationToken cancellationToken)
        {
            var cachedTargetPosition = ResolveTargetPosition();
            var initialDistance = Mathf.Max(0.01f, Vector3.Distance(transform.position, cachedTargetPosition));
            var moveSpeed = Mathf.Max(initialDistance / Mathf.Max(0.01f, _pickupDuration), _closeLerpSpeed);

            while (!_collected)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldCollect(timeoutAt))
                {
                    return;
                }

                var targetPosition = ResolveTargetPosition();
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private bool ShouldCollect(float timeoutAt)
        {
            if (_collected)
            {
                return true;
            }

            if (_target != null && Vector3.Distance(transform.position, ResolveTargetPosition()) <= _pickupDistance)
            {
                Collect();
                return true;
            }

            if (Time.time >= timeoutAt)
            {
                Collect();
                return true;
            }

            return false;
        }

        private void Collect()
        {
            if (_collected)
            {
                return;
            }

            if (_itemDefinition == null)
            {
                return;
            }

            if (_inventorySystem == null)
            {
                return;
            }

            _collected = true;

            if (!_inventorySystem.AddItem(_itemDefinition, Mathf.Max(1, _quantity)))
            {
                // The pickup is consumed only when the full stack fit into inventory, so items are never silently lost.
                _collected = false;
                return;
            }

            if (_itemPickupPoolService != null)
            {
                _itemPickupPoolService.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        private Vector3 ResolveTargetPosition()
        {
            _target = _target != null ? _target : ResolvePlayerTarget();

            if (_target == null)
            {
                return transform.position;
            }

            return _target.position;
        }

        private Transform ResolvePlayerTarget()
        {
            if (string.IsNullOrWhiteSpace(_playerTag))
            {
                return null;
            }

            var playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            return playerObject != null ? playerObject.transform : null;
        }

        private void ResolveReferences()
        {
            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }

            if (_fallbackRenderers == null || _fallbackRenderers.Length == 0)
            {
                _fallbackRenderers = GetComponents<Renderer>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_pickupCollider == null)
            {
                _pickupCollider = GetComponent<Collider>();
            }

            EnsureTriggerCollider();
        }

        private void EnsureTriggerCollider()
        {
            if (_pickupCollider != null)
            {
                _pickupCollider.isTrigger = true;
                _pickupCollider.enabled = true;
            }
        }

        private void SetKinematicState(bool isKinematic)
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.isKinematic = isKinematic;
            _rigidbody.useGravity = !isKinematic;
        }

        private void ZeroDynamicRigidbodyVelocities()
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
            {
                return;
            }

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void RefreshVisual()
        {
            ResolveReferences();
            CleanupVisual();

            if (TryApplyIconVisual())
            {
                SetFallbackRenderersEnabled(false);
                return;
            }

            if (_itemVisualFactory == null
                || !_itemVisualFactory.TryCreatePickupFallbackVisual(_itemDefinition, _visualRoot != null ? _visualRoot : transform, out _spawnedVisualInstance)
                || _spawnedVisualInstance == null)
            {
                DisableIconVisual();
                SetFallbackRenderersEnabled(true);
                return;
            }

            _spawnedVisualInstance.transform.localPosition = Vector3.zero;
            _spawnedVisualInstance.transform.localRotation = Quaternion.identity;
            _spawnedVisualInstance.transform.localScale = Vector3.one;

            SanitizeSpawnedVisual(_spawnedVisualInstance);
            SetFallbackRenderersEnabled(false);
        }

        private void CleanupVisual()
        {
            if (_spawnedVisualInstance != null)
            {
                Destroy(_spawnedVisualInstance);
                _spawnedVisualInstance = null;
            }

            DisableIconVisual();
        }

        private void SetFallbackRenderersEnabled(bool isEnabled)
        {
            if (_fallbackRenderers == null)
            {
                return;
            }

            for (var index = 0; index < _fallbackRenderers.Length; index++)
            {
                if (_fallbackRenderers[index] != null)
                {
                    _fallbackRenderers[index].enabled = isEnabled;
                }
            }
        }

        private bool TryApplyIconVisual()
        {
            if (_itemDefinition == null || _itemDefinition.Icon == null)
            {
                DisableIconVisual();
                return false;
            }

            var iconRenderer = EnsureIconRenderer();
            if (iconRenderer == null)
            {
                return false;
            }

            iconRenderer.sprite = _itemDefinition.Icon;
            iconRenderer.color = _iconTint;
            iconRenderer.enabled = true;

            var iconTransform = iconRenderer.transform;
            iconTransform.localPosition = new Vector3(0f, _iconVerticalOffset, 0f);
            iconTransform.localRotation = Quaternion.identity;

            ApplyIconScale(iconRenderer);
            return true;
        }

        private SpriteRenderer EnsureIconRenderer()
        {
            if (_iconRenderer != null)
            {
                return _iconRenderer;
            }

            var parent = _visualRoot != null ? _visualRoot : transform;
            var iconObject = new GameObject("PickupIcon", typeof(SpriteRenderer));
            var iconTransform = iconObject.transform;
            iconTransform.SetParent(parent, false);

            _iconRenderer = iconObject.GetComponent<SpriteRenderer>();
            _iconRenderer.enabled = false;
            _iconRenderer.color = _iconTint;
            _iconRenderer.sortingOrder = 10;

            return _iconRenderer;
        }

        private void DisableIconVisual()
        {
            if (_iconRenderer == null)
            {
                return;
            }

            _iconRenderer.sprite = null;
            _iconRenderer.enabled = false;
        }

        private void ApplyIconScale(SpriteRenderer iconRenderer)
        {
            if (iconRenderer == null || iconRenderer.sprite == null)
            {
                return;
            }

            var spriteBounds = iconRenderer.sprite.bounds.size;
            var largestSide = Mathf.Max(spriteBounds.x, spriteBounds.y, 0.0001f);
            var uniformScale = _iconWorldSize / largestSide;
            iconRenderer.transform.localScale = Vector3.one * uniformScale;
        }

        private void UpdateIconBillboard()
        {
            if (!_billboardToCamera || _iconRenderer == null || !_iconRenderer.enabled)
            {
                return;
            }

            var cameraTransform = ResolveBillboardCamera();
            if (cameraTransform == null)
            {
                return;
            }

            var iconTransform = _iconRenderer.transform;
            var directionToCamera = cameraTransform.position - iconTransform.position;
            if (directionToCamera.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            // World pickup icons always face the active camera so one universal prefab can represent any item.
            iconTransform.rotation = Quaternion.LookRotation(directionToCamera.normalized, cameraTransform.up);
        }

        private Transform ResolveBillboardCamera()
        {
            if (_billboardCamera == null || !_billboardCamera.isActiveAndEnabled)
            {
                _billboardCamera = Camera.main;
            }

            return _billboardCamera != null ? _billboardCamera.transform : null;
        }

        private void CancelPickupRoutine()
        {
            if (_pickupCancellationSource == null)
            {
                return;
            }

            _pickupCancellationSource.Cancel();
            _pickupCancellationSource.Dispose();
            _pickupCancellationSource = null;
        }

        private static void SanitizeSpawnedVisual(GameObject visualInstance)
        {
            if (visualInstance == null)
            {
                return;
            }

            var colliders = visualInstance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = false;
                }
            }

            var rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);
            for (var index = 0; index < rigidbodies.Length; index++)
            {
                var rigidbody = rigidbodies[index];
                if (rigidbody == null)
                {
                    continue;
                }

                if (!rigidbody.isKinematic)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }

                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            var behaviours = visualInstance.GetComponentsInChildren<Behaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }
    }
}
