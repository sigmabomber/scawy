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
    [SerializeField] private KeyCode unequipKey = KeyCode.None;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask itemLayer;

    public SlotPriority slotPriority = SlotPriority.Normal;

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

    private Color originalTextColor;
    private bool isShowingFeedback = false;

    public IEquippable currentlyEquippedItem;
    private Camera playerCamera;
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private GameObject currentHighlightedObject;

    private void Start()
    {
        playerCamera = Camera.main;
        lastPlayerPosition = playerCamera.transform.position;

        if (itemNameText != null)
            originalTextColor = itemNameText.color;

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

        if (reticleUI != null)
        {
            reticleUI.SetActive(false);
            itemNameText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
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

        if (currentlyEquippedItem != null)
        {
            playerVelocity = (playerCamera.transform.position - lastPlayerPosition).magnitude / Time.deltaTime;
            lastPlayerPosition = playerCamera.transform.position;
            ApplyWeaponSway();

            if (playerVelocity > 0.1f)   
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
        if (isShowingFeedback)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, pickupRange, itemLayer))
            {
                StopAllCoroutines();
                isShowingFeedback = false;
                itemNameText.color = originalTextColor;

                if (reticleUI != null)
                {
                    reticleUI.SetActive(false);
                    itemNameText.gameObject.SetActive(false);
                }
                RemoveOutline();
            }
            return;
        }

        Ray checkRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit checkHit;

        if (Physics.Raycast(checkRay, out checkHit, pickupRange, itemLayer))
        {
            ItemPickup itemPickup = checkHit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null && itemPickup.itemData != null)
            {
                if (reticleUI != null)
                {
                    reticleUI.SetActive(true);
                    itemNameText.text = itemPickup.itemData.itemName;
                    itemNameText.gameObject.SetActive(true);
                }

                if (currentHighlightedObject != checkHit.collider.gameObject)
                {
                    RemoveOutline();
                    AddOutline(checkHit.collider.gameObject);
                }

                return;
            }
        }

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
        SinTime += Time.deltaTime * effectSpeed;

        float bobX = Mathf.Sin(SinTime) * effectIntensityX;
        float bobY = Mathf.Abs(Mathf.Cos(SinTime)) * effectIntensity;

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
            ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null && itemPickup.itemData != null)
            {
                SlotPriority priority = SlotPriority.Normal;

                var itemDataType = itemPickup.itemData.GetType();
                var priorityField = itemDataType.GetField("preferredSlotType");
                if (priorityField != null)
                {
                    priority = (SlotPriority)priorityField.GetValue(itemPickup.itemData);
                }
                else
                {
                    priority = itemPickup.slotPriority;
                }

                if (inventorySystem != null && inventorySystem.AddItem(itemPickup.itemData, itemPickup.quantity, priority))
                {
                    StartCoroutine(MoveItemToPlayerThenDestroy(hit.collider.gameObject));
                }
                else
                {
                    StartCoroutine(ShowFeedback("Backpack Full!", Color.red));
                }
            }
        }
    }

    private IEnumerator ShowFeedback(string message, Color textColor)
    {
        if (itemNameText != null)
        {
            isShowingFeedback = true;

            Color originalColor = itemNameText.color;
            itemNameText.text = message;
            itemNameText.color = textColor;

            yield return new WaitForSeconds(1.5f);

            isShowingFeedback = false;

            itemNameText.color = originalColor;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, pickupRange, itemLayer))
            {
                ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();
                if (itemPickup != null && itemPickup.itemData != null)
                {
                    itemNameText.text = itemPickup.itemData.itemName;
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