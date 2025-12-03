using UnityEngine;

public interface IItemUsable
{
    void OnUse(InventorySlotsUI slot);
    void OnEquip(InventorySlotsUI slot);
    void OnUnequip(InventorySlotsUI slot);

    void OnItemStateChanged(ItemState previousState, ItemState newState);
    void OnDroppedInWorld();
    void OnPickedUp();
}

