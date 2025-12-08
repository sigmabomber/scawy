using UnityEngine;
using System.Collections.Generic;

namespace Doody.InventoryFramework.Modules
{
    /// <summary>
    /// Module that handles drag and drop functionality with slot priority validation
    /// </summary>
    public class DragDropModule : IInventoryModule
    {
        public string ModuleName => "Drag & Drop Module";
        public bool IsEnabled { get; private set; }

        private IInventoryFramework framework;

        public void Initialize(IInventoryFramework framework)
        {
            this.framework = framework;
            IsEnabled = true;
            Debug.Log($"[{ModuleName}] Initialized");
        }

        public void Shutdown()
        {
            IsEnabled = false;
            Debug.Log($"[{ModuleName}] Shutdown");
        }

        public void Update(float deltaTime)
        {
            // No update logic needed for drag and drop
        }

        public void OnInventorySystemCreated(IInventorySystem system)
        {
            Debug.Log($"[{ModuleName}] New inventory system registered: {system.SystemId}");
        }

        /// <summary>
        /// Check if a slot can accept an item based on slot priority
        /// </summary>
        public bool CanSlotAcceptItem(IInventorySlotUI slot, ItemData itemData)
        {
            if (slot == null || itemData == null)
            {
                Debug.LogWarning($"[{ModuleName}] Slot or ItemData is null");
                return false;
            }

            if (slot is InventorySlotsUI inventorySlot)
            {
                bool isCompatible = inventorySlot.slotPriority == itemData.priority;

                Debug.Log($"[{ModuleName}] Slot Compatibility Check:\n" +
                         $"Slot: {inventorySlot.gameObject.name}, Priority: {inventorySlot.slotPriority}\n" +
                         $"Item: {itemData.itemName}, Priority: {itemData.priority}\n" +
                         $"Compatible: {isCompatible}");

                return isCompatible;
            }

            Debug.Log($"[{ModuleName}] Slot is not InventorySlotsUI, defaulting to allow");
            return true;
        }

        /// <summary>
        /// Handle swapping items between slots with priority validation
        /// </summary>
        public bool SwapSlots(IInventorySlotUI slotA, IInventorySlotUI slotB)
        {
            if (slotA == null || slotB == null)
            {
                Debug.LogWarning($"[{ModuleName}] Cannot swap null slots");
                return false;
            }

            if (slotA.ItemData == null && slotB.ItemData == null)
            {
                Debug.Log($"[{ModuleName}] Both slots are empty, nothing to swap");
                return true;
            }

            bool canMoveAtoB = true;
            bool canMoveBtoA = true;

            if (slotA.ItemData != null)
            {
                canMoveAtoB = CanSlotAcceptItem(slotB, slotA.ItemData);
                if (!canMoveAtoB)
                {
                    Debug.LogWarning($"[{ModuleName}] Cannot move item from Slot A to Slot B - priority mismatch");
                }
            }

            if (slotB.ItemData != null)
            {
                canMoveBtoA = CanSlotAcceptItem(slotA, slotB.ItemData);
                if (!canMoveBtoA)
                {
                    Debug.LogWarning($"[{ModuleName}] Cannot move item from Slot B to Slot A - priority mismatch");
                }
            }

            if (!canMoveAtoB || !canMoveBtoA)
            {
                SlotPriority slotAPriority = GetSlotPriority(slotA);
                SlotPriority slotBPriority = GetSlotPriority(slotB);

                Debug.LogWarning($"[{ModuleName}] Swap failed!\n" +
                    $"Slot A: Priority={slotAPriority}, Item={slotA.ItemData?.itemName} (Priority={slotA.ItemData?.priority})\n" +
                    $"Slot B: Priority={slotBPriority}, Item={slotB.ItemData?.itemName} (Priority={slotB.ItemData?.priority})");
                return false;
            }

            ItemData tempItemData = slotA.ItemData;
            int tempQuantity = slotA.Quantity;
            GameObject tempPrefab = slotA.InstantiatedPrefab;

            slotA.SetItem(slotB.ItemData, slotB.Quantity, slotB.InstantiatedPrefab);

            slotB.SetItem(tempItemData, tempQuantity, tempPrefab);

            Debug.Log($"[{ModuleName}] Successfully swapped items between slots");
            return true;
        }

        /// <summary>
        /// Get the slot priority from an IInventorySlotUI
        /// </summary>
        private SlotPriority GetSlotPriority(IInventorySlotUI slot)
        {
            if (slot is InventorySlotsUI inventorySlot)
            {
                return inventorySlot.slotPriority;
            }

            return SlotPriority.Normal;
        }

