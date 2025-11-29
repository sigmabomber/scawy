using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Equipment Point")]
    [SerializeField] private Transform equipPoint;

    [Header("Settings")]
    [SerializeField] private KeyCode equipKey = KeyCode.E;
    [SerializeField] private KeyCode unequipKey = KeyCode.Q;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask itemLayer;

    [Header("Sway Settings")]
    [SerializeField] private float swaySmoothing = 8f;
    [SerializeField] private float swayAmount = 0.02f;

    private IEquippable currentlyEquippedItem;
    private Camera playerCamera;
    private Quaternion targetRotation;
    private Vector3 targetPosition;

    private void Start()
    {
        playerCamera = Camera.main;

        // Create default equip point if not assigned
        if (equipPoint == null)
        {
            equipPoint = CreateEquipPoint("EquipPoint", new Vector3(0.5f, -0.3f, 0.5f));
        }

        targetRotation = equipPoint.localRotation;
        targetPosition = equipPoint.localPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(equipKey))
        {
            if (currentlyEquippedItem == null)
            {
                TryPickupItem();
            }
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

    private void ApplyWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Calculate sway based on mouse movement
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY * swayAmount, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX * swayAmount, Vector3.up);

        targetRotation = rotationX * rotationY;

        // Smoothly interpolate to target rotation
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
            IEquippable equippable = hit.collider.GetComponent<IEquippable>();

            if (equippable != null)
            {
                EquipItem(equippable);
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

    // Public getters
    public IEquippable GetEquippedItem() => currentlyEquippedItem;
    public bool IsEquipped() => currentlyEquippedItem != null;
}