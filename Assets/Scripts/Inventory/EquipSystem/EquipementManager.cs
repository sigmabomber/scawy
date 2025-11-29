using UnityEngine;
using UnityEngine.UI;

public class EquipmentManager : MonoBehaviour
{
    [Header("Equipment Point")]
    [SerializeField] private Transform equipPoint;

    [Header("Settings")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode unequipKey = KeyCode.Q;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask itemLayer;

    [Header("Sway Settings")]
    [SerializeField] private float swaySmoothing = 8f;
    [SerializeField] private float swayAmount = 0.02f;

    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private GameObject reticleUI;

    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    private IEquippable currentlyEquippedItem;
    private Camera playerCamera;
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private GameObject currentHighlightedObject;

    private void Start()
    {
        playerCamera = Camera.main;

        if (equipPoint == null)
        {
            equipPoint = CreateEquipPoint("EquipPoint", new Vector3(0.5f, -0.3f, 0.5f));
        }

        // Find inventory system if not assigned
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }

        targetRotation = equipPoint.localRotation;
        targetPosition = equipPoint.localPosition;

        // Hide reticle by default
        if (reticleUI != null)
        {
            reticleUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Check if looking at pickable item
        CheckForPickableItem();

        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }

        if (Input.GetKeyDown(unequipKey))
        {
            if (currentlyEquippedItem != null)
            {
                UnequipItem();
            }
        }

        // Apply weapon sway
        if (currentlyEquippedItem != null)
        {
            ApplyWeaponSway();
        }
    }

    private void CheckForPickableItem()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, itemLayer))
        {
            ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null && itemPickup.itemData != null)
            {
                // Show reticle
                if (reticleUI != null)
                {
                    reticleUI.SetActive(true);
                }

                // Add outline to the object
                if (currentHighlightedObject != hit.collider.gameObject)
                {
                    RemoveOutline();
                    AddOutline(hit.collider.gameObject);
                }

                return;
            }
        }

        // Hide reticle and remove outline when not looking at item
        if (reticleUI != null)
        {
            reticleUI.SetActive(false);
        }
        RemoveOutline();
    }

    private void AddOutline(GameObject obj)
    {
        currentHighlightedObject = obj;

        // Try to get or add Outline component
        Outline outline = obj.GetComponent<Outline>();

        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }

        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = true;
    }

    private void RemoveOutline()
    {
        if (currentHighlightedObject != null)
        {
            Outline outline = currentHighlightedObject.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled = false;
            }

            currentHighlightedObject = null;
        }
    }

    private void OnDisable()
    {
        // Clean up outline when script is disabled
        RemoveOutline();
    }

    private void ApplyWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Quaternion rotationX = Quaternion.AngleAxis(-mouseY * swayAmount, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX * swayAmount, Vector3.up);

        targetRotation = rotationX * rotationY;

        equipPoint.localRotation = Quaternion.Slerp(
            equipPoint.localRotation,
            targetRotation,
            swaySmoothing * Time.deltaTime
        );
    }

    private void TryPickupItem()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, itemLayer))
        {
            // Try to get ItemPickup component
            ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null && itemPickup.itemData != null)
            {
                // Add to inventory
                if (inventorySystem != null && inventorySystem.AddItem(itemPickup.itemData, itemPickup.quantity))
                {
                    Debug.Log($"Picked up {itemPickup.itemData.name}");
                    Destroy(hit.collider.gameObject);
                }
                else
                {
                    Debug.Log("Inventory is full!");
                }
            }
        }
    }

    public void EquipItem(IEquippable item)
    {
        if (currentlyEquippedItem != null)
        {
            UnequipItem();
        }

        currentlyEquippedItem = item;
        item.OnEquip(equipPoint);
    }

    public void UnequipItem()
    {
        if (currentlyEquippedItem != null)
        {
            currentlyEquippedItem.OnUnequip();
            currentlyEquippedItem = null;
        }
    }

    private Transform CreateEquipPoint(string name, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(playerCamera.transform);
        point.transform.localPosition = localPosition;
        point.transform.localRotation = Quaternion.identity;
        return point.transform;
    }

    public IEquippable GetEquippedItem() => currentlyEquippedItem;
    public bool IsEquipped() => currentlyEquippedItem != null;
}