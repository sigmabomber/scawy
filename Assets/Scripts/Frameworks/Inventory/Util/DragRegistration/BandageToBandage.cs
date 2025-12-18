using Doody.InventoryFramework;
using UnityEngine;

public class BandageToBandageHandler : IItemDragHandler
{
    private HealingItemData medkitData;

    public BandageToBandageHandler(HealingItemData medkitData)
    {
        this.medkitData = medkitData;

        if (medkitData == null)
        {
            Debug.LogError("BandageToBandageHandler: medkitData is null! Make sure to pass a valid medkit ItemData.");
        }
    }

    public bool CanHandleDrag(ItemData sourceItem, ItemData targetItem)
    {
        if (sourceItem is HealingItemData && targetItem is HealingItemData)
        {
            return sourceItem.itemName == "Bandage" && targetItem.itemName == "Bandage";
        }
        return false;
    }

    public void HandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        var sourceBandage = sourceSlot.itemData as HealingItemData;
        var targetBandage = targetSlot.itemData as HealingItemData;

        if (sourceBandage == null || targetBandage == null)
        {
            Debug.LogWarning("Both items must be HealingItemData (bandages)");
            return;
        }

        if (medkitData == null)
        {
            Debug.LogError("Medkit ItemData not assigned! Cannot craft medkit.");
            return;
        }

        sourceSlot.quantity--;
        if (sourceSlot.quantity <= 0)
        {
            sourceSlot.ClearSlot();
        }
        else
        {
            sourceSlot.UpdateQuantity(sourceSlot.quantity);
        }

        targetSlot.ClearSlot();

        var inventorySystem = InventorySystem.Instance;
        if (inventorySystem != null)
        {
            bool success = inventorySystem.AddItem(
                medkitData,
                quantity: 1,
                slotPriority: medkitData.priority
            );

            if (success)
            {
               
            }
            else
            {
                Debug.LogWarning("Failed to add medkit to inventory (inventory full?)");
            }
        }
        else
        {
            Debug.LogError("InventorySystem.Instance is null!");
        }
    }
}