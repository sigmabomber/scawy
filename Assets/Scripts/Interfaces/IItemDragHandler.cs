using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemDragHandler
{
    bool CanHandleDrag(ItemData sourceItem, ItemData targetItem);
    void HandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot);
}