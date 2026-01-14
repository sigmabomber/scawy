using Doody.Framework.DialogueSystem;
using Doody.GameEvents;
using Doody.InventoryFramework;
using UnityEngine;

public class DialogueInventoryIntegration : MonoBehaviour
{
    public static DialogueInventoryIntegration Instance;

    [Header("Settings")]
    [Tooltip("Automatically create Has[ItemName] flags for items in inventory")]
    public bool autoSyncInventory = true;

    [Header("Debug")]
    public bool logFlagChanges = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (autoSyncInventory)
        {
            SetupInventorySync();
        }
    }

    private void SetupInventorySync()
    {
        bool foundSystem = false;

        if (InventorySystem.Instance != null)
        {
            foundSystem = true;
        }

        try
        {
            Events.Subscribe<ItemAddedEvent>(OnItemAdded, this);
            Events.Subscribe<ItemRemovedEvent>(OnItemRemoved, this);
            Events.Subscribe<ItemDroppedEvent>(OnItemDropped, this);
            Events.Subscribe<SlotChangedEvent>(OnSlotChanged, this);
            foundSystem = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DialogueInventory] Failed to subscribe to InventoryFramework events: {e.Message}");
        }

        if (!foundSystem)
        {
            Debug.LogWarning("[DialogueInventory] No inventory system found! Sync disabled.");
            return;
        }

        SyncAllInventoryItems();
    }

    private void OnSlotChanged(SlotChangedEvent evt)
    {
        // Only process player inventory changes
        if (evt.SystemId != "player_inventory")
            return;


        if (logFlagChanges)
            Debug.Log($"[DialogueInventory] SlotChangedEvent received - checking if it's a removal...");

        // If previous item exists but current is different, something was removed
        if (evt.PreviousItem != null && evt.Slot.ItemData != evt.PreviousItem)
        {
            if (logFlagChanges)
                Debug.Log($"[DialogueInventory] Item changed/removed, updating: {evt.PreviousItem.itemName}");
            UpdateItemFlag(evt.PreviousItem);
        }
    }

    private void OnItemAdded(ItemAddedEvent evt)
    {
        if (evt.ItemData != null)
        {
            string flagName = $"Has{evt.ItemData.itemName}";

            if (!DialogueManager.Instance.HasFlag(flagName))
            {
                DialogueManager.Instance.SetFlag(flagName);
                if (logFlagChanges)
                    Debug.Log($"[DialogueInventory] ✓ Flag SET: {flagName} (from ItemAdded)");
            }
        }
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        if (evt.ItemData != null)
        {
            // Check if any remain after removal
            UpdateItemFlag(evt.ItemData);
        }
    }

    private void OnItemDropped(ItemDroppedEvent evt)
    {
        if (evt.ItemData != null)
        {
            // Check if any remain after drop
            UpdateItemFlag(evt.ItemData);
        }
    }

    private void UpdateItemFlag(ItemData itemData)
    {
        if (itemData == null || InventorySystem.Instance == null) return;

        string flagName = $"Has{itemData.itemName}";

        // Always check actual inventory count
        int count = InventorySystem.Instance.GetItemCount(itemData);
        bool shouldHaveFlag = count > 0;
        bool currentlyHasFlag = DialogueManager.Instance.HasFlag(flagName);

        if (logFlagChanges)
            Debug.Log($"[DialogueInventory] UpdateItemFlag: {flagName} | Count: {count} | CurrentFlag: {currentlyHasFlag} | ShouldHave: {shouldHaveFlag}");

        if (shouldHaveFlag && !currentlyHasFlag)
        {
            DialogueManager.Instance.SetFlag(flagName);
            if (logFlagChanges)
                Debug.Log($"[DialogueInventory] ✓ Flag SET: {flagName} (count: {count})");
        }
        else if (!shouldHaveFlag && currentlyHasFlag)
        {
            DialogueManager.Instance.ClearFlag(flagName);
            if (logFlagChanges)
                Debug.Log($"[DialogueInventory] ✗ Flag CLEARED: {flagName} (count: 0)");
        }
    }

    private void SyncAllInventoryItems()
    {
        int syncedCount = 0;

        if (InventorySystem.Instance != null)
        {
            var allSlots = InventorySystem.Instance.GetAllSlots();
            foreach (var slot in allSlots)
            {
                if (slot.ItemData != null)
                {
                    string flagName = $"Has{slot.ItemData.itemName}";
                    DialogueManager.Instance.SetFlag(flagName);
                    syncedCount++;
                }
            }
        }

        if (logFlagChanges)
            Debug.Log($"[DialogueInventory] Total synced items: {syncedCount}");
    }

    /// <summary>
    /// Manually check if player has an item by name
    /// </summary>
    public bool HasItem(string itemName)
    {
        return DialogueManager.Instance.HasFlag($"Has{itemName}");
    }

    /// <summary>
    /// Manually resync all inventory items with flags
    /// </summary>
    public void ResyncInventory()
    {
        SyncAllInventoryItems();
    }

    /// <summary>
    /// Manually set an item flag (for debugging or special cases)
    /// </summary>
    public void SetItemFlag(string itemName, bool hasItem = true)
    {
        string flagName = $"Has{itemName}";
        if (hasItem)
        {
            DialogueManager.Instance.SetFlag(flagName);
            Debug.Log($"[DialogueInventory] Manually set flag: {flagName}");
        }
        else
        {
            DialogueManager.Instance.ClearFlag(flagName);
            Debug.Log($"[DialogueInventory] Manually cleared flag: {flagName}");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Events.UnsubscribeAll(this);
    }
}