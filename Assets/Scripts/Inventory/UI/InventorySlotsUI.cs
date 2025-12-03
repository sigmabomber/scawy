using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
public class InventorySlotsUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{

    // Data
    public ItemData itemData;
    public int quantity;
    public float usage;

    //  Managers
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

    void Awake()
    {
        inventorySystem = InventorySystem.Instance;
        canvas = GetComponentInParent<Canvas>();
        SetItem(itemData, quantity);

        if (_dragManager == null)
            _dragManager = new DragHandlerManager();

        equipmentManager = EquipmentManager.Instance;

       
            if (PlayerController.Instance != null)
                playerTransform = PlayerController.Instance.transform;
        
    }

    private void Start()
    {
        lastClickTime = 0f;
    }

    public void SetItem(ItemData newItem, int newQuantity, GameObject itemObj = null)
    {
        itemData = newItem;
        quantity = newQuantity;
        if (itemObj != null)
        instantiatedPrefab = itemObj != null ? itemObj : null;
        usageSlider.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        if(newItem != null)
        icon.gameObject.SetActive(true);
        if (itemData == null) return;

        if (itemData.maxStack > 1 && itemData is not FlashlightItemData)
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

                flashlightBehavior.Initialize(flashlightItemData);
            }
            usageSlider.gameObject.SetActive(true);
            usageSlider.maxValue = flashlightItemData.maxBattery;
            usageSlider.value = itemObj != null ? flashlightBehavior.GetCurrentBattery() : flashlightItemData.maxBattery;
           

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
        if (isEquipped)
            OnUnequip();

        itemData = null;
        quantity = 0;
        instantiatedPrefab = null;
        icon.gameObject.SetActive(false);
        quantityText.gameObject.SetActive(false);
        usageSlider.gameObject.SetActive(false);
        isEquipped = false;
    }

    public void OnEquip()
    {
        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning("Cannot equip: No item data or prefab");
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
            Debug.LogWarning("EquipmentManager not found!");
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
        }
        else
        {
            isEquipped = true;
        }

        var mainUsableComponents = instantiatedPrefab.GetComponents<IItemUsable>();
        foreach (var usable in mainUsableComponents)
        {
            usable?.OnEquip(this);
        }

        var childUsableComponents = instantiatedPrefab.GetComponentsInChildren<IItemUsable>(true);
        var mainComponentsList = mainUsableComponents.ToList(); // Convert to List for Contains

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
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData == null)
        {
            return;
        }

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
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
            lastClickTime = Time.time;
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

        bool droppedOnInventorySlot = false;

        foreach (var result in results)
        {
            if (result.gameObject == null) continue;

            Debug.Log($"Checking: {result.gameObject.name}");

            var targetSlot = result.gameObject.GetComponentInParent<InventorySlotsUI>();
            if (targetSlot != null)
            {
                Debug.Log($"Found InventorySlotsUI on: {targetSlot.gameObject.name}");

                if (targetSlot != this)
                {
                    _dragManager.TryHandleDrag(this, targetSlot);
                }
                droppedOnInventorySlot = true;
                break;
            }

            if (result.gameObject.GetComponent<InventorySlotsUI>() != null ||
                result.gameObject.GetComponent<InventorySystem>() != null )
            {
                droppedOnInventorySlot = true;
                break;
            }
        }

        if (!droppedOnInventorySlot)
        {
            DropItemInWorld();
        }
        else
        {
        }
    }
    private void DropItemInWorld()
    {
        if (itemData == null || itemData.prefab == null)
        {
            print(":(");
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

        ClearSlot();
    }

}