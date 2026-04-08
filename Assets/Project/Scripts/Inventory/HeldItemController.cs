// Path: Assets/Project/Scpripts/Inventory/HeldItemController.cs
// Purpose: Visualizes up to two held items, supports dropping them into the world, and syncs player carry weight.
// Dependencies: MessagePipe, UnityEngine.Pool, VContainer, InventorySystem, PlayerWeight.

using System;
using System.Collections.Generic;
using MessagePipe;
using ProjectResonance.PlayerWeight;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Handles the player's held world-space item visuals.
    /// </summary>
    [AddComponentMenu("Project Resonance/Inventory/Held Item Controller")]
    [DisallowMultipleComponent]
    public sealed class HeldItemController : MonoBehaviour
    {
        [Serializable]
        private struct HeldItemSlot
        {
            [SerializeField]
            private ItemDefinition _itemDefinition;

            [SerializeField]
            private GameObject _instance;

            [SerializeField]
            private int _sourceSlotIndex;

            /// <summary>
            /// Gets the item definition represented by this hand slot.
            /// </summary>
            public ItemDefinition ItemDefinition => _itemDefinition;

            /// <summary>
            /// Gets the spawned world instance attached to this hand slot.
            /// </summary>
            public GameObject Instance => _instance;

            /// <summary>
            /// Gets the originating inventory slot index.
            /// </summary>
            public int SourceSlotIndex => _sourceSlotIndex;

            /// <summary>
            /// Gets whether the hand slot currently contains an item.
            /// </summary>
            public bool HasItem => _itemDefinition != null && _instance != null;

            /// <summary>
            /// Creates a new held item slot.
            /// </summary>
            /// <param name="itemDefinition">Held item definition.</param>
            /// <param name="instance">Spawned held instance.</param>
            /// <param name="sourceSlotIndex">Origin inventory slot index.</param>
            public HeldItemSlot(ItemDefinition itemDefinition, GameObject instance, int sourceSlotIndex)
            {
                _itemDefinition = itemDefinition;
                _instance = instance;
                _sourceSlotIndex = sourceSlotIndex;
            }

            /// <summary>
            /// Returns an empty slot.
            /// </summary>
            /// <returns>Cleared slot value.</returns>
            public HeldItemSlot Clear()
            {
                return new HeldItemSlot(null, null, -1);
            }
        }

        [Header("Attach Points")]
        [SerializeField]
        private Transform _primaryAttachPoint;

        [SerializeField]
        private Transform _secondaryAttachPoint;

        [SerializeField]
        private Transform _dropOrigin;

        [SerializeField]
        private Animator _characterAnimator;

        [Header("Drop Physics")]
        [SerializeField]
        [Min(0f)]
        private float _dropForwardForce = 2f;

        [SerializeField]
        [Min(0f)]
        private float _dropUpwardForce = 0.75f;

        [Header("Weight Thresholds")]
        [SerializeField]
        [Min(0f)]
        private float _lightWeightThreshold = 2f;

        [SerializeField]
        [Min(0f)]
        private float _heavyWeightThreshold = 5f;

        [SerializeField]
        [Min(0f)]
        private float _twoLogsWeightThreshold = 9f;

        private readonly Dictionary<ItemDefinition, ObjectPool<GameObject>> _poolByItemDefinition = new Dictionary<ItemDefinition, ObjectPool<GameObject>>();
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _ownerPoolByInstance = new Dictionary<GameObject, ObjectPool<GameObject>>();

        private InventoryConfig _inventoryConfig;
        private InventorySystem _inventorySystem;
        private IItemVisualFactory _itemVisualFactory;
        private PlayerWeightState _playerWeightState;
        private IBufferedSubscriber<ActiveSlotChangedEvent> _activeSlotChangedSubscriber;
        private IBufferedSubscriber<InventoryChangedEvent> _inventoryChangedSubscriber;

        private IDisposable _activeSlotChangedSubscription;
        private IDisposable _inventoryChangedSubscription;
        private HeldItemSlot _primarySlot;
        private HeldItemSlot _secondarySlot;
        private bool _suspendInventoryValidation;

        /// <summary>
        /// Gets the number of currently held items.
        /// </summary>
        public int HeldCount
        {
            get
            {
                var heldCount = 0;
                if (_primarySlot.HasItem)
                {
                    heldCount++;
                }

                if (_secondarySlot.HasItem)
                {
                    heldCount++;
                }

                return heldCount;
            }
        }

        /// <summary>
        /// Gets the world position where crafted previews and dropped items appear.
        /// </summary>
        public Vector3 CraftSpawnPosition => ResolveDropOrigin().position;

        /// <summary>
        /// Gets the world rotation used for crafted previews and dropped items.
        /// </summary>
        public Quaternion CraftSpawnRotation => ResolveDropOrigin().rotation;

        [Inject]
        private void Construct(
            InventoryConfig inventoryConfig,
            InventorySystem inventorySystem,
            IItemVisualFactory itemVisualFactory,
            PlayerWeightState playerWeightState,
            IBufferedSubscriber<ActiveSlotChangedEvent> activeSlotChangedSubscriber,
            IBufferedSubscriber<InventoryChangedEvent> inventoryChangedSubscriber)
        {
            _inventoryConfig = inventoryConfig;
            _inventorySystem = inventorySystem;
            _itemVisualFactory = itemVisualFactory;
            _playerWeightState = playerWeightState;
            _activeSlotChangedSubscriber = activeSlotChangedSubscriber;
            _inventoryChangedSubscriber = inventoryChangedSubscriber;
        }

        private void Start()
        {
            if (_activeSlotChangedSubscriber != null)
            {
                _activeSlotChangedSubscription = _activeSlotChangedSubscriber.Subscribe(OnActiveSlotChanged);
            }

            if (_inventoryChangedSubscriber != null)
            {
                _inventoryChangedSubscription = _inventoryChangedSubscriber.Subscribe(_ => OnInventoryChanged());
            }

            UpdateWeightState();
        }

        private void OnDestroy()
        {
            _activeSlotChangedSubscription?.Dispose();
            _inventoryChangedSubscription?.Dispose();

            ClearHeldItems();

            foreach (var pool in _poolByItemDefinition.Values)
            {
                pool.Dispose();
            }

            _poolByItemDefinition.Clear();
            _ownerPoolByInstance.Clear();
        }

        private void Reset()
        {
            _primaryAttachPoint = transform;
            _secondaryAttachPoint = transform;
            _dropOrigin = transform;
            _characterAnimator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Spawns an item world prefab and attaches it to the next available hand slot.
        /// </summary>
        /// <param name="itemDefinition">Item definition to hold.</param>
        /// <returns>True when the item was attached successfully.</returns>
        public bool PickUp(ItemDefinition itemDefinition)
        {
            return TryEquipItem(itemDefinition, -1);
        }

        /// <summary>
        /// Drops the primary held item into the world and removes one matching item from inventory.
        /// </summary>
        public void Drop()
        {
            if (!_primarySlot.HasItem)
            {
                return;
            }

            _suspendInventoryValidation = true;
            var removedFromInventory = _inventorySystem.RemoveItem(_primarySlot.ItemDefinition, 1);
            _suspendInventoryValidation = false;

            if (!removedFromInventory)
            {
                ReleaseHeldSlot(ref _primarySlot);
                CompactHeldSlots();
                UpdateWeightState();
                return;
            }

            DropHeldSlotToWorld(ref _primarySlot);
            CompactHeldSlots();
            UpdateWeightState();
        }

        /// <summary>
        /// Clears all currently held item visuals without changing inventory contents.
        /// </summary>
        public void ClearHeldItems()
        {
            ReleaseHeldSlot(ref _primarySlot);
            ReleaseHeldSlot(ref _secondarySlot);
            UpdateWeightState();
        }

        /// <summary>
        /// Copies the current held item definitions into the provided buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <returns>Number of item definitions written into the buffer.</returns>
        public int GetHeldItemsNonAlloc(ItemDefinition[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return 0;
            }

            var writeIndex = 0;

            if (_primarySlot.HasItem)
            {
                buffer[writeIndex++] = _primarySlot.ItemDefinition;
            }

            if (_secondarySlot.HasItem && writeIndex < buffer.Length)
            {
                buffer[writeIndex++] = _secondarySlot.ItemDefinition;
            }

            for (var index = writeIndex; index < buffer.Length; index++)
            {
                buffer[index] = null;
            }

            return writeIndex;
        }

        /// <summary>
        /// Plays the provided animation trigger on the optional character animator.
        /// </summary>
        /// <param name="animationTrigger">Animator trigger name.</param>
        public void TriggerAnimation(string animationTrigger)
        {
            if (_characterAnimator == null || string.IsNullOrWhiteSpace(animationTrigger))
            {
                return;
            }

            _characterAnimator.SetTrigger(animationTrigger);
        }

        private void OnActiveSlotChanged(ActiveSlotChangedEvent message)
        {
            var slot = _inventorySystem.GetSlot(message.CurrentSlotIndex);
            if (slot.IsEmpty)
            {
                return;
            }

            TryEquipItem(slot.ItemDefinition, message.CurrentSlotIndex);
        }

        private void OnInventoryChanged()
        {
            if (_suspendInventoryValidation)
            {
                return;
            }

            ValidateHeldItemsAgainstInventory();
        }

        private bool TryEquipItem(ItemDefinition itemDefinition, int sourceSlotIndex)
        {
            if (itemDefinition == null || !_inventorySystem.HasItem(itemDefinition, 1))
            {
                return false;
            }

            if (IsMatchingHeldSlot(_secondarySlot, itemDefinition, sourceSlotIndex))
            {
                var previousPrimary = _primarySlot;
                _primarySlot = _secondarySlot;
                _secondarySlot = previousPrimary;

                ReattachHeldSlot(_primarySlot, ResolvePrimaryAttachPoint());
                ReattachHeldSlot(_secondarySlot, ResolveSecondaryAttachPoint());
                UpdateWeightState();
                return true;
            }

            if (IsMatchingHeldSlot(_primarySlot, itemDefinition, sourceSlotIndex))
            {
                _primarySlot = new HeldItemSlot(itemDefinition, _primarySlot.Instance, sourceSlotIndex);
                return true;
            }

            if (_primarySlot.HasItem
                && _primarySlot.ItemDefinition == itemDefinition
                && !_secondarySlot.HasItem
                && _inventorySystem.HasItem(itemDefinition, 2))
            {
                var secondInstance = SpawnHeldInstance(itemDefinition);
                if (secondInstance == null)
                {
                    return false;
                }

                _secondarySlot = new HeldItemSlot(itemDefinition, secondInstance, sourceSlotIndex);
                ReattachHeldSlot(_secondarySlot, ResolveSecondaryAttachPoint());
                UpdateWeightState();
                return true;
            }

            ReleaseHeldSlot(ref _secondarySlot);

            if (_primarySlot.HasItem)
            {
                _secondarySlot = _primarySlot;
                ReattachHeldSlot(_secondarySlot, ResolveSecondaryAttachPoint());
            }

            var instance = SpawnHeldInstance(itemDefinition);
            if (instance == null)
            {
                return false;
            }

            _primarySlot = new HeldItemSlot(itemDefinition, instance, sourceSlotIndex);
            ReattachHeldSlot(_primarySlot, ResolvePrimaryAttachPoint());
            UpdateWeightState();
            return true;
        }

        private static bool IsMatchingHeldSlot(HeldItemSlot slot, ItemDefinition itemDefinition, int sourceSlotIndex)
        {
            if (!slot.HasItem || slot.ItemDefinition != itemDefinition)
            {
                return false;
            }

            if (sourceSlotIndex < 0 || slot.SourceSlotIndex < 0)
            {
                return true;
            }

            return slot.SourceSlotIndex == sourceSlotIndex;
        }

        private void ValidateHeldItemsAgainstInventory()
        {
            if (_secondarySlot.HasItem)
            {
                var requiredCount = _primarySlot.HasItem && _primarySlot.ItemDefinition == _secondarySlot.ItemDefinition ? 2 : 1;
                if (!_inventorySystem.HasItem(_secondarySlot.ItemDefinition, requiredCount))
                {
                    ReleaseHeldSlot(ref _secondarySlot);
                }
            }

            if (_primarySlot.HasItem && !_inventorySystem.HasItem(_primarySlot.ItemDefinition, 1))
            {
                ReleaseHeldSlot(ref _primarySlot);
            }

            CompactHeldSlots();
            UpdateWeightState();
        }

        private void CompactHeldSlots()
        {
            if (_primarySlot.HasItem || !_secondarySlot.HasItem)
            {
                return;
            }

            _primarySlot = _secondarySlot;
            _secondarySlot = _secondarySlot.Clear();
            ReattachHeldSlot(_primarySlot, ResolvePrimaryAttachPoint());
        }

        private void ReattachHeldSlot(HeldItemSlot slot, Transform attachPoint)
        {
            if (!slot.HasItem)
            {
                return;
            }

            PrepareHeldInstance(slot.Instance, attachPoint);
        }

        private void ReleaseHeldSlot(ref HeldItemSlot slot)
        {
            if (slot.Instance != null && _ownerPoolByInstance.TryGetValue(slot.Instance, out var ownerPool))
            {
                ownerPool.Release(slot.Instance);
            }

            slot = slot.Clear();
        }

        private void DropHeldSlotToWorld(ref HeldItemSlot slot)
        {
            if (!slot.HasItem)
            {
                return;
            }

            var droppedInstance = slot.Instance;
            var origin = ResolveDropOrigin();

            droppedInstance.transform.SetParent(null, true);
            droppedInstance.transform.SetPositionAndRotation(origin.position, origin.rotation);

            ToggleColliders(droppedInstance, true);

            if (!droppedInstance.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody = droppedInstance.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            // A small forward + upward impulse makes the drop read as intentional without needing a dedicated throw system.
            var impulse = (origin.forward * _dropForwardForce) + (Vector3.up * _dropUpwardForce);
            rigidbody.AddForce(impulse, ForceMode.Impulse);

            slot = slot.Clear();
        }

        private GameObject SpawnHeldInstance(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
            {
                return null;
            }

            var pool = ResolvePool(itemDefinition);
            var instance = pool.Get();
            _ownerPoolByInstance[instance] = pool;
            return instance;
        }

        private ObjectPool<GameObject> ResolvePool(ItemDefinition itemDefinition)
        {
            if (_poolByItemDefinition.TryGetValue(itemDefinition, out var existingPool))
            {
                return existingPool;
            }

            var defaultCapacity = _inventoryConfig != null
                ? _inventoryConfig.HeldVisualPoolCapacityPerItem
                : Mathf.Max(1, _inventorySystem.MaxSlots);
            var maxPoolSize = _inventoryConfig != null
                ? Mathf.Max(defaultCapacity, _inventoryConfig.MaxPooledVisualsPerItem)
                : Mathf.Max(defaultCapacity, _inventorySystem.MaxSlots);

            var pool = new ObjectPool<GameObject>(
                () => CreateInstance(itemDefinition),
                OnTakeInstanceFromPool,
                OnReturnInstanceToPool,
                OnDestroyPooledInstance,
                false,
                defaultCapacity,
                maxPoolSize);

            _poolByItemDefinition.Add(itemDefinition, pool);
            return pool;
        }

        private GameObject CreateInstance(ItemDefinition itemDefinition)
        {
            var instance = _itemVisualFactory != null
                ? _itemVisualFactory.CreateHeldVisual(itemDefinition)
                : new GameObject(itemDefinition != null ? itemDefinition.DisplayName : "HeldItem");
            instance.SetActive(false);
            return instance;
        }

        private void OnTakeInstanceFromPool(GameObject instance)
        {
            if (instance != null)
            {
                instance.SetActive(true);
            }
        }

        private void OnReturnInstanceToPool(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(null, false);
            instance.SetActive(false);
        }

        private void OnDestroyPooledInstance(GameObject instance)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private void PrepareHeldInstance(GameObject instance, Transform attachPoint)
        {
            if (instance == null)
            {
                return;
            }

            var targetAttachPoint = attachPoint != null ? attachPoint : transform;
            instance.transform.SetParent(targetAttachPoint, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            if (instance.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            ToggleColliders(instance, false);
        }

        private void ToggleColliders(GameObject instance, bool isEnabled)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = isEnabled;
                }
            }
        }

        private void UpdateWeightState()
        {
            if (_playerWeightState == null)
            {
                return;
            }

            var totalWeight = 0f;

            if (_primarySlot.HasItem)
            {
                totalWeight += Mathf.Max(0f, _primarySlot.ItemDefinition.Weight);
            }

            if (_secondarySlot.HasItem)
            {
                totalWeight += Mathf.Max(0f, _secondarySlot.ItemDefinition.Weight);
            }

            if (HeldCount <= 0 || totalWeight <= 0f)
            {
                _playerWeightState.SetWeight(PlayerWeightType.Empty);
                return;
            }

            if (totalWeight <= _lightWeightThreshold)
            {
                _playerWeightState.SetWeight(PlayerWeightType.LightItem);
                return;
            }

            if (HeldCount >= 2 || totalWeight >= _twoLogsWeightThreshold)
            {
                _playerWeightState.SetWeight(PlayerWeightType.TwoLogs);
                return;
            }

            if (totalWeight <= _heavyWeightThreshold)
            {
                _playerWeightState.SetWeight(PlayerWeightType.HeavyLog);
                return;
            }

            _playerWeightState.SetWeight(PlayerWeightType.HeavyLog);
        }

        private Transform ResolvePrimaryAttachPoint()
        {
            return _primaryAttachPoint != null ? _primaryAttachPoint : transform;
        }

        private Transform ResolveSecondaryAttachPoint()
        {
            if (_secondaryAttachPoint != null)
            {
                return _secondaryAttachPoint;
            }

            return ResolvePrimaryAttachPoint();
        }

        private Transform ResolveDropOrigin()
        {
            return _dropOrigin != null ? _dropOrigin : transform;
        }
    }
}
