using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotsUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{

    // Data
    public ItemData itemData;
    public int quantity;
    public float usage;

    //  Managers
    private static DragHandlerManager _dragManager;

    [Header("Equipment")]
    public EquipmentManager equipmentManager;

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


    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        SetItem(itemData, quantity);

        // Initialize the drag manager once
        if (_dragManager == null)
            _dragManager = new DragHandlerManager();

        // Find equipment manager if not assigned
        if (equipmentManager == null)
        {
            equipmentManager = FindObjectOfType<EquipmentManager>();
        }
    }

    private void Start()
    {
        // Don't auto-equip at start, wait for player interaction
        lastClickTime = 0f;
    }

    public void SetItem(ItemData newItem, int newQuantity)
    {
        itemData = newItem;
        quantity = newQuantity;

        // Disable all then re-enable only the necessary objects
        usageSlider.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        icon.gameObject.SetActive(true);
        if (itemData == null) return;

        // if item max stack is over 1 and isnt a flashlight
        if (itemData.maxStack > 1 && itemData is not FlashlightItemData)
        {
            quantityText.gameObject.SetActive(true);
        }

        if (itemData is FlashlightItemData flashlightItemData)
        {
            usageSlider.gameObject.SetActive(true);
            usageSlider.maxValue = flashlightItemData.maxBattery;
            usageSlider.value = flashlightItemData.maxBattery;
        }


        icon.sprite = itemData.icon;
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
        quantity = newQuantity;

        if (quantityText.gameObject.activeSelf)
            quantityText.text = quantity.ToString();
    }

    public void ClearSlot()
    {
        // Unequip before clearing
        if(isEquipped)
         OnUnequip();

        itemData = null;
        quantity = 0;
        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        usageSlider.gameObject.SetActive(false);
    }

    public void OnEquip()
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("EquipmentManager not found!");
            return;
        }

        if (itemData is FlashlightItemData flashlightData)
        {
            // Instantiate the flashlight prefab if not already created
            if (instantiatedPrefab == null && flashlightData.flashlightPrefab != null)
            {
                instantiatedPrefab = Instantiate(flashlightData.flashlightPrefab);
                instantiatedPrefab.SetActive(true);
            }

            instantiatedPrefab.SetActive(true);
            // Get the equippable component and equip it
            IEquippable equippable = instantiatedPrefab.GetComponent<IEquippable>();
            if (equippable != null)
            {
                equipmentManager.EquipItem(equippable);
                isEquipped = true;
            }

            // Handle usable interface (for flashlight on/off functionality)
            var usable = instantiatedPrefab.GetComponent<IItemUsable>();
            usable?.OnEquip(this);
        }
    }

    public void UseItem()
    {
        if (instantiatedPrefab == null) return;

        var usable = instantiatedPrefab.GetComponent<IItemUsable>();
        usable?.OnUse(this);
    }

    public void OnUnequip()
    {
        if (equipmentManager == null) return;

        // Unequip from equipment manager
        if (equipmentManager.IsEquipped())
        {
            equipmentManager.UnequipItem();
            isEquipped = false;
        }

        // Call the usable interface unequip
        if (instantiatedPrefab != null)
        {
            var usable = instantiatedPrefab.GetComponent<IItemUsable>();
            usable?.OnUnequip(this);
        }

        // Destroy the instantiated prefab
        if (instantiatedPrefab != null)
        {
            instantiatedPrefab.SetActive(false);
        }
    }
    
    // Double click handler
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData == null) return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // Double click detected - toggle equip/unequip
            if (isEquipped)
            {
                OnUnequip();
            }
            else
            {
                OnEquip();
            }

            lastClickTime = 0f; // Reset to prevent triple-click issues
        }
        else
        {
            // First click
            lastClickTime = Time.time;
        }
    }

    private void Update()
    {
        if (useItem)
        {
            useItem = false;
            UseItem();
        }
    }

    // Draggable events 
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

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
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            Destroy(dragIcon);

        icon.color = Original;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var targetSlot = result.gameObject.GetComponent<InventorySlotsUI>();
            if (targetSlot != null && targetSlot != this)
            {
                _dragManager.TryHandleDrag(this, targetSlot);
                break;
            }
        }
    }
}