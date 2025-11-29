using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryToFlashlightHandler : IItemDragHandler
{
    public bool CanHandleDrag(ItemData sourceItem, ItemData targetItem)
    {
        return sourceItem is BatteryItemData && targetItem is FlashlightItemData;
    }

    public void HandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        var battery = sourceSlot.itemData as BatteryItemData;

        if (targetSlot.instantiatedPrefab != null)
        {
            var flashlight = targetSlot.instantiatedPrefab.GetComponent<FlashlightBehavior>();
            if (flashlight != null)
            {
                flashlight.Recharge(battery.chargeAmount);
                targetSlot.UpdateUsage(flashlight.GetCurrentBattery());

                sourceSlot.quantity--;
                if (sourceSlot.quantity <= 0)
                    sourceSlot.ClearSlot();
                else
                    sourceSlot.UpdateQuantity(sourceSlot.quantity);
            }
        }
    }
}

