using UnityEngine;
using Doody.GameEvents;
using System.Collections.Generic;

/// <summary>
/// INVENTORY SAVE HANDLER - Updated with ItemDatabaseManager
/// </summary>
public class InventorySaveHandler : MonoBehaviour
{
    [Header("Settings")]
    public string systemName = "InventorySystem";
    public bool debugMode = true;

    // Reference to your InventorySystem
    private InventorySystem inventorySystem;

    void Awake()
    {
        inventorySystem = FindAnyObjectByType<InventorySystem>();
        if (inventorySystem == null)
        {
            Debug.LogError("❌ InventorySaveHandler requires InventorySystem component!");
            return;
        }

        // Subscribe to save/load events
        Events.Subscribe<SaveDataRequestEvent>(OnSaveRequested, this);
        Events.Subscribe<LoadDataEvent>(OnLoadRequested, this);

        if (debugMode) Debug.Log($"✅ InventorySaveHandler ready: {systemName}");
    }

    void OnDestroy()
    {
        Events.Unsubscribe<SaveDataRequestEvent>(OnSaveRequested);
        Events.Unsubscribe<LoadDataEvent>(OnLoadRequested);
    }

    private void OnSaveRequested(SaveDataRequestEvent saveEvent)
    {
        if (debugMode) Debug.Log($"📤 Inventory saving requested for slot {saveEvent.saveSlot}");

        // Get save data from inventory system
        InventorySaveData saveData = GetInventorySaveData();

        // Convert to JSON
        string jsonData = JsonUtility.ToJson(saveData);

        // Send response back
        Events.Publish(new SaveDataResponseEvent
        {
            systemName = systemName,
            saveData = jsonData,
            totalSystems = 1,
            responseTime = System.DateTime.Now
        });

        if (debugMode) Debug.Log($"📨 Sent inventory save data: {saveData.normalSlots.Count} normal + {saveData.dedicatedSlots.Count} dedicated slots");
    }