        /// <summary>
        /// Transfer item from source to target (with priority check)
        /// </summary>
        public bool TransferItem(IInventorySlotUI sourceSlot, IInventorySlotUI targetSlot)
        {
            if (sourceSlot == null || targetSlot == null)
            {
                Debug.LogWarning($"[{ModuleName}] Cannot transfer - null slots");
                return false;
            }

            if (sourceSlot.ItemData == null)
            {
                Debug.LogWarning($"[{ModuleName}] Source slot is empty, cannot transfer");
                return false;
            }

            if (!CanSlotAcceptItem(targetSlot, sourceSlot.ItemData))
            {
                SlotPriority targetSlotPriority = GetSlotPriority(targetSlot);
                Debug.LogWarning($"[{ModuleName}] Cannot transfer {sourceSlot.ItemData.itemName} " +
                    $"(Priority: {sourceSlot.ItemData.priority}) to slot with Priority: {targetSlotPriority}");
                return false;
            }

            if (targetSlot.ItemData == null)
            {
                targetSlot.SetItem(sourceSlot.ItemData, sourceSlot.Quantity, sourceSlot.InstantiatedPrefab);
                sourceSlot.ClearSlot();
                Debug.Log($"[{ModuleName}] Transferred item to empty slot");
                return true;
            }

            if (sourceSlot.ItemData == targetSlot.ItemData)
            {
                return TryStackItems(sourceSlot, targetSlot);
            }

            return SwapSlots(sourceSlot, targetSlot);
        }

        /// <summary>
        /// Handle stacking items
        /// </summary>
        public bool TryStackItems(IInventorySlotUI sourceSlot, IInventorySlotUI targetSlot)
        {
            if (sourceSlot == null || targetSlot == null)
            {
                Debug.LogWarning($"[{ModuleName}] Cannot stack - null slots");
                return false;
            }

            if (sourceSlot.ItemData != targetSlot.ItemData)
            {
                Debug.LogWarning($"[{ModuleName}] Cannot stack different items");
                return false;
            }

            if (targetSlot.ItemData.maxStack <= 1)
            {
                return false;
            }

            int spaceLeft = targetSlot.ItemData.maxStack - targetSlot.Quantity;
            if (spaceLeft <= 0)
            {
                return false;
            }

            int amountToMove = Mathf.Min(sourceSlot.Quantity, spaceLeft);

            // Update quantities
            targetSlot.UpdateQuantity(targetSlot.Quantity + amountToMove);

            int remainingInSource = sourceSlot.Quantity - amountToMove;
            if (remainingInSource > 0)
            {
                sourceSlot.UpdateQuantity(remainingInSource);
            }
            else
            {
                sourceSlot.ClearSlot();
            }

            return true;
        }

        /// <summary>
        /// Quick method to check if swap is possible
        /// </summary>
        public bool CanSwapSlots(IInventorySlotUI slotA, IInventorySlotUI slotB)
        {
            if (slotA == null || slotB == null)
            {
                return false;
            }

            if (slotA.ItemData == null && slotB.ItemData == null)
            {
                return true;
            }

            bool canMoveAtoB = true;
            bool canMoveBtoA = true;

            if (slotA.ItemData != null)
            {
                canMoveAtoB = CanSlotAcceptItem(slotB, slotA.ItemData);
            }

            if (slotB.ItemData != null)
            {
                canMoveBtoA = CanSlotAcceptItem(slotA, slotB.ItemData);
            }

            return canMoveAtoB && canMoveBtoA;
        }

        /// <summary>
        /// Find all slots that can accept a specific item
        /// </summary>
        public List<IInventorySlotUI> FindCompatibleSlots(IInventorySystem system, ItemData itemData)
        {
            List<IInventorySlotUI> compatibleSlots = new List<IInventorySlotUI>();

            foreach (var slot in system.GetAllSlots())
            {
                if (slot.ItemData == null && CanSlotAcceptItem(slot, itemData))
                {
                    compatibleSlots.Add(slot);
                }
            }

            Debug.Log($"[{ModuleName}] Found {compatibleSlots.Count} compatible slots for {itemData.itemName}");
            return compatibleSlots;
        }

        /// <summary>
        /// Find the best slot for an item
        /// </summary>
        public IInventorySlotUI FindBestSlotForItem(IInventorySystem system, ItemData itemData)
        {
            if (itemData.priority == SlotPriority.Dedicated)
            {
                foreach (var slot in system.GetAllSlots())
                {
                    if (slot.ItemData == null && CanSlotAcceptItem(slot, itemData))
                    {
                        return slot;
                    }
                }
                return null;
            }

            foreach (var slot in system.GetAllSlots())
            {
                if (slot.ItemData == null && CanSlotAcceptItem(slot, itemData))
                {
                    Debug.Log($"[{ModuleName}] Found normal slot for {itemData.itemName}");
                    return slot;
                }
            }

            Debug.Log($"[{ModuleName}] No compatible slot found for {itemData.itemName}");
            return null;
        }
    }
}