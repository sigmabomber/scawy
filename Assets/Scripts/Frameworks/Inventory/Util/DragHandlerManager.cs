using Doody.InventoryFramework;
using Doody.InventoryFramework.Modules;
using System.Collections.Generic;
using UnityEngine;

public class DragHandlerManager
{
    private List<IItemDragHandler> customHandlers = new List<IItemDragHandler>();
    private DragDropModule dragDropModule;

    public DragHandlerManager()
    {
        if (InventoryFramework.Instance != null)
        {
            dragDropModule = InventoryFramework.Instance.GetModule<DragDropModule>();
            if (dragDropModule != null)
            {
            }
            else
            {
                Debug.LogWarning("DragHandlerManager: DragDropModule not found in framework");
            }
        }

        RegisterDefaultHandlers();
    }

    private void RegisterDefaultHandlers()
    {
        RegisterHandler(new BatteryToFlashlightHandler());

    }

    public void RegisterHandler(IItemDragHandler handler)
    {
        if (handler != null && !customHandlers.Contains(handler))
        {
            customHandlers.Add(handler);
            Debug.Log($"Registered drag handler: {handler.GetType().Name}");
        }
    }

    public void UnregisterHandler(IItemDragHandler handler)
    {
        customHandlers.Remove(handler);
    }

    public bool TryHandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        if (sourceSlot == null || targetSlot == null)
            return false;

        if (!CheckPriorityCompatibility(sourceSlot, targetSlot))
        {
            Debug.LogWarning($"Drag blocked: Priority mismatch between slots");
            return false;
        }

        foreach (var handler in customHandlers)
        {
            if (handler.CanHandleDrag(sourceSlot.itemData, targetSlot.itemData))
            {
                handler.HandleDrag(sourceSlot, targetSlot);
                Debug.Log($"Used custom drag handler: {handler.GetType().Name}");
                return true;
            }
        }

        return PerformFrameworkSwap(sourceSlot, targetSlot);
    }

    /// <summary>
    /// Check if items can be swapped between slots based on priority
    /// </summary>
    private bool CheckPriorityCompatibility(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        if (dragDropModule != null)
        {
            return dragDropModule.CanSwapSlots(sourceSlot, targetSlot);
        }

        if (sourceSlot.itemData != null)
        {
            if (!CanSlotAcceptItem(targetSlot, sourceSlot.itemData))
            {
                Debug.Log($"Cannot move {sourceSlot.itemData.itemName} to {targetSlot.gameObject.name} - priority mismatch");
                return false;
            }
        }

        if (targetSlot.itemData != null)
        {
            if (!CanSlotAcceptItem(sourceSlot, targetSlot.itemData))
            {
                Debug.Log($"Cannot move {targetSlot.itemData.itemName} to {sourceSlot.gameObject.name} - priority mismatch");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Fallback priority check without framework
    /// </summary>
    private bool CanSlotAcceptItem(InventorySlotsUI slot, ItemData itemData)
    {
        if (slot == null || itemData == null) return false;

        return slot.slotPriority == itemData.priority;
    }

    /// <summary>
    /// Use framework's swap method which includes priority validation
    /// </summary>
    private bool PerformFrameworkSwap(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        if (dragDropModule != null)
        {
            bool success = dragDropModule.SwapSlots(sourceSlot, targetSlot);

            if (success)
            {
            }
            else
            {
                Debug.LogWarning($"Framework prevented swap due to priority mismatch");
            }

            return success;
        }

        // Fallback to default swap if no framework
        return PerformDefaultSwap(sourceSlot, targetSlot);
    }

    /// <summary>
    /// Old default swap (should only be used as fallback)
    /// </summary>
    private bool PerformDefaultSwap(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {

        ItemData sourceItem = sourceSlot.itemData;
        int sourceQuantity = sourceSlot.quantity;
        GameObject sourcePrefab = sourceSlot.instantiatedPrefab;

        ItemData targetItem = targetSlot.itemData;
        int targetQuantity = targetSlot.quantity;
        GameObject targetPrefab = targetSlot.instantiatedPrefab;

        if (targetItem != null)
        {
            sourceSlot.SetItem(targetItem, targetQuantity, targetPrefab);
        }
        else
        {
            sourceSlot.ClearSlot();
        }

        if (sourceItem != null)
        {
            targetSlot.SetItem(sourceItem, sourceQuantity, sourcePrefab);
        }
        else
        {
            targetSlot.ClearSlot();
        }

        return true;
    }

    /// <summary>
    /// Alternative: Transfer item with priority check (for drag to empty slot)
    /// </summary>
    public bool TryTransferItem(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        if (sourceSlot == null || targetSlot == null || sourceSlot.itemData == null)
            return false;

        if (dragDropModule != null)
        {
            return dragDropModule.TransferItem(sourceSlot, targetSlot);
        }

        if (!CanSlotAcceptItem(targetSlot, sourceSlot.itemData))
        {
            Debug.LogWarning($"Cannot transfer {sourceSlot.itemData.itemName} to {targetSlot.gameObject.name} - priority mismatch");
            return false;
        }

        if (targetSlot.itemData == null)
        {
            targetSlot.SetItem(sourceSlot.itemData, sourceSlot.quantity, sourceSlot.instantiatedPrefab);
            sourceSlot.ClearSlot();
            Debug.Log($"Transferred {sourceSlot.itemData.itemName} to empty slot");
            return true;
        }

        return PerformDefaultSwap(sourceSlot, targetSlot);
    }
}