    private void OnLoadRequested(LoadDataEvent loadEvent)
    {
        if (debugMode) Debug.Log($"📥 Inventory loading requested for slot {loadEvent.saveSlot}");

        // Check if our system data exists in the save
        if (loadEvent.systemData != null && loadEvent.systemData.ContainsKey(systemName))
        {
            string jsonData = loadEvent.systemData[systemName];

            // Parse the save data
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);

            if (saveData != null)
            {
                // Load the inventory data
                LoadInventoryFromSave(saveData);

                if (debugMode) Debug.Log($"✅ Inventory loaded: {saveData.normalSlots.Count + saveData.dedicatedSlots.Count} slots");
            }
        }
    }

    private InventorySaveData GetInventorySaveData()
    {
        InventorySaveData saveData = new InventorySaveData();

        // Save normal inventory slots
        if (inventorySystem.normalInventorySlots != null)
        {
            foreach (var slot in inventorySystem.normalInventorySlots)
            {
                var slotData = new InventorySlotSaveData();

                if (slot.itemData != null)
                {
                    // Save both item name AND item ID for better lookup
                    slotData.itemName = slot.itemData.itemName;
                    slotData.quantity = slot.quantity;
                    slotData.isEmpty = false;

                    // Try to find item ID using ItemDatabaseManager
                    string itemId = FindItemIdByName(slot.itemData.itemName);
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        slotData.itemId = itemId;
                    }
                }
                else
                {
                    slotData.isEmpty = true;
                }

                saveData.normalSlots.Add(slotData);
            }
        }

        // Save dedicated inventory slots
        if (inventorySystem.dedicatedInventorySlots != null)
        {
            foreach (var slot in inventorySystem.dedicatedInventorySlots)
            {
                var slotData = new InventorySlotSaveData();

                if (slot.itemData != null)
                {
                    slotData.itemName = slot.itemData.itemName;
                    slotData.quantity = slot.quantity;
                    slotData.isEmpty = false;

                    // Save item ID
                    string itemId = FindItemIdByName(slot.itemData.itemName);
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        slotData.itemId = itemId;
                    }
                }
                else
                {
                    slotData.isEmpty = true;
                }

                saveData.dedicatedSlots.Add(slotData);
            }
        }

        // Save currently equipped slot
        if (inventorySystem.currentlyEquippedSlot != null &&
            inventorySystem.currentlyEquippedSlot.itemData != null)
        {
            saveData.equippedItem = inventorySystem.currentlyEquippedSlot.itemData.itemName;
            saveData.equippedQuantity = inventorySystem.currentlyEquippedSlot.quantity;
        }

        return saveData;
    }

    private void LoadInventoryFromSave(InventorySaveData saveData)
    {
        if (inventorySystem == null) return;

        // Clear existing inventory first
        ClearInventory();

        // Load normal slots
        if (inventorySystem.normalInventorySlots != null)
        {
            int maxSlots = Mathf.Min(saveData.normalSlots.Count, inventorySystem.normalInventorySlots.Count);

            for (int i = 0; i < maxSlots; i++)
            {
                var slotData = saveData.normalSlots[i];
                if (!slotData.isEmpty)
                {
                    ItemData itemData = FindItemData(slotData);
                    if (itemData != null)
                    {
                        // Use your inventory system's method to add item
                        inventorySystem.AddItem(itemData, slotData.quantity,
                            SlotPriority.Normal, null, false);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find item: {slotData.itemName} (ID: {slotData.itemId})");
                    }
                }
            }
        }

        // Load dedicated slots
        if (inventorySystem.dedicatedInventorySlots != null)
        {
            int maxSlots = Mathf.Min(saveData.dedicatedSlots.Count, inventorySystem.dedicatedInventorySlots.Count);

            for (int i = 0; i < maxSlots; i++)
            {
                var slotData = saveData.dedicatedSlots[i];
                if (!slotData.isEmpty)
                {
                    ItemData itemData = FindItemData(slotData);
                    if (itemData != null)
                    {
                        inventorySystem.AddItem(itemData, slotData.quantity,
                            SlotPriority.Dedicated, null, false);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find item: {slotData.itemName} (ID: {slotData.itemId})");
                    }
                }
            }
        }

        // Load equipped item
        if (!string.IsNullOrEmpty(saveData.equippedItem))
        {
            ItemData equippedItem = FindItemByName(saveData.equippedItem);
            if (equippedItem != null && inventorySystem.currentlyEquippedSlot != null)
            {
                // You might need to add equip logic here
                Debug.Log($"Equipped item loaded: {saveData.equippedItem}");
            }
        }
    }

    private void ClearInventory()
    {
        // Clear normal slots
        if (inventorySystem.normalInventorySlots != null)
        {
            foreach (var slot in inventorySystem.normalInventorySlots)
            {
                // Use your inventory system's clear method
                if (slot.itemData != null)
                {
                    // Remove all items from slot
                    while (slot.quantity > 0)
                    {
                        inventorySystem.RemoveItem(slot.itemData, 1);
                    }
                }
            }
        }

        // Clear dedicated slots
        if (inventorySystem.dedicatedInventorySlots != null)
        {
            foreach (var slot in inventorySystem.dedicatedInventorySlots)
            {
                if (slot.itemData != null)
                {
                    while (slot.quantity > 0)
                    {
                        inventorySystem.RemoveItem(slot.itemData, 1);
                    }
                }
            }
        }
    }

    private ItemData FindItemData(InventorySlotSaveData slotData)
    {
        // First try to find by ID (more reliable)
        if (!string.IsNullOrEmpty(slotData.itemId) &&
            ItemDatabaseManager.Instance != null)
        {
            ItemData item = ItemDatabaseManager.Instance.GetItem(slotData.itemId);
            if (item != null)
            {
                return item;
            }
        }

        // Fallback to name
        if (!string.IsNullOrEmpty(slotData.itemName))
        {
            return FindItemByName(slotData.itemName);
        }

        return null;
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

        Debug.LogWarning($"Item not found by name: {itemName}");
        return null;
    }

    private string FindItemIdByName(string itemName)
    {
        if (ItemDatabaseManager.Instance == null) return null;

        var allItems = ItemDatabaseManager.Instance.GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
            {
                // We need to find the ID - check if your ItemData has an ID field
                // If not, we'll need to modify the ItemDatabase to support ID lookup
                return item.itemName; // Using name as ID for now
            }
        }

        return null;
    }
}

// ========== INVENTORY SAVE DATA STRUCTURES ==========

[System.Serializable]
public class InventorySaveData
{
    public List<InventorySlotSaveData> normalSlots = new List<InventorySlotSaveData>();
    public List<InventorySlotSaveData> dedicatedSlots = new List<InventorySlotSaveData>();
    public string equippedItem;
    public int equippedQuantity;
}

[System.Serializable]
public class InventorySlotSaveData
{
    public string itemName;
    public string itemId; // Optional - for better item lookup
    public int quantity;
    public bool isEmpty = true;
}