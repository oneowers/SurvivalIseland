// Path: Assets/Project/Scpripts/Inventory/PlayerInventoryBridgeAdapter.cs
// Purpose: Bridges the shared inventory system to tree-felling and campfire inventory interfaces.
// Dependencies: MessagePipe, UnityEngine, Inventory, TreeFelling, Campfire.

using System;
using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.TreeFelling;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Adapter that exposes inventory-backed axe, log, and ignition queries to older gameplay systems.
    /// </summary>
    [AddComponentMenu("Project Resonance/Inventory/Player Inventory Bridge Adapter")]
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryBridgeAdapter : MonoBehaviour, IInventoryQuery, IInventoryWriteService, ICampfireInventoryQuery, ICampfireInventoryWriteService
    {
        [Header("Items")]
        [SerializeField]
        private ItemDefinition _logItem;

        [SerializeField]
        private ItemDefinition _flintItem;

        [SerializeField]
        private ItemDefinition _firesteelItem;

        private InventorySystem _inventorySystem;
        private IBufferedSubscriber<ActiveSlotChangedEvent> _activeSlotChangedSubscriber;

        private IDisposable _activeSlotChangedSubscription;
        private int _activeSlotIndex;

        [Inject]
        private void Construct(
            InventorySystem inventorySystem,
            IBufferedSubscriber<ActiveSlotChangedEvent> activeSlotChangedSubscriber)
        {
            _inventorySystem = inventorySystem;
            _activeSlotChangedSubscriber = activeSlotChangedSubscriber;
        }

        private void Start()
        {
            if (_activeSlotChangedSubscriber == null)
            {
                return;
            }

            _activeSlotChangedSubscription = _activeSlotChangedSubscriber.Subscribe(OnActiveSlotChanged);
        }

        private void OnDestroy()
        {
            _activeSlotChangedSubscription?.Dispose();
            _activeSlotChangedSubscription = null;
        }

        /// <summary>
        /// Returns the currently equipped axe tier derived from the active inventory slot.
        /// </summary>
        /// <returns>Resolved axe tier.</returns>
        public AxeTier GetEquippedAxeTier()
        {
            if (_inventorySystem == null)
            {
                return AxeTier.None;
            }

            var activeSlot = _inventorySystem.GetSlot(_activeSlotIndex);
            if (activeSlot.ItemDefinition != null && activeSlot.ItemDefinition.IsTool)
            {
                return activeSlot.ItemDefinition.AxeTier;
            }

            return AxeTier.None;
        }

        /// <summary>
        /// Returns whether flint exists in the shared inventory.
        /// </summary>
        /// <returns>True when at least one flint is available.</returns>
        public bool HasFlint()
        {
            return HasItem(_flintItem);
        }

        /// <summary>
        /// Returns whether firesteel exists in the shared inventory.
        /// </summary>
        /// <returns>True when at least one firesteel is available.</returns>
        public bool HasFiresteel()
        {
            return HasItem(_firesteelItem);
        }

        /// <summary>
        /// Returns whether any ignition source is available.
        /// </summary>
        /// <returns>True when the inventory can ignite the campfire.</returns>
        public bool HasIgnitionSource()
        {
            return HasFlint() || HasFiresteel();
        }

        /// <summary>
        /// Attempts to add logs into the shared inventory.
        /// </summary>
        /// <param name="amount">Number of logs to add.</param>
        /// <returns>True when the logs were added successfully.</returns>
        public bool TryAddLog(int amount)
        {
            return _inventorySystem != null
                && _logItem != null
                && _inventorySystem.AddItem(_logItem, amount);
        }

        /// <summary>
        /// Attempts to consume logs from the shared inventory.
        /// </summary>
        /// <param name="amount">Number of logs to consume.</param>
        /// <returns>True when enough logs were available.</returns>
        public bool TryConsumeLog(int amount)
        {
            return _inventorySystem != null
                && _logItem != null
                && _inventorySystem.RemoveItem(_logItem, amount);
        }

        private void OnActiveSlotChanged(ActiveSlotChangedEvent message)
        {
            _activeSlotIndex = message.CurrentSlotIndex;
        }

        private bool HasItem(ItemDefinition itemDefinition)
        {
            return _inventorySystem != null
                && itemDefinition != null
                && _inventorySystem.HasItem(itemDefinition, 1);
        }
    }
}
