using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public GameObject inventory;

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
    private List<InventorySlotsUI> normalInventorySlots = new();
    private List<InventorySlotsUI> dedicatedInventorySlots = new();

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

    public bool AddItem(ItemData itemData, int quantity = 1, SlotPriority slotPriority = SlotPriority.Normal)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Trying to add null item to inventory!");
            return false;
        }

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
                            int amountToAdd = Mathf.Min(spaceLeft, quantity);
                            slot.UpdateQuantity(slot.quantity + amountToAdd);

                            quantity -= amountToAdd;

                            if (quantity <= 0)
                                return true;
                        }
                    }
                }

                foreach (var slot in normalInventorySlots)
                {
                    if (slot.itemData == null)
                    {
                        slot.SetItem(itemData, quantity);
                        return true;
                    }
                }

                return false;

            case SlotPriority.Dedicated:
                if (itemData.maxStack > 1)
                {
                    foreach (var slot in dedicatedInventorySlots)
                    {
                        if (slot.itemData == itemData && slot.quantity < itemData.maxStack)
                        {
                            int spaceLeft = itemData.maxStack - slot.quantity;
                            int amountToAdd = Mathf.Min(spaceLeft, quantity);
                            slot.UpdateQuantity(slot.quantity + amountToAdd);

                            quantity -= amountToAdd;

                            if (quantity <= 0)
                                return true;
                        }
                    }
                }

                foreach (var slot in dedicatedInventorySlots)
                {
                    if (slot.itemData == null)
                    {
                        slot.SetItem(itemData, quantity);
                        return true;
                    }
                }

                return false;

            default:
                Debug.LogWarning("Unknown slot priority!");
                return false;
        }
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

