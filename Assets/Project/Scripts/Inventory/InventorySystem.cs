// Path: Assets/Project/Scpripts/Inventory/InventorySystem.cs
// Purpose: Owns the player's 10-slot inventory, stack rules, and inventory change events.
// Dependencies: UnityEngine, VContainer.

using System;
using System.Collections.Generic;
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

        [SerializeField]
        [Min(0)]
        private int _currentDurability;

        [SerializeField]
        [Min(0)]
        private int _maxDurability;

        /// <summary>
        /// Creates a new item stack.
        /// </summary>
        /// <param name="itemDefinition">Item definition stored in the stack.</param>
        /// <param name="count">Item count in the stack.</param>
        public ItemStack(ItemDefinition itemDefinition, int count, int currentDurability = 0, int maxDurability = 0)
        {
            if (itemDefinition == null || count <= 0)
            {
                _itemDefinition = null;
                _count = 0;
                _currentDurability = 0;
                _maxDurability = 0;
                return;
            }

            _itemDefinition = itemDefinition;
            _count = Mathf.Max(0, count);
            _maxDurability = Mathf.Max(0, maxDurability);
            _currentDurability = Mathf.Clamp(currentDurability, 0, _maxDurability);
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
        /// Gets the current runtime durability for the item in this slot.
        /// </summary>
        public int CurrentDurability => _currentDurability;

        /// <summary>
        /// Gets the maximum runtime durability for the item in this slot.
        /// </summary>
        public int MaxDurability => _maxDurability;

        /// <summary>
        /// Gets whether the slot item currently uses durability.
        /// </summary>
        public bool HasDurability => _maxDurability > 0;

        /// <summary>
        /// Gets the normalized durability ratio.
        /// </summary>
        public float DurabilityNormalized => HasDurability ? Mathf.Clamp01((float)_currentDurability / _maxDurability) : 0f;

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
            return new ItemStack(_itemDefinition, count, _currentDurability, _maxDurability);
        }

        /// <summary>
        /// Returns a copy with updated durability values.
        /// </summary>
        /// <param name="currentDurability">Updated current durability.</param>
        /// <param name="maxDurability">Updated maximum durability.</param>
        /// <returns>Updated stack copy.</returns>
        public ItemStack WithDurability(int currentDurability, int maxDurability)
        {
            return new ItemStack(_itemDefinition, _count, currentDurability, maxDurability);
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
        private readonly List<ItemStack> _slots;

        private int _version;
        private int _activeSlotIndex;

        /// <summary>
        /// Creates the player inventory system.
        /// </summary>
        /// <param name="inventoryConfig">Shared inventory authoring config.</param>
        public InventorySystem(InventoryConfig inventoryConfig)
        {
            _inventoryConfig = inventoryConfig;
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
        /// Gets the currently active quick slot index.
        /// </summary>
        public int ActiveSlotIndex => _activeSlotIndex;

        /// <summary>
        /// Raised when any slot content changes.
        /// </summary>
        public event Action<InventoryChangedEvent> InventoryChanged;

        /// <summary>
        /// Raised when the active quick slot changes.
        /// </summary>
        public event Action<ActiveSlotChangedEvent> ActiveSlotChanged;

        /// <summary>
        /// Initializes the fixed-size slot list and publishes the initial snapshot.
        /// </summary>
        public void Start()
        {
            EnsureInitialized();
            ApplyStartupLoadout();
            PublishInventoryChanged();
            PublishActiveSlotChanged(_activeSlotIndex, _activeSlotIndex);
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
            return AddItemInternal(itemDefinition, count, publishInventoryChanged: true);
        }

        /// <summary>
        /// Attempts to remove the item stored in a specific slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to clear.</param>
        /// <returns>True when the slot contained an item and was cleared.</returns>
        public bool RemoveItemAt(int slotIndex)
        {
            EnsureInitialized();

            if (slotIndex < 0 || slotIndex >= _slots.Count || _slots[slotIndex].IsEmpty)
            {
                return false;
            }

            var removedStack = _slots[slotIndex];
            _slots[slotIndex] = removedStack.Clear();
            PublishInventoryChanged();
            return true;
        }

        /// <summary>
        /// Attempts to consume items directly from the requested slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to mutate.</param>
        /// <param name="amount">Amount to remove from the slot.</param>
        /// <returns>True when the slot contained enough items and was updated.</returns>
        public bool TryConsumeItemAt(int slotIndex, int amount)
        {
            EnsureInitialized();

            if (amount <= 0 || slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return false;
            }

            var slot = _slots[slotIndex];
            if (slot.IsEmpty || slot.Count < amount)
            {
                return false;
            }

            var nextCount = slot.Count - amount;
            _slots[slotIndex] = nextCount > 0 ? slot.WithCount(nextCount) : slot.Clear();
            PublishInventoryChanged();
            return true;
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

        /// <summary>
        /// Attempts to get durability data for the requested slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to inspect.</param>
        /// <param name="currentDurability">Resolved current durability.</param>
        /// <param name="maxDurability">Resolved maximum durability.</param>
        /// <returns>True when the slot contains an item with durability.</returns>
        public bool TryGetActiveDurability(int slotIndex, out int currentDurability, out int maxDurability)
        {
            EnsureInitialized();

            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                var slot = _slots[slotIndex];
                if (slot.HasDurability)
                {
                    currentDurability = slot.CurrentDurability;
                    maxDurability = slot.MaxDurability;
                    return true;
                }
            }

            currentDurability = 0;
            maxDurability = 0;
            return false;
        }

        /// <summary>
        /// Attempts to consume durability from the item stored in the requested slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to mutate.</param>
        /// <param name="amount">Durability amount to consume.</param>
        /// <returns>True when durability was consumed.</returns>
        public bool TryConsumeItemDurability(int slotIndex, int amount)
        {
            EnsureInitialized();

            if (amount <= 0 || slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return false;
            }

            var slot = _slots[slotIndex];
            var itemDefinition = slot.ItemDefinition;
            if (slot.IsEmpty || !slot.HasDurability || itemDefinition == null || !itemDefinition.UsesDurability)
            {
                return false;
            }

            var nextDurability = Mathf.Max(0, slot.CurrentDurability - amount);
            if (nextDurability <= 0 && itemDefinition.BreaksOnZeroDurability)
            {
                _slots[slotIndex] = slot.Clear();
                PublishInventoryChanged();
                return true;
            }

            _slots[slotIndex] = slot.WithDurability(nextDurability, slot.MaxDurability);
            PublishInventoryChanged();
            return true;
        }

        /// <summary>
        /// Sets the currently active quick slot index.
        /// </summary>
        /// <param name="slotIndex">Requested slot index.</param>
        public void SetActiveSlot(int slotIndex)
        {
            EnsureInitialized();

            var clampedSlotIndex = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, _slots.Count - 1));
            if (_activeSlotIndex == clampedSlotIndex)
            {
                PublishActiveSlotChanged(_activeSlotIndex, _activeSlotIndex);
                return;
            }

            var previousSlotIndex = _activeSlotIndex;
            _activeSlotIndex = clampedSlotIndex;
            PublishActiveSlotChanged(previousSlotIndex, _activeSlotIndex);
        }

        private void EnsureInitialized()
        {
            while (_slots.Count < MaxSlots)
            {
                _slots.Add(new ItemStack(null, 0));
            }
        }

        private bool AddItemInternal(ItemDefinition itemDefinition, int count, bool publishInventoryChanged)
        {
            EnsureInitialized();

            if (!CanAddItem(itemDefinition, count))
            {
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
                        if (publishInventoryChanged)
                        {
                            PublishInventoryChanged();
                        }

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

                _slots[index] = CreateRuntimeStack(itemDefinition, amountToStore);
                remaining -= amountToStore;

                if (remaining <= 0)
                {
                    if (publishInventoryChanged)
                    {
                        PublishInventoryChanged();
                    }

                    return true;
                }
            }
            return false;
        }

        private void ApplyStartupLoadout()
        {
            if (_inventoryConfig == null || _inventoryConfig.StartupItems == null)
            {
                return;
            }

            for (var index = 0; index < _inventoryConfig.StartupItems.Count; index++)
            {
                var entry = _inventoryConfig.StartupItems[index];
                if (entry.ItemDefinition == null || entry.Count <= 0)
                {
                    continue;
                }

                if (!AddItemInternal(entry.ItemDefinition, entry.Count, publishInventoryChanged: false))
                {
                    continue;
                }
            }
        }

        private static ItemStack CreateRuntimeStack(ItemDefinition itemDefinition, int count)
        {
            if (itemDefinition != null && itemDefinition.UsesDurability)
            {
                return new ItemStack(itemDefinition, Mathf.Max(1, count), itemDefinition.MaxDurability, itemDefinition.MaxDurability);
            }

            return new ItemStack(itemDefinition, Mathf.Max(1, count));
        }

        private void PublishInventoryChanged()
        {
            _version++;
            InventoryChanged?.Invoke(new InventoryChangedEvent(_version));
        }

        private void PublishActiveSlotChanged(int previousSlotIndex, int currentSlotIndex)
        {
            ActiveSlotChanged?.Invoke(new ActiveSlotChangedEvent(previousSlotIndex, currentSlotIndex));
        }

    }
}
