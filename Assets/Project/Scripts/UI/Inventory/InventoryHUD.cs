// Path: Assets/Project/Scripts/UI/Inventory/InventoryHUD.cs
// Purpose: Passively renders the authored Canvas inventory HUD from the shared inventory runtime state and publishes active slot changes.
// Dependencies: MessagePipe, TMPro, UnityEngine.UI, Input System, VContainer, InventorySystem, InventoryHudSlotAuthoring.

using System;
using System.Collections.Generic;
using MessagePipe;
using ProjectResonance.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace ProjectResonance.InventoryUI
{
    /// <summary>
    /// Runtime Canvas HUD for the player's inventory.
    /// </summary>
    [AddComponentMenu("Project Resonance/UI/Inventory HUD")]
    [DisallowMultipleComponent]
    public sealed class InventoryHUD : MonoBehaviour
    {
        [Header("Authored Layout")]
        [SerializeField]
        private RectTransform _slotContainer;

        [Header("Colors")]
        [SerializeField]
        private Color _backgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.82f);

        [SerializeField]
        private Color _activeBackgroundColor = new Color(0.24f, 0.24f, 0.2f, 0.96f);

        [SerializeField]
        private Color _emptyTintColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField]
        private Color _filledTintColor = new Color(1f, 1f, 1f, 1f);

        private InventorySystem _inventorySystem;
        private IBufferedSubscriber<InventoryChangedEvent> _inventoryChangedSubscriber;
        private IBufferedSubscriber<ActiveSlotChangedEvent> _activeSlotChangedSubscriber;
        private IBufferedPublisher<ActiveSlotChangedEvent> _activeSlotChangedPublisher;

        private InventoryHudSlotAuthoring[] _slotViews = Array.Empty<InventoryHudSlotAuthoring>();
        private InputAction _scrollAction;
        private IDisposable _inventoryChangedSubscription;
        private IDisposable _activeSlotChangedSubscription;
        private int _activeSlotIndex;

        [Inject]
        private void Construct(
            InventorySystem inventorySystem,
            IBufferedSubscriber<InventoryChangedEvent> inventoryChangedSubscriber,
            IBufferedSubscriber<ActiveSlotChangedEvent> activeSlotChangedSubscriber,
            IBufferedPublisher<ActiveSlotChangedEvent> activeSlotChangedPublisher)
        {
            _inventorySystem = inventorySystem;
            _inventoryChangedSubscriber = inventoryChangedSubscriber;
            _activeSlotChangedSubscriber = activeSlotChangedSubscriber;
            _activeSlotChangedPublisher = activeSlotChangedPublisher;
        }

        private void Awake()
        {
            CollectSlotViews();
            RegisterSlotClicks();
            InitializeAuthoredHud();
            CreateScrollAction();
        }

        private void Start()
        {
            if (_inventoryChangedSubscriber != null)
            {
                _inventoryChangedSubscription = _inventoryChangedSubscriber.Subscribe(_ => RefreshSlots());
            }

            if (_activeSlotChangedSubscriber != null)
            {
                _activeSlotChangedSubscription = _activeSlotChangedSubscriber.Subscribe(OnActiveSlotChanged);
            }

            RefreshSlots();
            PublishActiveSlotChange(_activeSlotIndex);
        }

        private void OnEnable()
        {
            _scrollAction?.Enable();
        }

        private void OnDisable()
        {
            _scrollAction?.Disable();
        }

        private void OnDestroy()
        {
            _inventoryChangedSubscription?.Dispose();
            _activeSlotChangedSubscription?.Dispose();
            UnregisterSlotClicks();

            if (_scrollAction != null)
            {
                _scrollAction.performed -= OnScrollPerformed;
                _scrollAction.Dispose();
                _scrollAction = null;
            }
        }

        private void CollectSlotViews()
        {
            var authoredSlots = GetComponentsInChildren<InventoryHudSlotAuthoring>(true);
            if (authoredSlots != null && authoredSlots.Length > 0)
            {
                Array.Sort(authoredSlots, CompareSlotOrder);
                _slotViews = authoredSlots;
                ValidateSlotCount();
                return;
            }

            var slotRoot = _slotContainer != null ? _slotContainer : transform as RectTransform;
            if (slotRoot == null)
            {
                _slotViews = Array.Empty<InventoryHudSlotAuthoring>();
                return;
            }

            var discoveredSlots = new List<InventoryHudSlotAuthoring>(slotRoot.childCount);
            for (var childIndex = 0; childIndex < slotRoot.childCount; childIndex++)
            {
                var child = slotRoot.GetChild(childIndex);
                if (child == null)
                {
                    continue;
                }

                var slotAuthoring = child.GetComponent<InventoryHudSlotAuthoring>();
                if (slotAuthoring == null)
                {
                    // The current scene already has authored slot GameObjects, so this runtime add only upgrades them into the passive presenter model.
                    slotAuthoring = child.gameObject.AddComponent<InventoryHudSlotAuthoring>();
                }

                slotAuthoring.AssignRuntimeSlotIndex(discoveredSlots.Count);
                discoveredSlots.Add(slotAuthoring);
            }

            _slotViews = discoveredSlots.ToArray();
            Array.Sort(_slotViews, CompareSlotOrder);
            ValidateSlotCount();
        }

        private void InitializeAuthoredHud()
        {
            for (var slotIndex = 0; slotIndex < _slotViews.Length; slotIndex++)
            {
                var slotView = _slotViews[slotIndex];
                if (slotView == null)
                {
                    continue;
                }

                slotView.SetBackgroundColor(_backgroundColor);
                slotView.SetIcon(null, _emptyTintColor);
                slotView.SetCount(string.Empty);
                slotView.SetDurabilityVisible(false);
            }
        }

        private void CreateScrollAction()
        {
            _scrollAction = new InputAction("InventoryScroll", InputActionType.Value, "<Mouse>/scroll");
            _scrollAction.performed += OnScrollPerformed;
        }

        private void OnScrollPerformed(InputAction.CallbackContext context)
        {
            var scrollValue = context.ReadValue<Vector2>();
            var slotCount = ResolveSlotCount();
            if (Mathf.Approximately(scrollValue.y, 0f) || slotCount <= 0)
            {
                return;
            }

            var direction = scrollValue.y < 0f ? 1 : -1;
            var nextSlotIndex = (_activeSlotIndex + direction + slotCount) % slotCount;
            PublishActiveSlotChange(nextSlotIndex);
        }

        private void OnActiveSlotChanged(ActiveSlotChangedEvent message)
        {
            _activeSlotIndex = Mathf.Clamp(message.CurrentSlotIndex, 0, Mathf.Max(0, ResolveSlotCount() - 1));
            RefreshSlotHighlights();
        }

        private void PublishActiveSlotChange(int nextSlotIndex)
        {
            var slotCount = ResolveSlotCount();
            if (slotCount <= 0)
            {
                return;
            }

            var previousSlotIndex = _activeSlotIndex;
            _activeSlotIndex = Mathf.Clamp(nextSlotIndex, 0, slotCount - 1);

            if (previousSlotIndex == _activeSlotIndex)
            {
                RefreshSlotHighlights();
                return;
            }

            if (_activeSlotChangedPublisher == null)
            {
                RefreshSlotHighlights();
                return;
            }

            _activeSlotChangedPublisher.Publish(new ActiveSlotChangedEvent(previousSlotIndex, _activeSlotIndex));
        }

        private void OnSlotClicked(int slotIndex)
        {
            PublishActiveSlotChange(slotIndex);
        }

        private void RefreshSlots()
        {
            var slotCount = ResolveSlotCount();
            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                var slotView = _slotViews[slotIndex];
                if (slotView == null)
                {
                    continue;
                }

                var stack = _inventorySystem != null && slotIndex < _inventorySystem.MaxSlots
                    ? _inventorySystem.GetSlot(slotIndex)
                    : default;

                var itemDefinition = stack.ItemDefinition;
                var icon = itemDefinition != null ? itemDefinition.Icon : null;
                var countText = stack.IsEmpty || stack.HasDurability ? string.Empty : stack.Count.ToString();

                slotView.SetIcon(icon, stack.IsEmpty ? _emptyTintColor : _filledTintColor);
                slotView.SetCount(countText);
                slotView.SetDurabilityVisible(stack.HasDurability);
                if (stack.HasDurability)
                {
                    slotView.SetDurabilityNormalized(stack.DurabilityNormalized);
                    Debug.Log($"[InventoryHUD] Durable slot refreshed. Slot={slotIndex}, Item={(itemDefinition != null ? itemDefinition.DisplayName : "null")}, Durability={stack.CurrentDurability}/{stack.MaxDurability}");
                }
            }

            RefreshSlotHighlights();
        }

        private void RefreshSlotHighlights()
        {
            for (var slotIndex = 0; slotIndex < _slotViews.Length; slotIndex++)
            {
                var slotView = _slotViews[slotIndex];
                if (slotView == null)
                {
                    continue;
                }

                slotView.SetBackgroundColor(slotIndex == _activeSlotIndex ? _activeBackgroundColor : _backgroundColor);
            }
        }

        private int ResolveSlotCount()
        {
            if (_slotViews != null && _slotViews.Length > 0)
            {
                return _slotViews.Length;
            }

            return _inventorySystem != null ? _inventorySystem.MaxSlots : 0;
        }

        private void ValidateSlotCount()
        {
            if (_inventorySystem == null || _slotViews == null)
            {
                return;
            }

            if (_slotViews.Length != _inventorySystem.MaxSlots)
            {
                Debug.LogWarning(
                    $"[InventoryHUD] Expected {_inventorySystem.MaxSlots} authored inventory slots but found {_slotViews.Length}. HUD will render the discovered slot count.",
                    this);
            }
        }

        private void RegisterSlotClicks()
        {
            if (_slotViews == null)
            {
                return;
            }

            for (var slotIndex = 0; slotIndex < _slotViews.Length; slotIndex++)
            {
                var slotView = _slotViews[slotIndex];
                if (slotView == null)
                {
                    continue;
                }

                slotView.Clicked += OnSlotClicked;
            }
        }

        private void UnregisterSlotClicks()
        {
            if (_slotViews == null)
            {
                return;
            }

            for (var slotIndex = 0; slotIndex < _slotViews.Length; slotIndex++)
            {
                var slotView = _slotViews[slotIndex];
                if (slotView == null)
                {
                    continue;
                }

                slotView.Clicked -= OnSlotClicked;
            }
        }

        private static int CompareSlotOrder(InventoryHudSlotAuthoring left, InventoryHudSlotAuthoring right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.SlotIndex.CompareTo(right.SlotIndex);
        }
    }
}
