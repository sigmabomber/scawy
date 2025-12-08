using System.Collections.Generic;
using UnityEngine;

namespace Doody.InventoryFramework
{
    /// <summary>
    /// Core interfaces for the inventory framework
    /// </summary>

    public interface IInventoryModule
    {
        string ModuleName { get; }
        bool IsEnabled { get; }

        void Initialize(IInventoryFramework framework);
        void Shutdown();
        void Update(float deltaTime);
        void OnInventorySystemCreated(IInventorySystem system);
    }

    public interface IInventoryFramework
    {
        void RegisterModule(IInventoryModule module);
        void UnregisterModule(IInventoryModule module);
        T GetModule<T>() where T : IInventoryModule;
        bool HasModule<T>() where T : IInventoryModule;
        IInventorySystem GetInventorySystem(string systemId);
        void RegisterInventorySystem(IInventorySystem system);
    }

    public interface IInventorySystem
    {
        string SystemId { get; }

        bool AddItem(ItemData itemData, int quantity, SlotPriority priority, GameObject itemObj);
        bool RemoveItem(ItemData itemData, int quantity);
        bool HasItem(ItemData itemData, int quantity);
        int GetItemCount(ItemData itemData);
        IInventorySlotUI GetSlot(int index);
        List<IInventorySlotUI> GetAllSlots();
        IInventorySlotUI GetEquippedSlot();
        void SetEquippedSlot(IInventorySlotUI slot);
    }

    public interface IInventorySlotUI
    {
        ItemData ItemData { get; }
        int Quantity { get; }
        GameObject InstantiatedPrefab { get; }

        void SetItem(ItemData itemData, int quantity, GameObject prefab);
        void UpdateQuantity(int newQuantity);
        void ClearSlot();
    }

   
}

public enum SlotPriority
{
    Normal,
    Dedicated
}

public enum ItemState
{
    InWorld,
    InInventory,
    Equipped
}