using UnityEngine;
using Doody.GameEvents;
using System.Collections.Generic;

/// <summary>
/// Handles save/load events for storage containers
/// </summary>
public class StorageSaveHandler : MonoBehaviour
{
    [Header("Settings")]
    public string systemName = "StorageManager";
    public bool debugMode = true;

    void Awake()
    {
        // Subscribe to save/load events
        Events.Subscribe<SaveDataRequestEvent>(OnSaveRequested, this);
        Events.Subscribe<LoadDataEvent>(OnLoadRequested, this);

        if (debugMode) Debug.Log($"StorageSaveHandler ready: {systemName}");
    }

    void OnDestroy()
    {
        Events.Unsubscribe<SaveDataRequestEvent>(OnSaveRequested);
        Events.Unsubscribe<LoadDataEvent>(OnLoadRequested);
    }

    private void OnSaveRequested(SaveDataRequestEvent saveEvent)
    {
        if (debugMode) Debug.Log($"Storage saving requested for slot {saveEvent.saveSlot}");

        StorageSaveData saveData = new StorageSaveData
        {
            containers = new List<ContainerSaveData>()
        };

        // Get all storage containers from StorageManager
        if (StorageManager.Instance != null)
        {
            var allContainers = StorageManager.Instance.GetAllStorageContainers();

            foreach (var container in allContainers)
            {
                var containerData = new ContainerSaveData
                {
                    storageId = container.StorageId,
                    slotCount = container.SlotCount,
                    slots = new List<ContainerSlotSaveData>()
                };

                var allSlots = container.GetAllSlots();
                for (int i = 0; i < allSlots.Count; i++)
                {
                    var slot = allSlots[i];
                    if (slot.itemData != null)
                    {
                        containerData.slots.Add(new ContainerSlotSaveData
                        {
                            slotIndex = i,
                            itemName = slot.itemData.itemName,
                            quantity = slot.quantity
                        });
                    }
                }

                saveData.containers.Add(containerData);
            }
        }

        string jsonData = JsonUtility.ToJson(saveData);

        Events.Publish(new SaveDataResponseEvent
        {
            systemName = systemName,
            saveData = jsonData,
            totalSystems = 1,
            responseTime = System.DateTime.Now
        });

        if (debugMode) Debug.Log($"Sent storage save data: {saveData.containers.Count} containers");
    }

    private void OnLoadRequested(LoadDataEvent loadEvent)
    {
        if (debugMode) Debug.Log($"Storage loading requested for slot {loadEvent.saveSlot}");

        if (loadEvent.systemData != null && loadEvent.systemData.ContainsKey(systemName))
        {
            string jsonData = loadEvent.systemData[systemName];
            StorageSaveData saveData = JsonUtility.FromJson<StorageSaveData>(jsonData);

            if (saveData != null)
            {
                LoadStorageData(saveData);

                if (debugMode) Debug.Log($"Storage loaded: {saveData.containers.Count} containers");
            }
        }
    }

    private void LoadStorageData(StorageSaveData saveData)
    {
        if (StorageManager.Instance == null) return;

        // First, clear all existing storage containers
        var existingContainers = StorageManager.Instance.GetAllStorageContainers();
        foreach (var container in existingContainers)
        {
            container.ClearAllSlots();
        }

        // Load saved containers
        foreach (var containerData in saveData.containers)
        {
            var container = StorageManager.Instance.GetStorageById(containerData.storageId);
            if (container != null)
            {
                // Make sure container has enough slots
                if (container.SlotCount < containerData.slotCount)
                {
                    Debug.LogWarning($"Storage container {containerData.storageId} has fewer slots ({container.SlotCount}) than saved ({containerData.slotCount})");
                }

                // Load items
                foreach (var slotData in containerData.slots)
                {
                    if (slotData.slotIndex < container.SlotCount)
                    {
                        ItemData item = FindItemByName(slotData.itemName);
                        if (item != null)
                        {
                            container.SetSlotData(slotData.slotIndex, item, slotData.quantity);
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find item for storage {containerData.storageId}: {slotData.itemName}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Storage container not found: {containerData.storageId}");
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
}

// Storage save data structures


[System.Serializable]
public class ContainerSaveData
{
    public string storageId;
    public int slotCount;
    public List<ContainerSlotSaveData> slots;
}

[System.Serializable]
public class ContainerSlotSaveData
{
    public int slotIndex;
    public string itemName;
    public int quantity;
}