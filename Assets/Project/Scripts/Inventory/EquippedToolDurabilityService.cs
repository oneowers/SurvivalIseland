// Path: Assets/Project/Scripts/Inventory/EquippedToolDurabilityService.cs
// Purpose: Tracks the active inventory slot and consumes durability on the equipped tool after successful hits.
// Dependencies: VContainer.Unity, UnityEngine, ProjectResonance.Inventory.

using System;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Shared runtime service that owns equipped tool durability consumption.
    /// </summary>
    public sealed class EquippedToolDurabilityService : IStartable, IDisposable
    {
        private readonly InventorySystem _inventorySystem;
        private int _activeSlotIndex;

        /// <summary>
        /// Creates the equipped tool durability service.
        /// </summary>
        /// <param name="inventorySystem">Shared inventory runtime.</param>
        public EquippedToolDurabilityService(
            InventorySystem inventorySystem)
        {
            _inventorySystem = inventorySystem;
        }

        /// <summary>
        /// Starts listening for active-slot changes.
        /// </summary>
        public void Start()
        {
            if (_inventorySystem == null)
            {
                return;
            }

            _activeSlotIndex = _inventorySystem.ActiveSlotIndex;
            _inventorySystem.ActiveSlotChanged += OnActiveSlotChanged;
        }

        /// <summary>
        /// Releases subscriptions owned by this service.
        /// </summary>
        public void Dispose()
        {
            if (_inventorySystem != null)
            {
                _inventorySystem.ActiveSlotChanged -= OnActiveSlotChanged;
            }
        }

        /// <summary>
        /// Attempts to consume durability on the currently equipped tool after a successful hit.
        /// </summary>
        /// <param name="targetName">Target name used for logging.</param>
        /// <param name="amount">Durability amount to consume.</param>
        /// <returns>True when a durable tool consumed durability.</returns>
        public bool TryConsumeEquippedToolDurability(string targetName, int amount)
        {
            if (_inventorySystem == null)
            {
                return false;
            }

            var activeSlot = _inventorySystem.GetSlot(_activeSlotIndex);
            if (activeSlot.IsEmpty)
            {
                return false;
            }

            var equippedToolDefinition = activeSlot.ItemDefinition;
            if (equippedToolDefinition == null || !equippedToolDefinition.IsTool)
            {
                return false;
            }

            if (!equippedToolDefinition.UsesDurability)
            {
                return false;
            }

            return _inventorySystem.TryConsumeItemDurability(_activeSlotIndex, amount);
        }

        /// <summary>
        /// Attempts to get durability of the currently equipped tool.
        /// </summary>
        /// <param name="currentDurability">Resolved current durability.</param>
        /// <param name="maxDurability">Resolved maximum durability.</param>
        /// <returns>True when the active slot contains a durable tool.</returns>
        public bool TryGetEquippedToolDurability(out int currentDurability, out int maxDurability)
        {
            if (_inventorySystem != null)
            {
                return _inventorySystem.TryGetActiveDurability(_activeSlotIndex, out currentDurability, out maxDurability);
            }

            currentDurability = 0;
            maxDurability = 0;
            return false;
        }

        private void OnActiveSlotChanged(ActiveSlotChangedEvent message)
        {
            _activeSlotIndex = Mathf.Max(0, message.CurrentSlotIndex);
        }
    }
}
