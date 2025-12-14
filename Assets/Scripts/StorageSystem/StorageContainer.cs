using UnityEngine;
using System.Collections.Generic;
using Doody.InventoryFramework;
using Doody.GameEvents;

/// <summary>
/// Individual storage container - stores its own data
/// Uses shared UI when opened via interaction system
/// </summary>
public class StorageContainer : MonoBehaviour, IInteractable
{
    [Header("Storage Settings")]
    [SerializeField] private string storageId;
    [SerializeField] private int slotCount = 20;

    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Press E to open storage";
    [SerializeField] private Sprite interactionIcon;

    // Storage data 
    private List<StorageSlotData> storedItems = new List<StorageSlotData>();

    private void Start()
    {
        for (int i = 0; i < slotCount; i++)
        {
            storedItems.Add(new StorageSlotData());
        }

        if (string.IsNullOrEmpty(storageId))
        {
            storageId = $"storage_{GetInstanceID()}";
        }
    }

    // IInteractable implementation
    public string GetInteractionPrompt() => interactionPrompt;
    public Sprite GetInteractionIcon() => interactionIcon;

    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        StorageUIManager.Instance?.OpenStorage(this);
    }

    public string StorageId => storageId;
    public int SlotCount => slotCount;

    public StorageSlotData GetSlotData(int index)
    {
        if (index < 0 || index >= storedItems.Count)
            return null;
        return storedItems[index];
    }

    public void SetSlotData(int index, ItemData itemData, int quantity, GameObject prefab = null)
    {
        if (index < 0 || index >= storedItems.Count) return;

        storedItems[index].itemData = itemData;
        storedItems[index].quantity = quantity;
        storedItems[index].itemPrefab = prefab;

        Events.Publish(new StorageSlotChangedEvent(storageId, index, itemData, quantity));
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= storedItems.Count) return;

        storedItems[index].itemData = null;
        storedItems[index].quantity = 0;
        storedItems[index].itemPrefab = null;

        Events.Publish(new StorageSlotChangedEvent(storageId, index, null, 0));
    }

    public bool HasEmptySlot(out int emptyIndex)
    {
        for (int i = 0; i < storedItems.Count; i++)
        {
            if (storedItems[i].itemData == null)
            {
                emptyIndex = i;
                return true;
            }
        }
        emptyIndex = -1;
        return false;
    }

    public List<StorageSlotData> GetAllSlots()
    {
        return new List<StorageSlotData>(storedItems);
    }

    // For saving
    public StorageSaveData GetSaveData()
    {
        StorageSaveData data = new StorageSaveData
        {
            storageId = this.storageId,
            slots = new List<StorageSlotSaveData>()
        };

        for (int i = 0; i < storedItems.Count; i++)
        {
            if (storedItems[i].itemData != null)
            {
                data.slots.Add(new StorageSlotSaveData
                {
                    slotIndex = i,
                    itemName = storedItems[i].itemData.itemName,
                    quantity = storedItems[i].quantity
                });
            }
        }

        return data;
    }

    public void LoadSaveData(StorageSaveData data)
    {
        // Clear all slots
        for (int i = 0; i < storedItems.Count; i++)
        {
            storedItems[i] = new StorageSlotData();
        }

        // Load saved items
        foreach (var slotData in data.slots)
        {
            // ItemData item = ItemDatabase.GetItemByName(slotData.itemName);
            // SetSlotData(slotData.slotIndex, item, slotData.quantity);
        }
    }
}



