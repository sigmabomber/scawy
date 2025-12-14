using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using Doody.InventoryFramework;
using Doody.GameEvents;
using System.Runtime.CompilerServices;

public class InventorySlotsUI : EventListener,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler,
    IInventorySlotUI
{
    // Data
    public ItemData itemData;
    public int quantity;
    public float usage;
    public Sprite equippedSprite;
    public Sprite normalSprite;
    private Image slotSprite;

    // IInventorySlotUI implementation
    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public GameObject InstantiatedPrefab => instantiatedPrefab;
    public Sprite EquippedSlotSprite => equippedSprite;
    public Sprite NormalSlotSprite => normalSprite;

    // Managers
    private static DragHandlerManager _dragManager;

    [Header("Equipment")]
    private EquipmentManager equipmentManager;

    [Header("Drop Settings")]
    public float dropDistance = 2f;
    public Transform playerTransform;

    // Objects
    public TMP_Text quantityText;
    public Slider usageSlider;
    public GameObject instantiatedPrefab;
    public Image icon;
    public Color isDragging = new Color(1f, 1f, 1f, .5f);
    public Color Original = new Color(1f, 1f, 1f, 1f);

    // Dragging
    private GameObject dragIcon;
    private Transform originalParent;
    private Canvas canvas;

    // Double click detection
    private float lastClickTime;
    private const float doubleClickThreshold = 0.3f;

    public bool useItem = false;
    private bool isEquipped = false;
    public InventorySystem inventorySystem;
    public SlotPriority slotPriority;

    private void Start()
    {
        lastClickTime = 0f;
        slotSprite = GetComponent<Image>();
        inventorySystem = InventorySystem.Instance;
        canvas = GetComponentInParent<Canvas>();

        if (itemData != null)
        {
            SetItemPublic(itemData, quantity);
        }

        if (_dragManager == null)
        {
            _dragManager = new DragHandlerManager();
        }

        equipmentManager = EquipmentManager.Instance;

        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;
    }

    void IInventorySlotUI.SetItem(ItemData itemData, int quantity, GameObject prefab)
    {
        SetItemImplementation(itemData, quantity, prefab);
    }

    public void SetItem(ItemData newItem, int newQuantity, GameObject itemObj = null)
    {
        SetItemImplementation(newItem, newQuantity, itemObj);
    }

    public void SetItemPublic(ItemData newItem, int newQuantity, GameObject itemObj = null)
    {
        SetItemImplementation(newItem, newQuantity, itemObj);
    }

    private void SetItemImplementation(ItemData newItem, int newQuantity, GameObject itemObj = null)
    {
        ItemData oldItem = itemData;
        int oldQuantity = quantity;

        itemData = newItem;
        quantity = newQuantity;

        if (itemObj != null)
            instantiatedPrefab = itemObj;

        usageSlider.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);

        if (newItem != null)
            icon.gameObject.SetActive(true);

        if (itemData == null)
        {
            icon.gameObject.SetActive(false);
            return;
        }

        if (itemData.maxStack > 1 && !(itemData is FlashlightItemData))
        {
            quantityText.gameObject.SetActive(true);
            UpdateQuantity(quantity);
        }

        if (itemData is FlashlightItemData flashlightItemData)
        {
            FlashlightBehavior flashlightBehavior = null;
            if (itemObj != null)
            {
                flashlightBehavior = itemObj.GetComponent<FlashlightBehavior>();
                if (flashlightBehavior != null)
                    flashlightBehavior.Initialize(flashlightItemData);
            }
            usageSlider.gameObject.SetActive(true);
            usageSlider.maxValue = flashlightItemData.maxBattery;
            usageSlider.value = (itemObj != null && flashlightBehavior != null)
                ? flashlightBehavior.GetCurrentBattery()
                : flashlightItemData.maxBattery;
        }

        icon.sprite = itemData.icon;

        if (InventorySystem.Instance != null)
        {
            Events.Publish(new SlotChangedEvent("player_inventory", this, oldItem, oldQuantity));
        }
    }

    public void InitializeForStorageUI()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (inventorySystem == null)
        {
            inventorySystem = InventorySystem.Instance;
        }

        if (playerTransform == null && PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }

        if (equipmentManager == null)
        {
            equipmentManager = EquipmentManager.Instance;
        }

        if (icon != null)
        {
            icon.raycastTarget = true;
        }
    }

    public void UpdateUsage(float newUsage)
    {
        usage = newUsage;
        if (usageSlider.gameObject.activeSelf)
        {
            usageSlider.value = usage;
        }
    }

    public void UpdateQuantity(int newQuantity)
    {
        int oldQuantity = quantity;

        quantity = newQuantity;

        if (quantityText.gameObject.activeSelf)
            quantityText.text = quantity.ToString();

        if (quantity <= 0)
        {
            ClearSlot();
        }
        else
        {
            if (InventorySystem.Instance != null)
            {
                Events.Publish(new SlotChangedEvent("player_inventory", this, itemData, oldQuantity));
            }
        }
    }

    public void ClearSlot()
    {
        ItemData oldItem = itemData;
        int oldQuantity = quantity;

        if (isEquipped)
            OnUnequip();

        itemData = null;
        quantity = 0;
        instantiatedPrefab = null;
        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        usageSlider.gameObject.SetActive(false);
        isEquipped = false;

        if (InventorySystem.Instance != null)
        {
            Events.Publish(new SlotChangedEvent("player_inventory", this, oldItem, oldQuantity));
        }
    }

    public void OnEquip()
    {
        if (itemData == null || itemData.prefab == null)
        {
            return;
        }

        bool hasUsableLogic = false;

        if (instantiatedPrefab != null)
        {
            var existingUsableComponents = instantiatedPrefab.GetComponents<IItemUsable>();
            hasUsableLogic = existingUsableComponents != null && existingUsableComponents.Length > 0;
        }
        else
        {
            hasUsableLogic = itemData.prefab.GetComponent<IItemUsable>() != null;
        }

        if (!hasUsableLogic)
        {
            return;
        }

        if (equipmentManager == null)
        {
            return;
        }

        if (inventorySystem.currentlyEquippedSlot != null &&
            inventorySystem.currentlyEquippedSlot != this)
        {
            inventorySystem.currentlyEquippedSlot.OnUnequip();
        }
        inventorySystem.currentlyEquippedSlot = this;

        if (instantiatedPrefab == null && itemData.prefab != null)
        {
            instantiatedPrefab = Instantiate(itemData.prefab);

            if (instantiatedPrefab.GetComponent<ItemStateTracker>() == null)
            {
                instantiatedPrefab.AddComponent<ItemStateTracker>();
            }

            var stateTracker = instantiatedPrefab.GetComponent<ItemStateTracker>();
            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.Equipped);
            }

            instantiatedPrefab.SetActive(true);
        }
        else if (instantiatedPrefab != null)
        {
            instantiatedPrefab.SetActive(true);

            var stateTracker = instantiatedPrefab.GetComponent<ItemStateTracker>();
            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.Equipped);
            }
        }

        if (instantiatedPrefab.layer != LayerMask.NameToLayer("PickedUpItem"))
        {
            foreach (Transform t in instantiatedPrefab.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = LayerMask.NameToLayer("PickedUpItem");
            }
        }

        IEquippable equippable = instantiatedPrefab.GetComponent<IEquippable>();
        if (equippable != null)
        {
            equipmentManager.EquipItem(equippable);
            isEquipped = true;

            Events.Publish(new ItemEquippedEvent("player_inventory", itemData,
                                                quantity, this, equippable));
        }
        else
        {
            isEquipped = true;

            Events.Publish(new ItemEquippedEvent("player_inventory", itemData,
                                                quantity, this, null));
        }
        slotSprite.sprite = equippedSprite;
        var mainUsableComponents = instantiatedPrefab.GetComponents<IItemUsable>();
        foreach (var usable in mainUsableComponents)
        {
            usable?.OnEquip(this);
        }

        var childUsableComponents = instantiatedPrefab.GetComponentsInChildren<IItemUsable>(true);
        var mainComponentsList = mainUsableComponents.ToList();

        foreach (var usable in childUsableComponents)
        {
            if (!mainComponentsList.Contains(usable))
            {
                usable?.OnEquip(this);
            }
        }
    }

    public void UseItem()
    {
        if (instantiatedPrefab == null) return;

        var mainUsableComponents = instantiatedPrefab.GetComponents<IItemUsable>();
        foreach (var usable in mainUsableComponents)
        {
            usable?.OnUse(this);
        }

        var childUsableComponents = instantiatedPrefab.GetComponentsInChildren<IItemUsable>(true);
        var mainComponentsList = mainUsableComponents.ToList();

        foreach (var usable in childUsableComponents)
        {
            if (!mainComponentsList.Contains(usable))
            {
                usable?.OnUse(this);
            }
        }

        bool wasConsumed = false;
        Events.Publish(new ItemUsedEvent("player_inventory", itemData, quantity,
                                        this, wasConsumed));
    }

    public void OnUnequip()
    {
        if (equipmentManager == null) return;

        if (equipmentManager.IsEquipped())
        {
            equipmentManager.UnequipItem();
            isEquipped = false;
        }

        if (instantiatedPrefab != null)
        {
            var mainUsableComponents = instantiatedPrefab.GetComponents<IItemUsable>();
            foreach (var usable in mainUsableComponents)
            {
                usable?.OnUnequip(this);
            }

            var childUsableComponents = instantiatedPrefab.GetComponentsInChildren<IItemUsable>(true);
            var mainComponentsList = mainUsableComponents.ToList();

            foreach (var usable in childUsableComponents)
            {
                if (!mainComponentsList.Contains(usable))
                {
                    usable?.OnUnequip(this);
                }
            }
        }

        if (instantiatedPrefab != null)
        {
            instantiatedPrefab.SetActive(false);
        }
        slotSprite.sprite = normalSprite;

        Events.Publish(new ItemUnequippedEvent("player_inventory", itemData,
                                              quantity, this));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData == null)
        {
            return;
        }

        // SHIFT+CLICK to transfer to storage (if storage is open)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (StorageUIManager.Instance != null && StorageUIManager.Instance.IsOpen)
            {
                TryTransferToStorage();
                return;
            }
        }

        // Normal double-click equip logic
        float timeSinceLastClick = Time.unscaledTime - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // DON'T EQUIP if useItem is false (in storage UI)
            if (!useItem)
            {
                Debug.Log("[InventorySlot] Cannot equip in storage UI");
                return;
            }

            if (isEquipped)
            {
                OnUnequip();
            }
            else
            {
                OnEquip();
            }

            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
    }

    private void TryTransferToStorage()
    {
        if (itemData == null) return;

        if (StorageUIManager.Instance == null || !StorageUIManager.Instance.IsOpen)
            return;

        var storageSlots = StorageUIManager.Instance.GetStorageSlots();
        if (storageSlots == null || storageSlots.Count == 0) return;

        // Try to stack in existing slots first
        foreach (var storageSlot in storageSlots)
        {
            if (storageSlot.ItemData == itemData &&
                storageSlot.Quantity < itemData.maxStack &&
                itemData.maxStack > 1)
            {
                int spaceLeft = itemData.maxStack - storageSlot.Quantity;
                int amountToAdd = Mathf.Min(spaceLeft, quantity);

                storageSlot.UpdateQuantity(storageSlot.Quantity + amountToAdd);

                quantity -= amountToAdd;
                if (quantity <= 0)
                {
                    ClearSlot();
                }
                else
                {
                    UpdateQuantity(quantity);
                }

                // SAVE IMMEDIATELY
                StorageUIManager.Instance.SaveAndSyncImmediate();
                return;
            }
        }

        // Find empty slot
        foreach (var storageSlot in storageSlots)
        {
            if (storageSlot.ItemData == null)
            {
                storageSlot.SetItem(itemData, quantity, instantiatedPrefab);
                ClearSlot();

                // SAVE IMMEDIATELY
                StorageUIManager.Instance.SaveAndSyncImmediate();
                return;
            }
        }

        Debug.Log("[Storage] Storage is full!");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null)
        {
            return;
        }

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;

        icon.color = isDragging;
        dragIcon.GetComponent<RectTransform>().sizeDelta = icon.rectTransform.sizeDelta;

        originalParent = transform.parent;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null)
        {
            return;
        }

        dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }

        icon.color = Original;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool droppedOnInventorySlot = false;

        foreach (var result in results)
        {
            if (result.gameObject == null) continue;

            var storageSlot = result.gameObject.GetComponentInParent<StorageSlotUI>();
            if (storageSlot != null)
            {
                TransferToStorage(storageSlot);
                droppedOnInventorySlot = true;
                break;
            }

            var targetSlot = result.gameObject.GetComponentInParent<InventorySlotsUI>();
            if (targetSlot != null)
            {
                if (targetSlot != this)
                {
                    if (_dragManager != null)
                    {
                        _dragManager.TryHandleDrag(this, targetSlot);
                    }
                }
                droppedOnInventorySlot = true;
                break;
            }

            if (result.gameObject.GetComponent<InventorySlotsUI>() != null ||
                result.gameObject.GetComponent<InventorySystem>() != null)
            {
                droppedOnInventorySlot = true;
                break;
            }
        }

        if (!droppedOnInventorySlot)
        {
            DropItemInWorld();
        }
    }

    private void TransferToStorage(StorageSlotUI targetSlot)
    {
        if (itemData == null) return;

        if (targetSlot.ItemData == null)
        {
            targetSlot.SetItem(itemData, quantity, instantiatedPrefab);
            ClearSlot();
        }
        else if (targetSlot.ItemData == itemData && itemData.maxStack > 1)
        {
            int spaceLeft = itemData.maxStack - targetSlot.Quantity;
            int amountToAdd = Mathf.Min(spaceLeft, quantity);

            targetSlot.UpdateQuantity(targetSlot.Quantity + amountToAdd);

            quantity -= amountToAdd;
            if (quantity <= 0)
            {
                ClearSlot();
            }
            else
            {
                UpdateQuantity(quantity);
            }
        }
        else
        {
            ItemData tempItem = targetSlot.ItemData;
            int tempQty = targetSlot.Quantity;
            GameObject tempPrefab = targetSlot.ItemPrefab;

            targetSlot.SetItem(itemData, quantity, instantiatedPrefab);
            SetItem(tempItem, tempQty, tempPrefab);
        }
    }

    private void DropItemInWorld()
    {
        if (itemData == null || itemData.prefab == null)
        {
            return;
        }

        if (isEquipped)
        {
            OnUnequip();
        }

        Vector3 dropPosition;
        if (playerTransform != null)
        {
            dropPosition = playerTransform.position;
            dropPosition.y += 0.5f;
        }
        else
        {
            dropPosition = Vector3.zero;
        }

        GameObject droppedItem;
        if (instantiatedPrefab == null)
        {
            droppedItem = Instantiate(itemData.prefab, dropPosition, itemData.prefab.transform.rotation);

            if (droppedItem.GetComponent<ItemStateTracker>() == null)
            {
                droppedItem.AddComponent<ItemStateTracker>();
            }

            ItemPickupInteractable pickup = droppedItem.GetComponent<ItemPickupInteractable>();
            if (pickup == null)
            {
                pickup = droppedItem.AddComponent<ItemPickupInteractable>();
            }
            pickup.itemData = itemData;
            pickup.quantity = quantity;
            pickup.slotPriority = slotPriority;
            pickup.isBeingPickedUp = false;

            var stateTracker = droppedItem.GetComponent<ItemStateTracker>();
            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.InWorld);
            }

            droppedItem.SetActive(true);
        }
        else
        {
            droppedItem = instantiatedPrefab;

            ItemPickupInteractable pickup = droppedItem.GetComponent<ItemPickupInteractable>();
            if (pickup == null)
            {
                pickup = droppedItem.AddComponent<ItemPickupInteractable>();
            }
            pickup.itemData = itemData;
            pickup.quantity = quantity;
            pickup.slotPriority = slotPriority;
            pickup.isBeingPickedUp = false;

            ItemStateTracker stateTracker = droppedItem.GetComponent<ItemStateTracker>();
            if (stateTracker == null)
            {
                stateTracker = droppedItem.AddComponent<ItemStateTracker>();
            }
            stateTracker.SetState(ItemState.InWorld, false);

            var usableComponents = droppedItem.GetComponents<IItemUsable>();
            foreach (var usable in usableComponents)
            {
                var dropMethod = usable.GetType().GetMethod("OnDroppedInWorld");
                if (dropMethod != null)
                {
                    dropMethod.Invoke(usable, null);
                }
            }

            droppedItem.SetActive(true);

            if (droppedItem.transform.parent != null)
            {
                droppedItem.transform.SetParent(null);
            }

            droppedItem.transform.position = dropPosition;
            droppedItem.transform.rotation = itemData.prefab.transform.rotation;
        }

        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedItem.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;

        Collider col = droppedItem.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        foreach (Transform t in droppedItem.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        Events.Publish(new ItemDroppedEvent("player_inventory", itemData, quantity,
                                           droppedItem, dropPosition));

        ClearSlot();
    }
}