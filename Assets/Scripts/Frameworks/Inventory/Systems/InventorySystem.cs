using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Updated InventorySystem - can work standalone or with framework
/// If InventoryUIModule is enabled, it will handle input. Otherwise, this handles it.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public GameObject inventory;
    public InventorySlotsUI currentlyEquippedSlot;
    public static InventorySystem Instance;


    // Prefabs
    public GameObject normalSlotsPrefab;
    public GameObject dedicatedSlotsPrefab;

    // Container for slots
    [Header("Slot Container")]
    [SerializeField] private Transform normalSlotsContainer;
    [SerializeField] private Transform dedicatedSlotsContainer;

    // List to track all inventory slots
    public List<InventorySlotsUI> normalInventorySlots = new();
    public List<InventorySlotsUI> dedicatedInventorySlots = new();

    // Framework integration
    private bool useFrameworkInput = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        InitializeSlots();
        CheckFrameworkIntegration();
    }

    private void InitializeSlots()
    {
        if (normalSlotsContainer != null)
        {
            InventorySlotsUI[] slots = normalSlotsContainer.GetComponentsInChildren<InventorySlotsUI>();
            foreach (var slot in slots)
            {
                if (slot.slotPriority == SlotPriority.Normal)
                {
                    normalInventorySlots.Add(slot);
                }
            }
        }

        if (dedicatedSlotsContainer != null)
        {
            InventorySlotsUI[] slots = dedicatedSlotsContainer.GetComponentsInChildren<InventorySlotsUI>();
            foreach (var slot in slots)
            {
                if (slot.slotPriority == SlotPriority.Dedicated)
                {
                    dedicatedInventorySlots.Add(slot);
                }
            }
        }
    }

    private void CheckFrameworkIntegration()
    {
        if (InventoryFramework.Instance != null)
        {
            try
            {
                var uiModule = InventoryFramework.Instance.GetModule<Doody.InventoryFramework.Modules.InventoryUIModule>();
                useFrameworkInput = uiModule != null && uiModule.IsEnabled;

                if (useFrameworkInput)
                {
                }
                else
                {
                    Debug.Log("[InventorySystem] No framework UI module - using local input handling");
                }
            }
            catch (System.Exception e)
            {
                useFrameworkInput = false;
                Debug.LogWarning($"[InventorySystem] Error checking UI module: {e.Message}");
            }
        }
        else
        {
            useFrameworkInput = false;
        }
    }

    void Update()
    {
        if (!useFrameworkInput)
        {
            HandleInventoryToggle();
        }
    }

    private void HandleInventoryToggle()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            bool isOpen = !inventory.activeSelf;
            inventory.SetActive(isOpen);

            Time.timeScale = isOpen ? 0 : 1;

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            PublishInventoryToggle(isOpen);
        }
    }

    public bool AddItem(ItemData itemData, int quantity = 1, SlotPriority slotPriority = SlotPriority.Normal, GameObject itemObj = null)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Trying to add null item to inventory!");
            return false;
        }

        bool success = false;
        int remainingQuantity = quantity;
        bool wasStacked = false;

        switch (slotPriority)
        {
            case SlotPriority.Normal:
                success = AddToSlots(normalInventorySlots, itemData, ref remainingQuantity, itemObj, out wasStacked);
                break;

            case SlotPriority.Dedicated:
                success = AddToSlots(dedicatedInventorySlots, itemData, ref remainingQuantity, itemObj, out wasStacked);
                break;

            default:
                Debug.LogWarning("Unknown slot priority!");
                success = false;
                break;
        }

        if (success)
        {
            PublishItemAdded(itemData, quantity, itemObj, slotPriority, wasStacked);

            if (itemObj != null)
            {
                PrepareItemForInventory(itemObj);
            }
        }
        else
        {
            PublishInventoryFull(itemData, quantity);
        }

        return success;
    }

    private bool AddToSlots(List<InventorySlotsUI> slots, ItemData itemData, ref int remainingQuantity,
                           GameObject itemObj, out bool wasStacked)
    {
        wasStacked = false;

        if (itemData.maxStack > 1)
        {
            foreach (var slot in slots)
            {
                if (slot.itemData == itemData && slot.quantity < itemData.maxStack)
                {
                    int spaceLeft = itemData.maxStack - slot.quantity;
                    int amountToAdd = Mathf.Min(spaceLeft, remainingQuantity);

                    PublishSlotChanged(slot, slot.itemData, slot.quantity);

                    slot.UpdateQuantity(slot.quantity + amountToAdd);
                    wasStacked = true;

                    remainingQuantity -= amountToAdd;

                    if (remainingQuantity <= 0)
                    {
                        return true;
                    }
                }
            }
        }

        if (remainingQuantity > 0)
        {
            foreach (var slot in slots)
            {
                if (slot.itemData == null)
                {
                    PublishSlotChanged(slot, null, 0);

                    slot.SetItem(itemData, remainingQuantity, itemObj);
                    remainingQuantity = 0;
                    return true;
                }
            }
        }

        return remainingQuantity == 0;
    }

    private void PrepareItemForInventory(GameObject itemObj)
    {
        ItemPickupInteractable pickup = itemObj.GetComponent<ItemPickupInteractable>();
        if (pickup != null)
        {
            pickup.enabled = false;
        }

        Collider col = itemObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ItemStateTracker stateTracker = itemObj.GetComponent<ItemStateTracker>();
        if (stateTracker != null)
        {
            stateTracker.SetState(ItemState.InInventory);
        }
    }

    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        foreach (var slot in normalInventorySlots)
        {
            if (slot.itemData == itemData)
            {
                ItemData oldItem = slot.itemData;
                int oldQuantity = slot.quantity;

                if (slot.quantity > quantity)
                {
                    slot.UpdateQuantity(slot.quantity - quantity);
                }
                else
                {
                    slot.ClearSlot();
                }

                PublishSlotChanged(slot, oldItem, oldQuantity);

                PublishItemRemoved(itemData, quantity);
                return true;
            }
        }

        return false;
    }

    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        int totalCount = 0;

        foreach (var slot in normalInventorySlots)
        {
            if (slot.itemData == itemData)
            {
                totalCount += slot.quantity;

                if (totalCount >= quantity)
                    return true;
            }
        }

        return false;
    }

    public int GetItemCount(ItemData itemData)
    {
        int count = 0;

        foreach (var slot in normalInventorySlots)
        {
            if (slot.itemData == itemData)
            {
                count += slot.quantity;
            }
        }

        return count;
    }

    public List<InventorySlotsUI> GetAllSlots()
    {
        var allSlots = new List<InventorySlotsUI>();
        allSlots.AddRange(normalInventorySlots);
        allSlots.AddRange(dedicatedInventorySlots);
        return allSlots;
    }

    public InventorySlotsUI FindSlotWithItem(ItemData itemData)
    {
        foreach (var slot in normalInventorySlots)
        {
            if (slot.itemData == itemData)
                return slot;
        }

        foreach (var slot in dedicatedInventorySlots)
        {
            if (slot.itemData == itemData)
                return slot;
        }

        return null;
    }

    public InventorySlotsUI FindItem(ItemData itemData)
    {
        return GetAllSlots().FirstOrDefault(slot => slot.itemData == itemData);
    }

    private void PublishItemAdded(ItemData itemData, int quantity, GameObject itemObj,
                                 SlotPriority priority, bool wasStacked)
    {
        Events.Publish(new ItemAddedEvent("player_inventory", itemData, quantity,
                                         itemObj, priority, wasStacked));
    }

    private void PublishItemRemoved(ItemData itemData, int quantity)
    {
        Events.Publish(new ItemRemovedEvent("player_inventory", itemData, quantity));
    }

    private void PublishItemEquipped(InventorySlotsUI slot, IEquippable equippable)
    {
        Events.Publish(new ItemEquippedEvent("player_inventory", slot.itemData,
                                            slot.quantity, slot, equippable));
    }

    private void PublishItemUnequipped(InventorySlotsUI slot)
    {
        Events.Publish(new ItemUnequippedEvent("player_inventory", slot.itemData,
                                              slot.quantity, slot));
    }

    private void PublishItemDropped(ItemData itemData, int quantity,
                                   GameObject droppedObj, Vector3 dropPosition)
    {
        Events.Publish(new ItemDroppedEvent("player_inventory", itemData, quantity,
                                           droppedObj, dropPosition));
    }

    private void PublishInventoryToggle(bool isOpen)
    {
        Events.Publish(new InventoryToggleEvent("player_inventory", isOpen));
    }

    private void PublishSlotChanged(InventorySlotsUI slot, ItemData previousItem,
                                   int previousQuantity)
    {
        Events.Publish(new SlotChangedEvent("player_inventory", slot,
                                           previousItem, previousQuantity));
    }

    private void PublishInventoryFull(ItemData itemData, int attemptedQuantity)
    {
        Events.Publish(new InventoryFullEvent("player_inventory", itemData, attemptedQuantity));
    }


    public void NotifyItemEquipped(InventorySlotsUI slot, IEquippable equippable)
    {
        PublishItemEquipped(slot, equippable);
    }

    public void NotifyItemUnequipped(InventorySlotsUI slot)
    {
        PublishItemUnequipped(slot);
    }

    public void NotifyItemUsed(InventorySlotsUI slot, bool wasConsumed)
    {
        Events.Publish(new ItemUsedEvent("player_inventory", slot.itemData,
                                        slot.quantity, slot, wasConsumed));
    }
}