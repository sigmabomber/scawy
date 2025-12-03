using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public GameObject inventory;
    public InventorySlotsUI currentlyEquippedSlot;
    public static InventorySystem Instance;
    // Variables
    public int maxSlots = 3;

    // Prefabs
    public GameObject normalSlotsPrefab;
    public GameObject dedicatedSlotsPrefab;

    // Container for slots
    [Header("Slot Container")]
    [SerializeField] private Transform normalSlotsContainer;
    [SerializeField] private Transform dedicatedSlotsContainer;

    // List to track all inventory slots
    public List<InventorySlotsUI> normalInventorySlots = new();
    private List<InventorySlotsUI> dedicatedInventorySlots = new();
    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
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


    void Update()
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

        switch (slotPriority)
        {
            case SlotPriority.Normal:
                if (itemData.maxStack > 1)
                {
                    foreach (var slot in normalInventorySlots)
                    {
                        if (slot.itemData == itemData && slot.quantity < itemData.maxStack)
                        {
                            int spaceLeft = itemData.maxStack - slot.quantity;
                            int amountToAdd = Mathf.Min(spaceLeft, remainingQuantity);
                            slot.UpdateQuantity(slot.quantity + amountToAdd);

                            remainingQuantity -= amountToAdd;

                            if (remainingQuantity <= 0)
                            {
                                success = true;
                                break;
                            }
                        }
                    }
                }

                // If we still have quantity to add, look for empty slots
                if (remainingQuantity > 0)
                {
                    foreach (var slot in normalInventorySlots)
                    {
                        if (slot.itemData == null)
                        {
                            slot.SetItem(itemData, remainingQuantity, itemObj);
                            remainingQuantity = 0;
                            success = true;
                            break;
                        }
                    }
                }
                else if (remainingQuantity == 0)
                {
                    success = true;
                }
                break;

            case SlotPriority.Dedicated:
                if (itemData.maxStack > 1)
                {
                    foreach (var slot in dedicatedInventorySlots)
                    {
                        if (slot.itemData == itemData && slot.quantity < itemData.maxStack)
                        {
                            int spaceLeft = itemData.maxStack - slot.quantity;
                            int amountToAdd = Mathf.Min(spaceLeft, remainingQuantity);
                            slot.UpdateQuantity(slot.quantity + amountToAdd);

                            remainingQuantity -= amountToAdd;

                            if (remainingQuantity <= 0)
                            {
                                success = true;
                                break;
                            }
                        }
                    }
                }

                // If we still have quantity to add, look for empty slots
                if (remainingQuantity > 0)
                {
                    foreach (var slot in dedicatedInventorySlots)
                    {
                        if (slot.itemData == null)
                        {
                            slot.SetItem(itemData, remainingQuantity, itemObj);
                            remainingQuantity = 0;
                            success = true;
                            break;
                        }
                    }
                }
                else if (remainingQuantity == 0)
                {
                    success = true;
                }
                break;

            default:
                Debug.LogWarning("Unknown slot priority!");
                success = false;
                break;
        }

        // DO NOT disable the item here - let ItemPickupInteractable handle it after the coroutine
        // Only prepare the item for inventory if successful
        if (success && itemObj != null)
        {
            ItemPickupInteractable pickup = itemObj.GetComponent<ItemPickupInteractable>();
            if (pickup != null)
            {
                pickup.enabled = false; // Disable the pickup script
            }

            Collider col = itemObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Update item state to inventory
            ItemStateTracker stateTracker = itemObj.GetComponent<ItemStateTracker>();
            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.InInventory);
            }

        }

        return success;
    }
    // Remove item from inventory
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        foreach (var slot in normalInventorySlots)
        {
            if (slot.itemData == itemData)
            {
                if (slot.quantity > quantity)
                {
                    slot.UpdateQuantity(slot.quantity - quantity);
                }
                else
                {
                    slot.ClearSlot();
                }
                return true;
            }
        }

        return false;
    }

    // Check if inventory has an item
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

    // Get total quantity of an item
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
}

