using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class StorageSlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler,
    IPointerEnterHandler,      // Add this
    IPointerExitHandler,       // Add this
    IPointerDownHandler,       // Add this
    IPointerUpHandler          // Add this
{
    [Header("UI Elements")]
    public UnityEngine.UI.Image icon;
    public TMPro.TMP_Text quantityText;

    private StorageContainer parentStorage;
    private int slotIndex;

    public ItemData itemData;
    private int quantity;
    private GameObject itemPrefab;

    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public GameObject ItemPrefab => itemPrefab;

    // Dragging
    private GameObject dragIcon;
    private Canvas canvas;
    private Color isDragging = new Color(1f, 1f, 1f, 0.5f);
    private Color original = new Color(1f, 1f, 1f, 1f);

    private static DragHandlerManager _dragManager;



    private void Start()
    {
        Debug.Log($"[StorageSlotUI] Start() called on {gameObject.name}");

        canvas = GetComponentInParent<Canvas>();
        Debug.Log($"[StorageSlotUI] Canvas found: {canvas != null} ({canvas?.name})");

        if (_dragManager == null)
        {
            Debug.Log("[StorageSlotUI] Creating new DragManager");
            _dragManager = new DragHandlerManager();
        }

        // Ensure EventTrigger component exists
        var eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            Debug.Log($"[StorageSlotUI] Adding EventTrigger component");
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }
        else
        {
            Debug.Log($"[StorageSlotUI] EventTrigger already exists");
        }

        // Force all graphics to accept raycasts
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            Debug.Log($"[StorageSlotUI] Adding Image component for raycast");
            image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0); // Transparent but still receives events
        }
        image.raycastTarget = true;

        if (icon != null)
        {
            icon.raycastTarget = true;
            Debug.Log($"[StorageSlotUI] Icon raycast target set to true");
        }

        // Check for CanvasGroup
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"[StorageSlotUI] Has CanvasGroup - blocksRaycasts: {canvasGroup.blocksRaycasts}, interactable: {canvasGroup.interactable}");

            // Ensure it doesn't block raycasts
            if (!canvasGroup.blocksRaycasts)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log($"[StorageSlotUI] Fixed CanvasGroup - blocksRaycasts now true");
            }
        }

        // Debug the full setup
        DebugSetup();
    }

    private void DebugSetup()
    {
        Debug.Log($"[StorageSlotUI] === Debug Setup for {gameObject.name} ===");

        // Check all parent CanvasGroups
        Transform current = transform;
        while (current != null)
        {
            var parentCanvasGroup = current.GetComponent<CanvasGroup>();
            if (parentCanvasGroup != null)
            {
                Debug.Log($"[StorageSlotUI] Parent '{current.name}' has CanvasGroup - blocksRaycasts: {parentCanvasGroup.blocksRaycasts}");
            }
            current = current.parent;
        }

        // Check for GraphicRaycaster on canvas
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError($"[StorageSlotUI] Canvas {canvas.name} has no GraphicRaycaster!");
            }
            else
            {
                Debug.Log($"[StorageSlotUI] GraphicRaycaster found: {raycaster.enabled}");
            }
        }

        // Check EventSystem
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError($"[StorageSlotUI] No EventSystem in scene!");
        }
        else
        {
            Debug.Log($"[StorageSlotUI] EventSystem: {eventSystem.gameObject.name}");
            Debug.Log($"[StorageSlotUI] Current Input Module: {eventSystem.currentInputModule?.GetType().Name}");
        }

        Debug.Log($"[StorageSlotUI] === End Debug Setup ===");
    }





    public void SetStorageReference(StorageContainer storage, int index)
    {
        parentStorage = storage;
        slotIndex = index;
    }

    public void SetItem(ItemData newItem, int newQuantity, GameObject prefab = null)
    {
        itemData = newItem;
        quantity = newQuantity;
        itemPrefab = prefab;

        if (itemData == null)
        {
            icon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
            return;
        }

        icon.sprite = itemData.icon;
        icon.gameObject.SetActive(true);
        icon.raycastTarget = true;

        if (itemData.maxStack > 1)
        {
            quantityText.text = quantity.ToString();
            quantityText.gameObject.SetActive(true);
        }
        else
        {
            quantityText.gameObject.SetActive(false);
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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[StorageSlotUI] ===== OnBeginDrag START =====");
        Debug.Log($"[StorageSlotUI] Slot: {gameObject.name}, ItemData: {itemData != null} ({itemData?.name})");
        Debug.Log($"[StorageSlotUI] Canvas: {canvas != null}, ParentStorage: {parentStorage != null}");
        Debug.Log($"[StorageSlotUI] Event position: {eventData.position}, Pressed: {eventData.pointerPress?.name}");

        if (itemData == null)
        {
            Debug.LogWarning("[StorageSlotUI] Can't drag - no item data");
            return;
        }

        if (canvas == null)
        {
            Debug.LogError("[StorageSlotUI] Canvas is null!");
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
        }

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<UnityEngine.UI.Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;

        icon.color = isDragging;
        dragIcon.GetComponent<RectTransform>().sizeDelta = icon.rectTransform.sizeDelta;

        Debug.Log($"[StorageSlotUI] Drag icon created");
        Debug.Log($"[StorageSlotUI] ===== OnBeginDrag END =====");
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void PostActivationSetup()
    {
        Debug.Log($"[StorageSlotUI] PostActivationSetup called on {gameObject.name}");

        // Ensure everything is set up correctly after activation
        gameObject.SetActive(true);
        enabled = true;

        // Ensure Image component exists and has raycast target
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            Debug.Log($"[StorageSlotUI] Adding Image component to {gameObject.name}");
            image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0); // Transparent
        }
        image.raycastTarget = true;

        // Ensure EventTrigger
        if (GetComponent<EventTrigger>() == null)
        {
            Debug.Log($"[StorageSlotUI] Adding EventTrigger to {gameObject.name}");
            gameObject.AddComponent<EventTrigger>();
        }

        // Ensure icon has raycast target
        if (icon != null)
        {
            icon.raycastTarget = true;
        }

        // Check RectTransform size
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"[StorageSlotUI] {gameObject.name} size: {rectTransform.sizeDelta}");
            if (rectTransform.sizeDelta.x < 10 || rectTransform.sizeDelta.y < 10)
            {
                Debug.LogWarning($"[StorageSlotUI] {gameObject.name} might be too small for interaction!");
                rectTransform.sizeDelta = new Vector2(100, 100); // Set minimum size
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            Destroy(dragIcon);

        icon.color = original;

        var results = new List<RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Check if dropped on player inventory slot
            var playerSlot = result.gameObject.GetComponentInParent<InventorySlotsUI>();
            if (playerSlot != null)
            {
                TransferToPlayerInventory(playerSlot);
                return;
            }

            // Check if dropped on another storage slot
            var storageSlot = result.gameObject.GetComponentInParent<StorageSlotUI>();
            if (storageSlot != null && storageSlot != this)
            {
                SwapWithStorageSlot(storageSlot);
                return;
            }
        }
    }

    private void TransferToPlayerInventory(InventorySlotsUI targetSlot)
    {
        if (itemData == null) return;

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
            {
                ClearSlot();
            }
            else
            {
                SetItem(itemData, quantity, itemPrefab);
            }
        }
        else
        {
            ItemData tempItem = targetSlot.itemData;
            int tempQty = targetSlot.quantity;
            GameObject tempPrefab = targetSlot.instantiatedPrefab;

            targetSlot.SetItem(itemData, quantity, itemPrefab);
            SetItem(tempItem, tempQty, tempPrefab);
        }
    }

    private void SwapWithStorageSlot(StorageSlotUI otherSlot)
    {
        ItemData tempItem = otherSlot.itemData;
        int tempQty = otherSlot.quantity;
        GameObject tempPrefab = otherSlot.itemPrefab;

        otherSlot.SetItem(itemData, quantity, itemPrefab);
        SetItem(tempItem, tempQty, tempPrefab);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[StorageSlotUI] OnPointerEnter on {gameObject.name}. Has item: {itemData != null}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[StorageSlotUI] OnPointerExit on {gameObject.name}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[StorageSlotUI] ===== OnPointerDown START =====");
        Debug.Log($"[StorageSlotUI] Mouse down on {gameObject.name}");
        Debug.Log($"[StorageSlotUI] Has item: {itemData != null}, Item: {itemData?.name}");
        Debug.Log($"[StorageSlotUI] Event position: {eventData.position}");
        Debug.Log($"[StorageSlotUI] PointerPress: {eventData.pointerPress?.name}");
        Debug.Log($"[StorageSlotUI] ===== OnPointerDown END =====");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[StorageSlotUI] OnPointerUp on {gameObject.name}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Input.GetKey(KeyCode.LeftShift) && itemData != null)
        {
            var playerMirrorSlots = StorageUIManager.Instance?.GetPlayerMirrorSlots();
            if (playerMirrorSlots != null)
            {
                // Try to stack first
                foreach (var slot in playerMirrorSlots)
                {
                    if (slot.itemData == itemData && slot.quantity < itemData.maxStack && itemData.maxStack > 1)
                    {
                        int spaceLeft = itemData.maxStack - slot.quantity;
                        int amountToAdd = Mathf.Min(spaceLeft, quantity);

                        slot.UpdateQuantity(slot.quantity + amountToAdd);

                        quantity -= amountToAdd;
                        if (quantity <= 0)
                        {
                            ClearSlot();
                            return;
                        }
                        else
                        {
                            SetItem(itemData, quantity, itemPrefab);
                            return;
                        }
                    }
                }

                // Find empty slot
                foreach (var slot in playerMirrorSlots)
                {
                    if (slot.itemData == null)
                    {
                        slot.SetItem(itemData, quantity, itemPrefab);
                        ClearSlot();
                        return;
                    }
                }

                Debug.Log("[Storage] Player inventory full!");
            }
        }
    }
}