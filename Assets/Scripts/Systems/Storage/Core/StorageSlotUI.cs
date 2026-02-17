using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Doody.InventoryFramework;

public class StorageSlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    public Image icon;
    public TMP_Text quantityText;
    public Slider usageSlider;

    public Image focusBorder;
    public Color selectedColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    public Color normalColor = new Color(0f, 0f, 0f, 0f);
    private Color originalBorderColor;
    private Vector3 originalScale;

    public SlotType slotType = SlotType.Storage;
    public SlotPriority slotPriority = SlotPriority.Normal;

    private StorageContainer parentStorage;
    private int slotIndex;

    private InventorySlotsUI linkedInventorySlot;
    private InventorySystem inventorySystem;

    public ItemData itemData;
    private int quantity;
    private GameObject itemPrefab;

    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public GameObject ItemPrefab => itemPrefab;

    private GameObject dragIcon;
    private Canvas canvas;
    private Color isDragging = new Color(1f, 1f, 1f, 0.5f);
    private Color original = new Color(1f, 1f, 1f, 1f);

    private static DragHandlerManager _dragManager;

    private float lastClickTime;
    private const float doubleClickThreshold = 0.3f;

    private bool isControllerDragging = false;
    public static StorageSlotUI controllerDragSource = null;
    public static GameObject controllerDragIcon = null;
    public static StorageSlotUI currentlyHighlightedForDrop = null;

    public enum SlotType { Storage, Inventory }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (_dragManager == null)
            _dragManager = new DragHandlerManager();

        if (icon != null)
            icon.raycastTarget = true;

        var image = GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        if (focusBorder != null)
        {
            originalBorderColor = focusBorder.color;
            focusBorder.gameObject.SetActive(false);
            originalScale = transform.localScale;
        }

        if (GetComponent<Button>() == null)
        {
            var button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnButtonClick);
        }

        if (inventorySystem == null)
            inventorySystem = InventorySystem.Instance;

        if (inventorySystem == null)
            inventorySystem = FindObjectOfType<InventorySystem>();
    }

    private void Update()
    {
        if (isControllerDragging && controllerDragIcon != null)
            UpdateControllerDragPosition();
    }

    public void HighlightForDrop(bool highlight, bool isValid = true)
    {
        if (focusBorder == null) return;

        if (highlight)
        {
            if (isValid)
                focusBorder.color = itemData != null ? Color.green : Color.yellow;
            else
                focusBorder.color = Color.red;
        }
        else
        {
            focusBorder.color = originalBorderColor;
        }

        focusBorder.gameObject.SetActive(highlight);
    }

    public bool CanDropOnSlot(StorageSlotUI targetSlot)
    {
        if (controllerDragSource == null || targetSlot == null) return true;

        if (targetSlot.slotType == SlotType.Inventory && controllerDragSource.itemData != null)
            return controllerDragSource.itemData.priority == targetSlot.slotPriority;

        return true;
    }

    public void OnControllerSelect()
    {
        if (itemData == null) return;

        if (isControllerDragging)
        {
            EndControllerDrag();
            return;
        }

        if (slotType == SlotType.Storage)
            TransferToPlayerInventory();
        else if (slotType == SlotType.Inventory)
            TransferToStorageViaYButton();
    }

    private void TransferToStorageViaYButton()
    {
        if (itemData == null || slotType != SlotType.Inventory) return;

        if (StorageUIManager.Instance == null) return;

        var storageSlots = StorageUIManager.Instance.GetStorageSlots();

        foreach (var storageSlot in storageSlots)
        {
            if (storageSlot.itemData == null)
            {
                ItemData tempItem = itemData;
                int tempQty = quantity;
                GameObject tempPrefab = itemPrefab;

                ClearSlot();
                storageSlot.SetItem(tempItem, tempQty, tempPrefab);
                SyncToInventory();
                StorageUIManager.Instance.SaveAndSyncImmediate();
                return;
            }
        }

        foreach (var storageSlot in storageSlots)
        {
            if (storageSlot.itemData == itemData && itemData.maxStack > 1)
            {
                int spaceLeft = itemData.maxStack - storageSlot.quantity;
                if (spaceLeft > 0)
                {
                    int amountToAdd = Mathf.Min(spaceLeft, quantity);
                    storageSlot.UpdateQuantity(storageSlot.quantity + amountToAdd);

                    quantity -= amountToAdd;
                    if (quantity <= 0)
                        ClearSlot();
                    else
                        SetItem(itemData, quantity, itemPrefab);

                    SyncToInventory();
                    StorageUIManager.Instance.SaveAndSyncImmediate();
                    return;
                }
            }
        }
    }

    public void OnControllerTransfer()
    {
        if (isControllerDragging && controllerDragSource == this)
        {
            if (StorageNavigation.Instance != null)
            {
                var focusedSlot = StorageNavigation.Instance.GetFocusedSlot();
                if (focusedSlot != null && focusedSlot != this)
                    CompleteControllerDrag(focusedSlot);
                else
                    EndControllerDrag();
            }
            else
            {
                EndControllerDrag();
            }
            return;
        }

        if (controllerDragSource != null && controllerDragSource != this)
        {
            CompleteControllerDrag(this);
            return;
        }

        if (itemData != null && !isControllerDragging)
            StartControllerDrag();
    }

    public void OnControllerDrop()
    {
        if (itemData == null) return;

        if (isControllerDragging)
            EndControllerDrag();

        DropItemInWorld();
    }

    private void StartControllerDrag()
    {
        if (itemData == null || isControllerDragging) return;

        isControllerDragging = true;
        controllerDragSource = this;

        controllerDragIcon = new GameObject("ControllerDragIcon");
        controllerDragIcon.transform.SetParent(canvas.transform, false);
        controllerDragIcon.transform.SetAsLastSibling();

        var img = controllerDragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.color = new Color(1f, 1f, 1f, 0.5f);
        img.raycastTarget = false;

        var rt = controllerDragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = icon.rectTransform.sizeDelta;
        rt.position = icon.rectTransform.position;

        icon.color = isDragging;
    }

    private void UpdateControllerDragPosition()
    {
        if (controllerDragIcon == null) return;

        if (InputDetector.Instance != null && !InputDetector.Instance.IsUsingController() && Mouse.current != null)
        {
            controllerDragIcon.transform.position = Mouse.current.position.ReadValue();
        }
        else if (Gamepad.current != null)
        {
            RectTransform rt = controllerDragIcon.GetComponent<RectTransform>();
            Vector2 stickInput = Gamepad.current.rightStick.ReadValue();

            if (stickInput.magnitude > 0.1f)
            {
                Vector2 newPos = rt.anchoredPosition + (stickInput * 500f * Time.deltaTime);
                float halfWidth = rt.rect.width / 2;
                float halfHeight = rt.rect.height / 2;

                newPos.x = Mathf.Clamp(newPos.x, halfWidth, canvas.pixelRect.width - halfWidth);
                newPos.y = Mathf.Clamp(newPos.y, halfHeight, canvas.pixelRect.height - halfHeight);

                rt.anchoredPosition = newPos;
            }
        }
    }

    private void CompleteControllerDrag(StorageSlotUI targetSlot)
    {
        if (controllerDragSource == null || targetSlot == null)
        {
            EndControllerDrag();
            return;
        }

        if (controllerDragSource.slotType == SlotType.Storage && targetSlot.slotType == SlotType.Storage)
            controllerDragSource.SwapWithStorageSlot(targetSlot);
        else if (controllerDragSource.slotType == SlotType.Inventory && targetSlot.slotType == SlotType.Inventory)
        {
            if (controllerDragSource.itemData != null &&
                controllerDragSource.itemData.priority != targetSlot.slotPriority)
            {
                EndControllerDrag();
                return;
            }
            controllerDragSource.SwapWithInventorySlot(targetSlot);
        }
        else if (controllerDragSource.slotType == SlotType.Storage && targetSlot.slotType == SlotType.Inventory)
        {
            if (controllerDragSource.itemData != null &&
                controllerDragSource.itemData.priority != targetSlot.slotPriority)
            {
                EndControllerDrag();
                return;
            }
            controllerDragSource.TransferToInventorySlot(targetSlot);
        }
        else if (controllerDragSource.slotType == SlotType.Inventory && targetSlot.slotType == SlotType.Storage)
        {
            controllerDragSource.TransferToStorageSlot(targetSlot);
        }

        EndControllerDrag();

        if (StorageNavigation.Instance != null)
            StorageNavigation.Instance.RefreshSlots();
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
            controllerDragSource.icon.color = original;
            controllerDragSource.isControllerDragging = false;
            controllerDragSource = null;
        }

        if (currentlyHighlightedForDrop != null)
        {
            currentlyHighlightedForDrop.HighlightForDrop(false);
            currentlyHighlightedForDrop = null;
        }
    }

    public void SetControllerFocus(bool focused)
    {
        Color focusColor = slotType == SlotType.Storage ? Color.cyan : Color.yellow;
        SetControllerFocus(focused, focusColor, 1.05f);
    }

    public void SetControllerFocus(bool focused, Color focusColor, float scaleMultiplier = 1f)
    {
        bool usingController = InputDetector.Instance != null && InputDetector.Instance.IsUsingController();

        if (focusBorder != null && usingController)
        {
            focusBorder.gameObject.SetActive(focused);
            focusBorder.color = focused ? focusColor : originalBorderColor;
        }

        transform.localScale = focused ? originalScale * scaleMultiplier : originalScale;

        var selectionBorder = GetComponentInChildren<Image>(true);
        if (selectionBorder != null && selectionBorder != focusBorder)
            selectionBorder.color = focused ? selectedColor : normalColor;

        if (controllerDragSource != null && controllerDragSource != this && focused)
        {
            bool canDropHere = CanDropOnSlot(this);
            currentlyHighlightedForDrop = this;
            HighlightForDrop(true, canDropHere);
        }
        else if (!focused && currentlyHighlightedForDrop == this)
        {
            HighlightForDrop(false);
            currentlyHighlightedForDrop = null;
        }
    }

    private void OnButtonClick()
    {
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController() && itemData != null)
            OnControllerSelect();
    }

    public void SetStorageReference(StorageContainer storage, int index)
    {
        slotType = SlotType.Storage;
        parentStorage = storage;
        slotIndex = index;
    }

    public void SetInventoryReference(InventorySlotsUI linkedSlot, SlotPriority priority)
    {
        slotType = SlotType.Inventory;
        linkedInventorySlot = linkedSlot;
        slotPriority = priority;

        if (linkedSlot != null)
            SetItem(linkedSlot.itemData, linkedSlot.quantity, linkedSlot.instantiatedPrefab);
    }

    public void SyncFromInventory()
    {
        if (slotType != SlotType.Inventory || linkedInventorySlot == null) return;

        SetItem(linkedInventorySlot.itemData, linkedInventorySlot.quantity,
                linkedInventorySlot.instantiatedPrefab);
    }

    public void SyncToInventory()
    {
        if (slotType != SlotType.Inventory || linkedInventorySlot == null || inventorySystem == null)
            return;

        if (itemData != null)
            linkedInventorySlot.SetItem(itemData, quantity, itemPrefab);
        else
            linkedInventorySlot.ClearSlot();
    }

    public void SetItem(ItemData newItem, int newQuantity, GameObject prefab = null)
    {
        itemData = newItem;
        quantity = newQuantity;
        itemPrefab = prefab;

        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        if (usageSlider != null)
            usageSlider.gameObject.SetActive(false);

        if (itemData == null)
            return;

        icon.sprite = itemData.icon;
        icon.gameObject.SetActive(true);
        icon.raycastTarget = true;

        if (itemData.maxStack > 1 && !(itemData is FlashlightItemData))
        {
            quantityText.text = quantity.ToString();
            quantityText.gameObject.SetActive(true);
        }

        if (itemData is FlashlightItemData flashlightItemData && usageSlider != null)
        {
            FlashlightBehavior flashlightBehavior = null;
            if (prefab != null)
            {
                flashlightBehavior = prefab.GetComponent<FlashlightBehavior>();
                if (flashlightBehavior != null)
                    flashlightBehavior.Initialize(flashlightItemData);
            }

            usageSlider.gameObject.SetActive(true);
            usageSlider.maxValue = flashlightItemData.maxBattery;
            usageSlider.value = (prefab != null && flashlightBehavior != null)
                ? flashlightBehavior.GetCurrentBattery()
                : flashlightItemData.maxBattery;
        }
    }

    public void UpdateQuantity(int newQuantity)
    {
        quantity = newQuantity;

        if (quantityText.gameObject.activeSelf)
            quantityText.text = quantity.ToString();

        if (quantity <= 0)
            ClearSlot();
    }

    public void ClearSlot()
    {
        itemData = null;
        quantity = 0;
        itemPrefab = null;
        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        if (usageSlider != null)
            usageSlider.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
            return;

        if (itemData == null) return;

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;

        icon.color = isDragging;
        dragIcon.GetComponent<RectTransform>().sizeDelta = icon.rectTransform.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
            return;

        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
            return;

        if (dragIcon != null)
            Destroy(dragIcon);

        icon.color = original;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var targetSlot = result.gameObject.GetComponentInParent<StorageSlotUI>();
            if (targetSlot != null && targetSlot != this)
            {
                HandleDragToSlot(targetSlot);
                return;
            }
        }
    }

    private void HandleDragToSlot(StorageSlotUI targetSlot)
    {
        if (itemData == null) return;

        if (slotType == SlotType.Inventory && targetSlot.slotType == SlotType.Inventory)
        {
            if (itemData.priority != targetSlot.slotPriority)
            {
                StartCoroutine(FlashSlotRed(targetSlot));
                return;
            }
        }

        if (targetSlot.itemData == null)
        {
            targetSlot.SetItem(itemData, quantity, itemPrefab);
            ClearSlot();
        }
        else if (targetSlot.itemData == itemData && itemData.maxStack > 1)
        {
            int spaceLeft = itemData.maxStack - targetSlot.quantity;
            int amountToAdd = Mathf.Min(spaceLeft, quantity);

            targetSlot.UpdateQuantity(targetSlot.quantity + amountToAdd);

            quantity -= amountToAdd;
            if (quantity <= 0)
                ClearSlot();
            else
                SetItem(itemData, quantity, itemPrefab);
        }
        else
        {
            ItemData tempItem = targetSlot.itemData;
            int tempQty = targetSlot.quantity;
            GameObject tempPrefab = targetSlot.itemPrefab;

            targetSlot.SetItem(itemData, quantity, itemPrefab);
            SetItem(tempItem, tempQty, tempPrefab);
        }

        if (slotType == SlotType.Storage || targetSlot.slotType == SlotType.Storage)
            StorageUIManager.Instance?.SaveAndSyncImmediate();

        if (slotType == SlotType.Inventory)
            SyncToInventory();
        if (targetSlot.slotType == SlotType.Inventory)
            targetSlot.SyncToInventory();
    }

    private void TransferToPlayerInventory()
    {
        if (itemData == null || slotType != SlotType.Storage) return;

        var inventorySlots = StorageUIManager.Instance?.GetAllPlayerStorageSlots();
        if (inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot.slotType == SlotType.Inventory &&
                slot.slotPriority == itemData.priority &&
                slot.itemData == null)
            {
                slot.SetItem(itemData, quantity, itemPrefab);
                slot.SyncToInventory();
                ClearSlot();
                StorageUIManager.Instance?.SaveAndSyncImmediate();
                return;
            }
        }
    }

    private void SwapWithStorageSlot(StorageSlotUI otherSlot)
    {
        ItemData tempItem = otherSlot.itemData;
        int tempQty = otherSlot.quantity;
        GameObject tempPrefab = otherSlot.itemPrefab;

        otherSlot.SetItem(itemData, quantity, itemPrefab);
        SetItem(tempItem, tempQty, tempPrefab);

        StorageUIManager.Instance?.SaveAndSyncImmediate();
    }

    private void SwapWithInventorySlot(StorageSlotUI otherSlot)
    {
        if (otherSlot.linkedInventorySlot == null) return;

        ItemData tempItem = otherSlot.itemData;
        int tempQty = otherSlot.quantity;
        GameObject tempPrefab = otherSlot.itemPrefab;

        otherSlot.SetItem(itemData, quantity, itemPrefab);
        SetItem(tempItem, tempQty, tempPrefab);

        SyncToInventory();
        otherSlot.SyncToInventory();
    }

    private void TransferToInventorySlot(StorageSlotUI targetSlot)
    {
        if (targetSlot.linkedInventorySlot == null)
            return;

        if (targetSlot.itemData == null)
        {
            targetSlot.SetItem(itemData, quantity, itemPrefab);
            ClearSlot();
        }
        else if (targetSlot.itemData == itemData && itemData.maxStack > 1)
        {
            int spaceLeft = itemData.maxStack - targetSlot.quantity;
            int amountToAdd = Mathf.Min(spaceLeft, quantity);

            targetSlot.UpdateQuantity(targetSlot.quantity + amountToAdd);

            quantity -= amountToAdd;
            if (quantity <= 0)
                ClearSlot();
            else
                SetItem(itemData, quantity, itemPrefab);
        }
        else
        {
            ItemData tempItem = targetSlot.itemData;
            int tempQty = targetSlot.quantity;
            GameObject tempPrefab = targetSlot.itemPrefab;

            targetSlot.SetItem(itemData, quantity, itemPrefab);
            SetItem(tempItem, tempQty, tempPrefab);
        }

        targetSlot.SyncToInventory();
        StorageUIManager.Instance?.SaveAndSyncImmediate();
    }

    private void TransferToStorageSlot(StorageSlotUI targetSlot)
    {
        ItemData itemToMove = itemData;
        int qtyToMove = quantity;
        GameObject prefabToMove = itemPrefab;

        if (targetSlot.itemData == null)
        {
            ClearSlot();
            targetSlot.SetItem(itemToMove, qtyToMove, prefabToMove);
        }
        else if (targetSlot.itemData == itemData && itemData.maxStack > 1)
        {
            int spaceLeft = itemData.maxStack - targetSlot.quantity;
            int amountToAdd = Mathf.Min(spaceLeft, quantity);

            targetSlot.UpdateQuantity(targetSlot.quantity + amountToAdd);

            quantity -= amountToAdd;
            if (quantity <= 0)
                ClearSlot();
            else
                SetItem(itemData, quantity, itemPrefab);
        }
        else
        {
            ItemData tempItem = targetSlot.itemData;
            int tempQty = targetSlot.quantity;
            GameObject tempPrefab = targetSlot.itemPrefab;

            ClearSlot();
            targetSlot.SetItem(itemToMove, qtyToMove, prefabToMove);
            SetItem(tempItem, tempQty, tempPrefab);
        }

        if (slotType == SlotType.Inventory)
            SyncToInventory();

        StorageUIManager.Instance?.SaveAndSyncImmediate();
    }

    private void DropItemInWorld()
    {
        if (itemData == null || itemData.prefab == null) return;

        Transform playerTransform = PlayerController.Instance?.transform;
        if (playerTransform == null) return;

        GameObject droppedItem = Instantiate(itemData.prefab);
        droppedItem.transform.position =
            playerTransform.position + playerTransform.forward * 2f + Vector3.up * 0.5f;

        var pickup = droppedItem.GetComponent<ItemPickupInteractable>();
        if (pickup == null)
            pickup = droppedItem.AddComponent<ItemPickupInteractable>();

        pickup.itemData = itemData;
        pickup.quantity = quantity;
        pickup.isBeingPickedUp = false;

        ClearSlot();

        if (slotType == SlotType.Inventory)
            SyncToInventory();
        else
            StorageUIManager.Instance?.SaveAndSyncImmediate();

        StorageNavigation.Instance?.RefreshSlots();
    }

    private IEnumerator FlashSlotRed(StorageSlotUI slot)
    {
        var graphic = slot.GetComponent<Image>();
        if (graphic != null)
        {
            Color originalColor = graphic.color;
            graphic.color = Color.red;
            yield return new WaitForSecondsRealtime(0.2f);
            graphic.color = originalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
            return;

        if (Input.GetKey(KeyCode.LeftShift) && itemData != null)
        {
            HandleShiftClickTransfer();
            return;
        }

        float timeSinceLastClick = Time.unscaledTime - lastClickTime;
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            OnControllerSelect();
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
    }

    private void HandleShiftClickTransfer()
    {
        if (slotType == SlotType.Storage)
            TransferToPlayerInventory();
        else if (slotType == SlotType.Inventory)
            TransferToStorageViaYButton();
    }
}