// Data classes
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StorageSlotData
{
    public ItemData itemData;
    public int quantity;
    public GameObject itemPrefab;
}

[System.Serializable]
public class StorageSaveData
{
    public string storageId;
    public List<StorageSlotSaveData> slots;
}

[System.Serializable]
public class StorageSlotSaveData
{
    public int slotIndex;
    public string itemName;
    public int quantity;
}

// Events 
namespace Doody.InventoryFramework
{
    public class StorageOpenedEvent
    {
        public string StorageId { get; private set; }

        public StorageOpenedEvent(string storageId)
        {
            StorageId = storageId;
        }
    }

    public class StorageClosedEvent
    {
        public string StorageId { get; private set; }

        public StorageClosedEvent(string storageId)
        {
            StorageId = storageId;
        }
    }

    public class StorageSlotChangedEvent
    {
        public string StorageId { get; private set; }
        public int SlotIndex { get; private set; }
        public ItemData ItemData { get; private set; }
        public int Quantity { get; private set; }

        public StorageSlotChangedEvent(string storageId, int slotIndex, ItemData itemData, int quantity)
        {
            StorageId = storageId;
            SlotIndex = slotIndex;
            ItemData = itemData;
            Quantity = quantity;
        }
    }
}