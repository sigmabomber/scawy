using UnityEngine;
using System.Collections.Generic;
using Doody.InventoryFramework;
using Doody.GameEvents;

/// <summary>
/// Individual storage container with save support
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
        InitializeSlots();

        if (string.IsNullOrEmpty(storageId))
        {
            storageId = $"storage_{GetInstanceID()}";
        }

        if (StorageManager.Instance != null)
        {
            StorageManager.Instance.RegisterStorage(this);
        }
        else
        {
            Debug.LogWarning("StorageManager not found. Creating one.");
            GameObject managerObj = new GameObject("StorageManager");
            managerObj.AddComponent<StorageManager>();
            StorageManager.Instance.RegisterStorage(this);
        }
    }

    private void OnDestroy()
    {
        if (StorageManager.Instance != null)
        {
            StorageManager.Instance.UnregisterStorage(this);
        }
    }

    private void InitializeSlots()
    {
        storedItems.Clear();
        for (int i = 0; i < slotCount; i++)
        {
            storedItems.Add(new StorageSlotData());
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

    /// <summary>
    /// Get save data for this storage container
    /// </summary>
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

    /// <summary>
    /// Load save data into this storage container
    /// </summary>
    public void LoadSaveData(StorageSaveData data)
    {
        if (data == null) return;

        // Clear all slots
        for (int i = 0; i < storedItems.Count; i++)
        {
            storedItems[i] = new StorageSlotData();
        }

        // Load saved items
        foreach (var slotData in data.slots)
        {
            if (slotData.slotIndex >= 0 && slotData.slotIndex < storedItems.Count)
            {
                ItemData item = FindItemByName(slotData.itemName);
                if (item != null)
                {
                    SetSlotData(slotData.slotIndex, item, slotData.quantity);
                }
                else
                {
                    Debug.LogWarning($"Item not found for storage {storageId}: {slotData.itemName}");
                }
            }
        }
    }

    private ItemData FindItemByName(string itemName)
    {
        if (ItemDatabaseManager.Instance == null)
        {
            Debug.LogError("ItemDatabaseManager.Instance is null!");
            return null;
        }

        // Get all items and find by name
        var allItems = ItemDatabaseManager.Instance.GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
                return item;
        }

        Debug.LogWarning($"Item not found: {itemName}");
        return null;
    }

    /// <summary>
    /// Clear all items from this storage container
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < storedItems.Count; i++)
        {
            ClearSlot(i);
        }
    }

    /// <summary>
    /// Get the number of occupied slots
    /// </summary>
    public int GetOccupiedSlotCount()
    {
        int count = 0;
        foreach (var slot in storedItems)
        {
            if (slot.itemData != null)
                count++;
        }
        return count;
    }
}