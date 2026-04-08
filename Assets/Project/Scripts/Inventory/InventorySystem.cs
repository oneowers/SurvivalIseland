// Path: Assets/Project/Scpripts/Inventory/InventorySystem.cs
// Purpose: Owns the player's 10-slot inventory, stack rules, and inventory change events.
// Dependencies: MessagePipe, UnityEngine, VContainer.

using System;
using System.Collections.Generic;
using System.Text;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Runtime item stack stored in inventory slots and recipe ingredients.
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        [SerializeField]
        private ItemDefinition _itemDefinition;

        [SerializeField]
        [Min(0)]
        private int _count;

        /// <summary>
        /// Creates a new item stack.
        /// </summary>
        /// <param name="itemDefinition">Item definition stored in the stack.</param>
        /// <param name="count">Item count in the stack.</param>
        public ItemStack(ItemDefinition itemDefinition, int count)
        {
            _itemDefinition = itemDefinition;
            _count = Mathf.Max(0, count);
        }

        /// <summary>
        /// Gets the item definition stored in this slot.
        /// </summary>
        public ItemDefinition ItemDefinition => _itemDefinition;

        /// <summary>
        /// Gets the current stack size.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets whether the stack is empty.
        /// </summary>
        public bool IsEmpty => _itemDefinition == null || _count <= 0;

        /// <summary>
        /// Returns a copy with a different item count.
        /// </summary>
        /// <param name="count">New stack size.</param>
        /// <returns>Updated stack copy.</returns>
        public ItemStack WithCount(int count)
        {
            return new ItemStack(_itemDefinition, count);
        }

        /// <summary>
        /// Returns an empty copy of this stack.
        /// </summary>
        /// <returns>Empty stack.</returns>
        public ItemStack Clear()
        {
            return new ItemStack(null, 0);
        }
    }

    /// <summary>
    /// Published when any slot content changes.
    /// </summary>
    public readonly struct InventoryChangedEvent
    {
        /// <summary>
        /// Creates a new inventory changed event.
        /// </summary>
        /// <param name="version">Monotonic inventory version.</param>
        public InventoryChangedEvent(int version)
        {
            Version = version;
        }

        /// <summary>
        /// Gets the current inventory version.
        /// </summary>
        public int Version { get; }
    }

    /// <summary>
    /// Published when the active quick slot changes.
    /// </summary>
    public readonly struct ActiveSlotChangedEvent
    {
        /// <summary>
        /// Creates a new active slot change event.
        /// </summary>
        /// <param name="previousSlotIndex">Previously active slot index.</param>
        /// <param name="currentSlotIndex">Currently active slot index.</param>
        public ActiveSlotChangedEvent(int previousSlotIndex, int currentSlotIndex)
        {
            PreviousSlotIndex = previousSlotIndex;
            CurrentSlotIndex = currentSlotIndex;
        }

        /// <summary>
        /// Gets the previously active slot index.
        /// </summary>
        public int PreviousSlotIndex { get; }

        /// <summary>
        /// Gets the currently active slot index.
        /// </summary>
        public int CurrentSlotIndex { get; }
    }

    /// <summary>
    /// Runtime inventory service used by gameplay systems.
    /// </summary>
    public sealed class InventorySystem : IStartable
    {
        private readonly InventoryConfig _inventoryConfig;
        private readonly IBufferedPublisher<InventoryChangedEvent> _inventoryChangedPublisher;
        private readonly List<ItemStack> _slots;

        private int _version;

        /// <summary>
        /// Creates the player inventory system.
        /// </summary>
        /// <param name="inventoryConfig">Shared inventory authoring config.</param>
        /// <param name="inventoryChangedPublisher">Buffered inventory change publisher.</param>
        public InventorySystem(InventoryConfig inventoryConfig, IBufferedPublisher<InventoryChangedEvent> inventoryChangedPublisher)
        {
            _inventoryConfig = inventoryConfig;
            _inventoryChangedPublisher = inventoryChangedPublisher;
            MaxSlots = Mathf.Max(1, _inventoryConfig != null ? _inventoryConfig.MaxSlots : 10);
            _slots = new List<ItemStack>(MaxSlots);
        }

        /// <summary>
        /// Gets the maximum number of inventory slots.
        /// </summary>
        public int MaxSlots { get; }

        /// <summary>
        /// Gets the runtime slot collection.
        /// </summary>
        public IReadOnlyList<ItemStack> Slots => _slots;

        /// <summary>
        /// Initializes the fixed-size slot list and publishes the initial snapshot.
        /// </summary>
        public void Start()
        {
            EnsureInitialized();
            PublishInventoryChanged();
        }

        /// <summary>
        /// Gets a copy of a slot at the requested index.
        /// </summary>
        /// <param name="slotIndex">Slot index to read.</param>
        /// <returns>Resolved slot copy, or an empty slot when the index is invalid.</returns>
        public ItemStack GetSlot(int slotIndex)
        {
            EnsureInitialized();

            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return new ItemStack(null, 0);
            }

            return _slots[slotIndex];
        }

        /// <summary>
        /// Returns whether the provided item count fits into the current inventory.
        /// </summary>
        /// <param name="itemDefinition">Item definition to test.</param>
        /// <param name="count">Item count to test.</param>
        /// <returns>True when the inventory has enough capacity.</returns>
        public bool CanAddItem(ItemDefinition itemDefinition, int count)
        {
            EnsureInitialized();

            if (itemDefinition == null || count <= 0)
            {
                return false;
            }

            var remaining = count;
            var freeSlotCount = 0;

            for (var index = 0; index < _slots.Count; index++)
            {
                var slot = _slots[index];
                if (slot.IsEmpty)
                {
                    freeSlotCount++;
                    continue;
                }

                if (!itemDefinition.IsStackable || slot.ItemDefinition != itemDefinition)
                {
                    continue;
                }

                // Existing stacks are filled first so we keep empty slots available for non-stackable items.
                var freeSpaceInStack = Mathf.Max(0, itemDefinition.MaxStackSize - slot.Count);
                remaining -= freeSpaceInStack;
                if (remaining <= 0)
                {
                    return true;
                }
            }

            var capacityPerEmptySlot = itemDefinition.IsStackable ? itemDefinition.MaxStackSize : 1;
            remaining -= freeSlotCount * capacityPerEmptySlot;
            return remaining <= 0;
        }

        /// <summary>
        /// Attempts to add items to the inventory.
        /// </summary>
        /// <param name="itemDefinition">Item to add.</param>
        /// <param name="count">Amount to add.</param>
        /// <returns>True when the full amount was added.</returns>
        public bool AddItem(ItemDefinition itemDefinition, int count)
        {
            EnsureInitialized();

            if (!CanAddItem(itemDefinition, count))
            {
                Debug.LogWarning(
                    $"[InventorySystem] AddItem failed. Item={(itemDefinition != null ? itemDefinition.DisplayName : "null")}, Count={count}, Snapshot={BuildInventorySnapshot()}");
                return false;
            }

            var remaining = count;

            if (itemDefinition.IsStackable)
            {
                for (var index = 0; index < _slots.Count; index++)
                {
                    var slot = _slots[index];
                    if (slot.IsEmpty || slot.ItemDefinition != itemDefinition)
                    {
                        continue;
                    }

                    var freeSpace = Mathf.Max(0, itemDefinition.MaxStackSize - slot.Count);
                    if (freeSpace <= 0)
                    {
                        continue;
                    }

                    var amountToMove = Mathf.Min(remaining, freeSpace);
                    _slots[index] = slot.WithCount(slot.Count + amountToMove);
                    remaining -= amountToMove;

                    if (remaining <= 0)
                    {
                        PublishInventoryChanged();
                        LogInventoryAdded(itemDefinition, count);
                        return true;
                    }
                }
            }

            for (var index = 0; index < _slots.Count; index++)
            {
                if (!_slots[index].IsEmpty)
                {
                    continue;
                }

                var amountToStore = itemDefinition.IsStackable
                    ? Mathf.Min(remaining, itemDefinition.MaxStackSize)
                    : 1;

                _slots[index] = new ItemStack(itemDefinition, amountToStore);
                remaining -= amountToStore;

                if (remaining <= 0)
                {
                    PublishInventoryChanged();
                    LogInventoryAdded(itemDefinition, count);
                    return true;
                }
            }

            Debug.LogWarning(
                $"[InventorySystem] AddItem reached unexpected incomplete state. Item={(itemDefinition != null ? itemDefinition.DisplayName : "null")}, Count={count}, Remaining={remaining}, Snapshot={BuildInventorySnapshot()}");
            return false;
        }

        /// <summary>
        /// Attempts to remove items from the inventory.
        /// </summary>
        /// <param name="itemDefinition">Item to remove.</param>
        /// <param name="count">Amount to remove.</param>
        /// <returns>True when the full amount was removed.</returns>
        public bool RemoveItem(ItemDefinition itemDefinition, int count)
        {
            EnsureInitialized();

            if (!HasItem(itemDefinition, count))
            {
                return false;
            }

            var remaining = count;

            for (var index = _slots.Count - 1; index >= 0; index--)
            {
                var slot = _slots[index];
                if (slot.IsEmpty || slot.ItemDefinition != itemDefinition)
                {
                    continue;
                }

                var amountToRemove = Mathf.Min(remaining, slot.Count);
                var nextCount = slot.Count - amountToRemove;
                _slots[index] = nextCount > 0 ? slot.WithCount(nextCount) : slot.Clear();
                remaining -= amountToRemove;

                if (remaining <= 0)
                {
                    PublishInventoryChanged();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the inventory contains at least the requested amount.
        /// </summary>
        /// <param name="itemDefinition">Item definition to query.</param>
        /// <param name="count">Required amount.</param>
        /// <returns>True when the inventory contains enough items.</returns>
        public bool HasItem(ItemDefinition itemDefinition, int count)
        {
            EnsureInitialized();

            if (itemDefinition == null || count <= 0)
            {
                return false;
            }

            var totalCount = 0;
            for (var index = 0; index < _slots.Count; index++)
            {
                var slot = _slots[index];
                if (slot.IsEmpty || slot.ItemDefinition != itemDefinition)
                {
                    continue;
                }

                totalCount += slot.Count;
                if (totalCount >= count)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureInitialized()
        {
            while (_slots.Count < MaxSlots)
            {
                _slots.Add(new ItemStack(null, 0));
            }
        }

        private void PublishInventoryChanged()
        {
            _version++;
            _inventoryChangedPublisher.Publish(new InventoryChangedEvent(_version));
        }

        private void LogInventoryAdded(ItemDefinition itemDefinition, int count)
        {
            Debug.Log(
                $"[InventorySystem] Added {count}x {(itemDefinition != null ? itemDefinition.DisplayName : "null")}. Snapshot={BuildInventorySnapshot()}");
        }

        private string BuildInventorySnapshot()
        {
            EnsureInitialized();

            var builder = new StringBuilder(128);
            builder.Append('[');

            for (var index = 0; index < _slots.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" | ");
                }

                var slot = _slots[index];
                builder.Append(index);
                builder.Append(':');

                if (slot.IsEmpty)
                {
                    builder.Append("Empty");
                    continue;
                }

                builder.Append(slot.ItemDefinition != null ? slot.ItemDefinition.DisplayName : "null");
                builder.Append('x');
                builder.Append(slot.Count);
            }

            builder.Append(']');
            return builder.ToString();
        }
    }
}
