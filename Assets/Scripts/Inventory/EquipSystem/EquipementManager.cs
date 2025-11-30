using System.Collections;
using TMPro;
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


    [Header("Bobbing Settings")]
    private Vector3 originalEquipPointPosition;
    public float effectIntensity, effectIntensityX, effectSpeed, SinTime;

    private Vector3 lastPlayerPosition;
    private float playerVelocity;

    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private GameObject reticleUI;
    [SerializeField] private TMP_Text itemNameText;

    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    public  IEquippable currentlyEquippedItem;
    private Camera playerCamera;
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private GameObject currentHighlightedObject;

    private void Start()
    {
        playerCamera = Camera.main;
        lastPlayerPosition = playerCamera.transform.position;
        if (equipPoint == null)
        {
            equipPoint = CreateEquipPoint("EquipPoint", new Vector3(0.5f, -0.3f, 0.5f));
        }

        originalEquipPointPosition = equipPoint.localPosition;

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
            itemNameText.gameObject.SetActive(false);
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
            playerVelocity = (playerCamera.transform.position - lastPlayerPosition).magnitude / Time.deltaTime;
            lastPlayerPosition = playerCamera.transform.position;
            ApplyWeaponSway();

            if (playerVelocity > 0.1f)   // Moving?
                ApplyWeaponBobbing();
            else
                ResetBobbingPosition();

        }

    }
    private void ResetBobbingPosition()
    {
        equipPoint.localPosition = Vector3.Lerp(
            equipPoint.localPosition,
            originalEquipPointPosition,
            Time.deltaTime * 6f
        );
        SinTime = 0f;
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
                    itemNameText.text = itemPickup.itemData.itemName;
                    itemNameText.gameObject.SetActive(true);
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
            itemNameText.gameObject.SetActive(false);
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

    private void ApplyWeaponBobbing()
    {
        // Increase time based on speed
        SinTime += Time.deltaTime * effectSpeed;

        // Calculate bobbing offsets
        float bobX = Mathf.Sin(SinTime) * effectIntensityX;
        float bobY = Mathf.Abs(Mathf.Cos(SinTime)) * effectIntensity;

        // Apply movement
        Vector3 bobOffset = new Vector3(bobX, bobY, 0);

        equipPoint.localPosition = Vector3.Lerp(
            equipPoint.localPosition,
            originalEquipPointPosition + bobOffset,
            Time.deltaTime * 6f
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

                    // Start coroutine to move item to player before destroying
                    StartCoroutine(MoveItemToPlayerThenDestroy(hit.collider.gameObject));
                }
                else
                {
                    Debug.Log("Inventory is full!");
                }
            }
        }
    }

    private IEnumerator MoveItemToPlayerThenDestroy(GameObject itemObject)
    {
        Collider itemCollider = itemObject.GetComponent<Collider>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 startPosition = itemObject.transform.position;
        Vector3 playerPosition = transform.position;

        while (elapsedTime < duration)
        {
            if (itemObject == null) yield break;

            itemObject.transform.position = Vector3.Lerp(startPosition, playerPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (itemObject != null)
            Destroy(itemObject);
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
        else
            print(":()");
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