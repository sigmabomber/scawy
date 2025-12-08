using UnityEngine;

public class BatteryToFlashlightHandler : IItemDragHandler
{
    public bool CanHandleDrag(ItemData sourceItem, ItemData targetItem)
    {
        // Check if dragging battery onto flashlight
        return sourceItem is BatteryItemData && targetItem is FlashlightItemData;
    }

    public void HandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        var battery = sourceSlot.itemData as BatteryItemData;
        var flashlightData = targetSlot.itemData as FlashlightItemData;

        if (battery == null || flashlightData == null)
            return;

        // Try to recharge the flashlight
        bool recharged = false;

        if (targetSlot.instantiatedPrefab != null)
        {
            var flashlight = targetSlot.instantiatedPrefab.GetComponent<FlashlightBehavior>();
            if (flashlight != null)
            {
                flashlight.Recharge(battery.chargeAmount);
                targetSlot.UpdateUsage(flashlight.GetCurrentBattery());
                recharged = true;
            }
        }
        else
        {
            // If no instantiated prefab, we might need to create one or just update UI
            // For now, we'll just show a message
            Debug.Log("Flashlight prefab not instantiated. Can't recharge.");
            return;
        }

        if (recharged)
        {
            // Consume one battery
            sourceSlot.quantity--;

            if (sourceSlot.quantity <= 0)
            {
                sourceSlot.ClearSlot();
                Debug.Log($"Used up battery. Recharged flashlight with {battery.chargeAmount} charge.");
            }
            else
            {
                sourceSlot.UpdateQuantity(sourceSlot.quantity);
                Debug.Log($"Used one battery ({sourceSlot.quantity} left). Recharged flashlight.");
            }
        }
    }
}