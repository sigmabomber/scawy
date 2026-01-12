using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventorySystem : InputScript
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

    // Drop settings
    [Header("Drop Settings")]
    [SerializeField] private Transform dropReferencePoint;
    [SerializeField] private float dropForwardOffset = 1.5f;
    [SerializeField] private float dropUpOffset = 0.5f;

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

        if (dropReferencePoint == null && PlayerController.Instance != null)
        {
            dropReferencePoint = PlayerController.Instance.transform;
        }
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

    protected override void HandleInput()
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

    public bool AddItem(ItemData itemData, int quantity = 1, SlotPriority slotPriority = SlotPriority.Normal,
                     GameObject itemObj = null, bool dropWhenFull = false)
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
            // Only drop if dropWhenFull is true
            if (dropWhenFull && remainingQuantity > 0)
            {
                DropItemWhenFull(itemData, remainingQuantity, itemObj, slotPriority);
                PublishInventoryFull(itemData, quantity);
            }
            else
            {
                PublishInventoryFull(itemData, quantity);
            }

            return false; // Return false to indicate item wasn't added to inventory
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

    private void DropItemWhenFull(ItemData itemData, int quantity, GameObject itemObj, SlotPriority slotPriority)
    {
        if (itemData == null || quantity <= 0)
            return;

        Vector3 dropPosition = CalculateDropPosition();

        GameObject droppedItem = null;

        // If we have a specific item object to drop, use it
        if (itemObj != null)
        {
            droppedItem = itemObj;
            PrepareItemForDrop(droppedItem, dropPosition);
        }
        else
        {
            // Create a new item object if we don't have one
            droppedItem = CreateDroppedItem(itemData, quantity, dropPosition, slotPriority);
        }

        if (droppedItem != null)
        {
            PublishItemDropped(itemData, quantity, droppedItem, dropPosition);
            Debug.Log($"Inventory full! Dropped {quantity} x {itemData.itemName}");
        }
    }

    private Vector3 CalculateDropPosition()
    {
        if (dropReferencePoint != null)
        {
            return dropReferencePoint.position +
                   dropReferencePoint.forward * dropForwardOffset +
                   dropReferencePoint.up * dropUpOffset;
        }

        // Default to player position if no reference point
        return Vector3.zero;
    }

    private void PrepareItemForDrop(GameObject itemObj, Vector3 dropPosition)
    {
        itemObj.transform.position = dropPosition;

        // Add Rigidbody if not present
        Rigidbody rb = itemObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = itemObj.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Enable collider
        Collider col = itemObj.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // Set layer to Interactable
        foreach (Transform t in itemObj.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        // Enable ItemPickupInteractable
        ItemPickupInteractable pickup = itemObj.GetComponent<ItemPickupInteractable>();
        if (pickup != null)
        {
            pickup.enabled = true;
        }

        // Set state to InWorld
        ItemStateTracker stateTracker = itemObj.GetComponent<ItemStateTracker>();
        if (stateTracker != null)
        {
            stateTracker.SetState(ItemState.InWorld);
        }

        // Call OnDroppedInWorld on IItemUsable components
        var usableComponents = itemObj.GetComponents<IItemUsable>();
        foreach (var usable in usableComponents)
        {
            var dropMethod = usable.GetType().GetMethod("OnDroppedInWorld");
            if (dropMethod != null)
            {
                dropMethod.Invoke(usable, null);
            }
        }
    }

    private GameObject CreateDroppedItem(ItemData itemData, int quantity, Vector3 dropPosition, SlotPriority slotPriority)
    {
        if (itemData.prefab == null)
        {
            Debug.LogWarning($"Cannot drop {itemData.itemName}: No prefab assigned!");
            return null;
        }

        GameObject droppedItem = Instantiate(itemData.prefab, dropPosition, itemData.prefab.transform.rotation);

        // Add ItemStateTracker if not present
        if (droppedItem.GetComponent<ItemStateTracker>() == null)
        {
            droppedItem.AddComponent<ItemStateTracker>();
        }

        // Add ItemPickupInteractable if not present
        ItemPickupInteractable pickup = droppedItem.GetComponent<ItemPickupInteractable>();
        if (pickup == null)
        {
            pickup = droppedItem.AddComponent<ItemPickupInteractable>();
        }
        pickup.itemData = itemData;
        pickup.quantity = quantity;
        pickup.slotPriority = slotPriority;
        pickup.isBeingPickedUp = false;

        // Add Rigidbody if not present
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedItem.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;

        // Enable collider
        Collider col = droppedItem.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // Set layer to Interactable
        foreach (Transform t in droppedItem.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        // Set state to InWorld
        var stateTracker = droppedItem.GetComponent<ItemStateTracker>();
        if (stateTracker != null)
        {
            stateTracker.SetState(ItemState.InWorld);
        }

        droppedItem.SetActive(true);

        return droppedItem;
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

        Rigidbody rb = itemObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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

    public bool GiveItem(ItemData itemData, int quantity)
    {
        return AddItem(itemData, quantity, SlotPriority.Normal, null, true);
    }

    public bool GiveItem(ItemData itemData, int quantity, SlotPriority slotPriority)
    {
        return AddItem(itemData, quantity, slotPriority, null, true);
    }

    public bool GiveItem(ItemData itemData, int quantity, SlotPriority slotPriority, GameObject itemObj)
    {
        return AddItem(itemData, quantity, slotPriority, itemObj, true);
    }

    public bool PickupItem(ItemData itemData, int quantity, SlotPriority slotPriority, GameObject itemObj)
    {
        return AddItem(itemData, quantity, slotPriority, itemObj, false);
    }
}