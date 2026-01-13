using System.Collections.Generic;
using UnityEngine;
using Doody.InventoryFramework;

/// <summary>
/// Adapter that makes InventorySystem compatible with IInventorySystem
/// </summary>
[RequireComponent(typeof(InventorySystem))]
public class InventorySystemAdapter : MonoBehaviour, IInventorySystem
{
    private InventorySystem inventorySystem;

    [SerializeField] private string systemId = "player_inventory";
    public string SystemId => systemId;

    private void Awake()
    {
        inventorySystem = GetComponent<InventorySystem>();
    }

    private void Start()
    {
        if (InventoryFramework.Instance != null)
        {
            InventoryFramework.Instance.RegisterInventorySystem(this);
        }
    }

    public bool AddItem(ItemData itemData, int quantity, SlotPriority priority, GameObject itemObj, bool dropWhenFull = false)
    {
        return inventorySystem.AddItem(itemData, quantity, priority, itemObj, dropWhenFull);
    }

    public bool RemoveItem(ItemData itemData, int quantity)
    {
        return inventorySystem.RemoveItem(itemData, quantity);
    }

    public bool HasItem(ItemData itemData, int quantity)
    {
        return inventorySystem.HasItem(itemData, quantity);
    }

    public int GetItemCount(ItemData itemData)
    {
        return inventorySystem.GetItemCount(itemData);
    }

    public IInventorySlotUI GetSlot(int index)
    {
        if (index < 0 || index >= inventorySystem.normalInventorySlots.Count)
            return null;

        return inventorySystem.normalInventorySlots[index] as IInventorySlotUI;
    }

    public List<IInventorySlotUI> GetAllSlots()
    {
        var allSlots = new List<IInventorySlotUI>();
        foreach (var slot in inventorySystem.normalInventorySlots)
        {
            allSlots.Add(slot as IInventorySlotUI);
        }
        return allSlots;
    }

    public IInventorySlotUI GetEquippedSlot()
    {
        return inventorySystem.currentlyEquippedSlot as IInventorySlotUI;
    }

    public void SetEquippedSlot(IInventorySlotUI slot)
    {
        inventorySystem.currentlyEquippedSlot = slot as InventorySlotsUI;
    }

    // Helper method to get the underlying InventorySystem
    public InventorySystem GetInventorySystem()
    {
        return inventorySystem;
    }
}