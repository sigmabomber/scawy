public interface IItemUsable
{
    void OnEquip(InventorySlotsUI slot);
    void OnUse(InventorySlotsUI slot);
    void OnUnequip(InventorySlotsUI slot);

    void OnItemStateChanged(ItemState previousState, ItemState newState);

    void OnDroppedInWorld();
}