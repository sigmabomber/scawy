using System.Collections.Generic;

public class DragHandlerManager
{
    private List<IItemDragHandler> _handlers = new List<IItemDragHandler>();

    public DragHandlerManager()
    {
        _handlers.Add(new BatteryToFlashlightHandler());
    }

    public bool TryHandleDrag(InventorySlotsUI sourceSlot, InventorySlotsUI targetSlot)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandleDrag(sourceSlot.itemData, targetSlot.itemData))
            {
                handler.HandleDrag(sourceSlot, targetSlot);
                return true;
            }
        }
        return false;
    }
}
