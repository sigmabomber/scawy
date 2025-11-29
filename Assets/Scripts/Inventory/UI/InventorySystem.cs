using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Transform slotsContainer;

    // List to track all inventory slots
    private List<InventorySlotsUI> inventorySlots = new List<InventorySlotsUI>();

    void Start()
    {
        // Find all existing inventory slots
        if (slotsContainer != null)
        {
            inventorySlots.AddRange(slotsContainer.GetComponentsInChildren<InventorySlotsUI>());
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

    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Trying to add null item to inventory!");
            return false;
        }

        // Check if item can stack and if we already have it
        if (itemData.maxStack > 1)
        {
            // Try to stack with existing items
            foreach (var slot in inventorySlots)
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

        // Find empty slot for remaining items
        foreach (var slot in inventorySlots)
        {
            if (slot.itemData == null)
            {
                slot.SetItem(itemData, quantity);
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    // Remove item from inventory
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        foreach (var slot in inventorySlots)
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

        foreach (var slot in inventorySlots)
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

        foreach (var slot in inventorySlots)
        {
            if (slot.itemData == itemData)
            {
                count += slot.quantity;
            }
        }

        return count;
    }
}

