using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using Doody.InventoryFramework;
using Doody.GameEvents;
using UnityEngine.InputSystem;

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

    [Header("Controller Support")]
    public Image selectionBorder;
    public Color selectedColor = new Color(1f, 1f, 0.5f, 0.3f);
    public Color normalColor = new Color(0f, 0f, 0f, 0f);
    private bool isSelected = false;

    [Header("Focus Settings")]
    public Image focusBorder;
    private Vector3 originalScale = new Vector3(1, 1, 1);
    private Color originalBorderColor;

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
    public bool isEquipped = false;
    public InventorySystem inventorySystem;
    public SlotPriority slotPriority;

    // X-button dragging
    private bool isControllerDragging = false;
    public static InventorySlotsUI controllerDragSource = null;
    public static GameObject controllerDragIcon = null;
    private static InventorySlotsUI currentlyHighlightedForDrop = null;

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

        // Initialize selection border
        if (selectionBorder != null)
        {
            selectionBorder.color = normalColor;
        }

        // Store original values for focus
        if (focusBorder != null)
        {
            originalBorderColor = focusBorder.color;
            focusBorder.gameObject.SetActive(false);
        }

        // Add button component if not present (for UI navigation)
        if (GetComponent<Button>() == null)
        {
            var button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void Update()
    {
        // Handle controller dragging if we're the current drag source
        if (isControllerDragging && controllerDragIcon != null)
        {
            UpdateControllerDragPosition();

            // Allow dropping with Y button while dragging
            if (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame)
            {
                EndControllerDrag();
                DropItemInWorld();
            }
        }

        
    }

    // In InventorySlotsUI.cs, update this method:
    public void SetControllerFocus(bool focused, Color focusColor, float scaleMultiplier = 1f)
    {
        bool usingController = InputDetector.Instance != null && InputDetector.Instance.IsUsingController();

        // IMPORTANT: Allow focus on empty inventory slots when storage is open
        bool storageOpen = StorageUIManager.Instance != null && StorageUIManager.Instance.IsOpen;

        // If not dragging and slot is empty, don't allow focus when using controller
        // UNLESS we're in storage mode (then we want to navigate to empty inventory slots)
        if (controllerDragSource == null && itemData == null && usingController && focused && !storageOpen)
        {
            // Skip empty slots when not in storage
            return;
        }

        if (focusBorder != null && usingController)
        {
            focusBorder.gameObject.SetActive(focused);
            focusBorder.color = focused ? focusColor : originalBorderColor;
        }

        transform.localScale = focused ? originalScale * scaleMultiplier : originalScale;

        // Also update selection border for compatibility
        if (selectionBorder != null)
        {
            selectionBorder.color = focused ? selectedColor : normalColor;
        }

        isSelected = focused;

        // Highlight for drop if we're controller dragging
        if (controllerDragSource != null && controllerDragSource != this && focused)
        {
            // Check if we can drop on this slot
            bool canDropHere = CanDropOnSlot(this);
            if (canDropHere)
            {
                currentlyHighlightedForDrop = this;
                HighlightForDrop(true);
            }
        }
        else if (!focused && currentlyHighlightedForDrop == this)
        {
            HighlightForDrop(false);
            currentlyHighlightedForDrop = null;
        }
    }
    public void SetControllerFocus(bool focused)
    {
        SetControllerFocus(focused, Color.yellow, 1.05f);
    }

    private void HighlightForDrop(bool highlight)
    {
        if (focusBorder != null)
        {
            if (highlight)
            {
                // Green for valid drop, yellow for empty slot (with same priority)
                bool isSamePriority = controllerDragSource != null &&
                    controllerDragSource.slotPriority == this.slotPriority;
                focusBorder.color = itemData != null ?
                    (isSamePriority ? Color.green : Color.red) :
                    (isSamePriority ? Color.yellow : Color.red);
            }
            else
            {
                focusBorder.color = originalBorderColor;
            }
            focusBorder.gameObject.SetActive(highlight || isSelected);
        }
    }

    private bool CanDropOnSlot(InventorySlotsUI targetSlot)
    {
        if (controllerDragSource == null || targetSlot == null) return false;

        // Check priority: dedicated items can only go in dedicated slots
        if (controllerDragSource.slotPriority == SlotPriority.Dedicated &&
            targetSlot.slotPriority != SlotPriority.Dedicated)
        {
            return false;
        }

        // When dragging, can only focus over slots with the same priority
        if (controllerDragSource.slotPriority != targetSlot.slotPriority)
        {
            return false;
        }

        return true;
    }

    public void OnControllerSelect()
    {
        if (itemData == null) return;

        if (isControllerDragging)
        {
            // Cancel drag if A is pressed while already dragging
            EndControllerDrag();
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
    }

    public void OnControllerTransfer()
    {
        // If we're the drag source and user presses X again, try to drop on highlighted slot
        if (isControllerDragging && controllerDragSource == this)
        {
            if (currentlyHighlightedForDrop != null)
            {
                CompleteControllerDrag(currentlyHighlightedForDrop);
            }
            else
            {
                EndControllerDrag();
            }
            return;
        }

        // If another slot is dragging and we press X on this slot, drop here
        if (controllerDragSource != null && controllerDragSource != this)
        {
            CompleteControllerDrag(this);
            return;
        }

        // If not dragging, start drag from this slot
        if (itemData != null && !isControllerDragging)
        {
            StartControllerDrag();
        }
    }

    public void OnControllerDrop()
    {
        if (itemData == null) return;

        // If we're dragging, cancel first
        if (isControllerDragging)
        {
            EndControllerDrag();
        }

        StartCoroutine(DropItemWithConfirmation());
    }

    private void StartControllerDrag()
    {
        if (itemData == null || isControllerDragging) return;

        isControllerDragging = true;
        controllerDragSource = this;

        // Create transparent drag icon at the slot's position
        controllerDragIcon = new GameObject("ControllerDragIcon");
        controllerDragIcon.transform.SetParent(canvas.transform, false);
        controllerDragIcon.transform.SetAsLastSibling();

        var img = controllerDragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.color = new Color(1f, 1f, 1f, 0.5f);
        img.raycastTarget = false;

        var rt = controllerDragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = icon.rectTransform.sizeDelta;

        // Position at the current slot
        rt.position = icon.rectTransform.position;

        // Fade out original icon slightly
        icon.color = isDragging;
    }

    private void UpdateControllerDragPosition()
    {
        if (controllerDragIcon == null) return;

        // Move drag icon
        Vector2 position;

        if (InputDetector.Instance != null && !InputDetector.Instance.IsUsingController() && Mouse.current != null)
        {
            // Use mouse position for keyboard/mouse
            position = Mouse.current.position.ReadValue();
            controllerDragIcon.transform.position = position;
        }
        else if (Gamepad.current != null)
        {
            // Use right stick for controller movement
            RectTransform rt = controllerDragIcon.GetComponent<RectTransform>();
            Vector2 stickInput = Gamepad.current.rightStick.ReadValue();

            if (stickInput.magnitude > 0.1f)
            {
                Vector2 newPos = rt.anchoredPosition + (stickInput * 500f * Time.deltaTime);

                // Clamp to screen bounds
                float halfWidth = rt.rect.width / 2;
                float halfHeight = rt.rect.height / 2;
                newPos.x = Mathf.Clamp(newPos.x, halfWidth, canvas.pixelRect.width - halfWidth);
                newPos.y = Mathf.Clamp(newPos.y, halfHeight, canvas.pixelRect.height - halfHeight);

                rt.anchoredPosition = newPos;
            }
        }
    }

    private void CompleteControllerDrag(InventorySlotsUI targetSlot)
    {
        if (controllerDragSource == null || targetSlot == null) return;

        // Check if we can drop here
        if (!CanDropOnSlot(targetSlot))
        {
            // Cannot drop - dedicated item on normal slot or different priorities
            EndControllerDrag();
            return;
        }

        // If target is empty, just move the item
        if (targetSlot.itemData == null)
        {
            targetSlot.SetItem(controllerDragSource.itemData, controllerDragSource.quantity,
                              controllerDragSource.instantiatedPrefab);
            controllerDragSource.ClearSlot();
        }
        else
        {
            // Use the existing drag handler to combine or swap
            if (_dragManager != null)
            {
                _dragManager.TryHandleDrag(controllerDragSource, targetSlot);
            }
        }

        EndControllerDrag();

        // Refresh the navigation to update slot list
        if (InventoryNavigation.Instance != null)
        {
            InventoryNavigation.Instance.RefreshSlots();
        }
    }

    public void EndControllerDrag()
    {
        if (controllerDragIcon != null)
        {
            Destroy(controllerDragIcon);
            controllerDragIcon = null;
        }

        if (controllerDragSource != null)
        {
            controllerDragSource.icon.color = Original;
            controllerDragSource.isControllerDragging = false;
            controllerDragSource = null;
        }

        // Clear any drop highlighting
        if (currentlyHighlightedForDrop != null)
        {
            currentlyHighlightedForDrop.HighlightForDrop(false);
            currentlyHighlightedForDrop = null;
        }

    }

    private void OnButtonClick()
    {
        // Handle button click (for controller navigation)
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController() && itemData != null)
        {
            OnControllerSelect();
        }
    }

    private IEnumerator DropItemWithConfirmation()
    {
        DropItemInWorld();
        yield return null;
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
            inventorySystem.currentlyEquippedSlot = null;
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
        // Skip mouse clicks when using controller (except for dragging which is handled separately)
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
        {
            return;
        }

        if (itemData == null)
        {
            return;
        }

        // SHIFT+CLICK to transfer to storage (if storage is open) - KEEP for mouse users
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (StorageUIManager.Instance != null && StorageUIManager.Instance.IsOpen)
            {
                TryTransferToStorage();
                return;
            }
        }

        float timeSinceLastClick = Time.unscaledTime - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            if (!useItem)
            {
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

                StorageUIManager.Instance.SaveAndSyncImmediate();
                return;
            }
        }

        foreach (var storageSlot in storageSlots)
        {
            if (storageSlot.ItemData == null)
            {
                storageSlot.SetItem(itemData, quantity, instantiatedPrefab);
                ClearSlot();

                StorageUIManager.Instance.SaveAndSyncImmediate();
                return;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Skip mouse dragging when using controller
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
        {
            return;
        }

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
        // Skip mouse dragging when using controller
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
        {
            return;
        }

        if (dragIcon == null)
        {
            return;
        }

        dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Skip mouse dragging when using controller
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
        {
            return;
        }

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
                    // Check for dedicated item dropping on normal slot
                    if (this.slotPriority == SlotPriority.Dedicated &&
                        targetSlot.slotPriority != SlotPriority.Dedicated)
                    {
                        // Cannot drop dedicated item on normal slot
                        Debug.Log("Cannot drop dedicated item on normal slot");
                        droppedOnInventorySlot = true;
                        break;
                    }

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