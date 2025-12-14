using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Storage slot UI with priority checking
/// </summary>
public class StorageSlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("UI Elements")]
    public UnityEngine.UI.Image icon;
    public TMPro.TMP_Text quantityText;
    public UnityEngine.UI.Slider usageSlider; 

    private StorageContainer parentStorage;
    private int slotIndex;

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

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (_dragManager == null)
            _dragManager = new DragHandlerManager();

        if (icon != null)
            icon.raycastTarget = true;

        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            image.raycastTarget = true;
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

        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        if (usageSlider != null)
            usageSlider.gameObject.SetActive(false);

        if (itemData == null)
        {
            return;
        }

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
        if (itemData == null) return;

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<UnityEngine.UI.Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;

        icon.color = isDragging;
        dragIcon.GetComponent<RectTransform>().sizeDelta = icon.rectTransform.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
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
            var playerSlot = result.gameObject.GetComponentInParent<InventorySlotsUI>();
            if (playerSlot != null)
            {
                TransferToPlayerInventory(playerSlot);
                return;
            }

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

        if (!CanSlotAcceptItem(targetSlot, itemData))
        {
            
            StartCoroutine(FlashSlotRed(targetSlot));
            return;
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
            if (!CanSlotAcceptItem(this, targetSlot.itemData))
            {
                StartCoroutine(FlashSlotRed(targetSlot));
                return;
            }

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

        StorageUIManager.Instance?.SaveAndSyncImmediate();
    }

    private bool CanSlotAcceptItem(InventorySlotsUI slot, ItemData item)
    {
        if (slot == null || item == null) return false;
        return slot.slotPriority == item.priority;
    }

    private bool CanSlotAcceptItem(StorageSlotUI slot, ItemData item)
    {
        return true;
    }

    private System.Collections.IEnumerator FlashSlotRed(InventorySlotsUI slot)
    {
        var graphic = slot.GetComponent<UnityEngine.UI.Image>();
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
        if (Input.GetKey(KeyCode.LeftShift) && itemData != null)
        {
            var allPlayerSlots = StorageUIManager.Instance?.GetAllPlayerMirrorSlots();
            if (allPlayerSlots != null)
            {
                foreach (var slot in allPlayerSlots)
                {
                    if (slot.itemData == itemData &&
                        slot.quantity < itemData.maxStack &&
                        itemData.maxStack > 1 &&
                        CanSlotAcceptItem(slot, itemData))
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

                foreach (var slot in allPlayerSlots)
                {
                    if (slot.itemData == null && CanSlotAcceptItem(slot, itemData))
                    {
                        slot.SetItem(itemData, quantity, itemPrefab);
                        ClearSlot();
                        return;
                    }
                }

            }
        }
    }
}