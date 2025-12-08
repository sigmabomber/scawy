using UnityEngine;
using Doody.InventoryFramework;
public class EquipmentManager : MonoBehaviour
{

    // Singleton
    public static EquipmentManager Instance { get; private set; }
    [Header("Equipment Point")]
    [SerializeField] private Transform equipPoint;

    [Header("Settings")]
    [SerializeField] private KeyCode unequipKey = KeyCode.G;

    [Header("Sway Settings")]
    [SerializeField] private float swaySmoothing = 8f;
    [SerializeField] private float swayAmount = 0.02f;

    [Header("Bobbing Settings")]
    [SerializeField] private float effectIntensity = 0.02f;
    [SerializeField] private float effectIntensityX = 0.01f;
    [SerializeField] private float effectSpeed = 10f;

    private Vector3 originalEquipPointPosition;
    private float sinTime;
    private Vector3 lastPlayerPosition;
    private float playerVelocity;

    public IEquippable currentlyEquippedItem { get; private set; }
    private Camera playerCamera;
    private Quaternion targetRotation;

    [Header("References")]
    private InteractionSystem interactionSystem;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        playerCamera = Camera.main;
        lastPlayerPosition = playerCamera.transform.position;

        if (equipPoint == null)
        {
            equipPoint = CreateEquipPoint("EquipPoint", new Vector3(0.5f, -0.3f, 0.5f));
        }

        originalEquipPointPosition = equipPoint.localPosition;
        targetRotation = equipPoint.localRotation;

        interactionSystem = GetComponent<InteractionSystem>();
        if (interactionSystem == null)
        {
            interactionSystem = gameObject.AddComponent<InteractionSystem>();
        }
    }

    private void Update()
    {
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
        sinTime = 0f;
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
        sinTime += Time.deltaTime * effectSpeed;

        float bobX = Mathf.Sin(sinTime) * effectIntensityX;
        float bobY = Mathf.Abs(Mathf.Cos(sinTime)) * effectIntensity;

        Vector3 bobOffset = new Vector3(bobX, bobY, 0);

        equipPoint.localPosition = Vector3.Lerp(
            equipPoint.localPosition,
            originalEquipPointPosition + bobOffset,
            Time.deltaTime * 6f
        );
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
        {
            Debug.Log("No item equipped");
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
    public Transform GetEquipPoint() => equipPoint;
}