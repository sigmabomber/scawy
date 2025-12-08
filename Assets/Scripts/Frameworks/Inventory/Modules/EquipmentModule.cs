using UnityEngine;
using System.Collections.Generic;
using Doody.InventoryFramework;
namespace Doody.InventoryFramework.Modules
{
    public interface IEquipmentModule : IInventoryModule
    {
        void EquipItem(IInventorySlotUI slot);
        void UnequipItem(IInventorySlotUI slot);
        bool IsEquipped(IInventorySlotUI slot);
        IEquippable GetEquippedItem();
    }

    public class EquipmentModule : IEquipmentModule
    {
        public string ModuleName => "Equipment";
        public bool IsEnabled => true;

        private Transform equipPoint;
        private IEquippable currentlyEquippedItem;
        private Camera playerCamera;
        private Vector3 originalEquipPointPosition;
        private Quaternion targetRotation;
        private Vector3 lastPlayerPosition;
        private float playerVelocity;
        private float sinTime;

        // Settings
        private KeyCode unequipKey = KeyCode.G;
        private float swaySmoothing = 8f;
        private float swayAmount = 0.02f;
        private float effectIntensity = 0.02f;
        private float effectIntensityX = 0.01f;
        private float effectSpeed = 10f;

        private IInventoryFramework framework;

        public void Initialize(IInventoryFramework framework)
        {
            this.framework = framework;

            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[EquipmentModule] Main camera not found!");
                return;
            }

            equipPoint = CreateEquipPoint("EquipPoint", new Vector3(0.5f, -0.3f, 0.5f));
            originalEquipPointPosition = equipPoint.localPosition;
            targetRotation = equipPoint.localRotation;
            lastPlayerPosition = playerCamera.transform.position;

            Debug.Log($"[EquipmentModule] Initialized");
        }

        public void EquipItem(IInventorySlotUI slot)
        {
            if (slot == null || slot.ItemData == null || slot.ItemData.prefab == null)
                return;

            if (currentlyEquippedItem != null)
                UnequipItem(slot);

            GameObject prefab = slot.InstantiatedPrefab ?? GameObject.Instantiate(slot.ItemData.prefab);

            IEquippable equippable = prefab.GetComponent<IEquippable>();
            if (equippable == null)
            {
                var defaultEquippable = prefab.AddComponent<DefaultEquippable>();
                equippable = defaultEquippable as IEquippable;
            }

            currentlyEquippedItem = equippable;
            currentlyEquippedItem.OnEquip(equipPoint);

            if (slot is InventorySlotsUI uiSlot)
            {
                
            }
        }

        public void UnequipItem(IInventorySlotUI slot = null)
        {
            if (currentlyEquippedItem != null)
            {
                currentlyEquippedItem.OnUnequip();
                currentlyEquippedItem = null;
            }
        }

        public bool IsEquipped(IInventorySlotUI slot)
        {
            return false;
        }

        public IEquippable GetEquippedItem() => currentlyEquippedItem;

        public void OnInventorySystemCreated(IInventorySystem system)
        {
            
        }

        public void Update(float deltaTime)
        {
            if (currentlyEquippedItem != null)
            {
                playerVelocity = (playerCamera.transform.position - lastPlayerPosition).magnitude / deltaTime;
                lastPlayerPosition = playerCamera.transform.position;

                ApplyWeaponSway();

                if (playerVelocity > 0.1f)
                    ApplyWeaponBobbing();
                else
                    ResetBobbingPosition();

                if (Input.GetKeyDown(unequipKey))
                    UnequipItem();
            }
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

        private void ResetBobbingPosition()
        {
            equipPoint.localPosition = Vector3.Lerp(
                equipPoint.localPosition,
                originalEquipPointPosition,
                Time.deltaTime * 6f
            );
            sinTime = 0f;
        }

        private Transform CreateEquipPoint(string name, Vector3 localPosition)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(playerCamera.transform);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            return point.transform;
        }

        public void Shutdown()
        {
            UnequipItem();
            currentlyEquippedItem = null;

            if (equipPoint != null)
                GameObject.Destroy(equipPoint.gameObject);
        }
    }
}



public class DefaultEquippable : MonoBehaviour, IEquippable, IItemUsable
{
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool wasActive;

    // IEquippable implementation
    public void OnEquip(Transform equipPoint)
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        wasActive = gameObject.activeSelf;

        transform.SetParent(equipPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        gameObject.SetActive(true);

        SetLayerRecursively(gameObject, LayerMask.NameToLayer("PickedUpItem"));

        Debug.Log($"{gameObject.name} equipped");
    }

    public void OnUnequip()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }
        else
        {
            transform.SetParent(null);
        }

        gameObject.SetActive(wasActive);

        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Default"));

        Debug.Log($"{gameObject.name} unequipped");
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnEquip(InventorySlotsUI slot)
    {
        Debug.Log($"{gameObject.name} equipped from slot");
    }

    public void OnUse(InventorySlotsUI slot)
    {
        Debug.Log($"{gameObject.name} used");
    }

    public void OnUnequip(InventorySlotsUI slot)
    {
        Debug.Log($"{gameObject.name} unequipped from slot");
    }

    public void OnItemStateChanged(ItemState previousState, ItemState newState)
    {
        Debug.Log($"{gameObject.name} state changed from {previousState} to {newState}");

        switch (newState)
        {
            case ItemState.InWorld:
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("Interactable"));
                EnablePhysics(true);
                break;

            case ItemState.InInventory:
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("PickedUpItem"));
                EnablePhysics(false);
                break;

            case ItemState.Equipped:
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("PickedUpItem"));
                EnablePhysics(false);
                break;
        }
    }

    public void OnDroppedInWorld()
    {
        Debug.Log($"{gameObject.name} dropped in world");
        EnablePhysics(true);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void EnablePhysics(bool enable)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enable;
            rb.useGravity = enable;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = enable;
        }
    }
}