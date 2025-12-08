using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlashlightBehavior : MonoBehaviour, IItemUsable
{
    public float currentBattery;
    private bool isOn = false;
    private FlashlightItemData flashlightData;
    private InventorySlotsUI slot;

    private float flickerTimer = 0f;
    private GameObject SpotLightObj;
    private Light flashlightLight;

    public bool initialized = false;

    private float maxIntensity;
    private float minIntensity = 0.3f;

    private float flickerSpeed;

    // State tracking
    private ItemStateTracker stateTracker;
    private bool canUpdate = false;

    private void Start()
    {
        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
        {
            stateTracker = gameObject.AddComponent<ItemStateTracker>();
        }

        if (gameObject.activeInHierarchy)
        {
            SpotLightObj = transform.Find("Spot Light").gameObject;

            if (SpotLightObj != null)
            {
                SpotLightObj.SetActive(false);
                flashlightLight = SpotLightObj.GetComponent<Light>();

                maxIntensity = flashlightLight.intensity;
            }

            UpdateBehaviorBasedOnState();
        }
    }

    // IItemUsable methods
    public void OnUse(InventorySlotsUI slotUI)
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        if (!initialized)
        {
            OnEquip(slotUI);
        }
        else
        {
            ToggleFlashlight();
        }
    }

    public void OnEquip(InventorySlotsUI slotUI)
    {
        slot = slotUI;

        if (slot.itemData is FlashlightItemData flashlight)
        {
            flashlightData = flashlight;
            flickerSpeed = flashlightData.flickerSpeed;
            if (!initialized)
            {
                Initialize(flashlight);
            }

            if (currentBattery > 0 && stateTracker != null && stateTracker.IsEquipped)
            {
                ToggleFlashlight();
            }

            slot.UpdateUsage(currentBattery);
        }
    }
    public void Initialize(FlashlightItemData data)
    {
        if (initialized) return;
        flashlightData = data;
        currentBattery = flashlightData.maxBattery;
        initialized = true;

    }
    public void OnUnequip(InventorySlotsUI slotUI)
    {
        if (isOn)
        {
            ToggleFlashlight();
        }
    }

    public void OnItemStateChanged(ItemState previousState, ItemState newState)
    {

        UpdateBehaviorBasedOnState();

        // Specific state handling
        if (newState == ItemState.Equipped)
        {
            canUpdate = true;

            if (isOn)
            {
                ToggleFlashlight();
            }
        }
        else if (newState == ItemState.InWorld)
        {
            canUpdate = false;

            if (isOn)
            {
                ToggleFlashlight();
            }

            slot = null;
            flashlightData = null;
        }
        else if (newState == ItemState.InInventory)
        {
            canUpdate = false;

            if (isOn)
            {
                ToggleFlashlight();
            }
        }
    }

    public void OnPickedUp()
    {
        Debug.Log("Flashlight picked up");
        // Reset any pickup-specific states if needed
    }

    public void OnDroppedInWorld()
    {
      
        if (isOn)
        {
            ToggleFlashlight();
        }
    }

    private void UpdateBehaviorBasedOnState()
    {
        if (stateTracker == null) return;

        if (stateTracker.IsEquipped)
        {
            canUpdate = true;
        }
        else
        {
            canUpdate = false;

            if (isOn)
            {
                ToggleFlashlight();
            }
        }
    }

    void ToggleFlashlight()
    {
        if (SpotLightObj == null) return;

        if (stateTracker != null && !stateTracker.IsEquipped) return;

        isOn = !isOn;
        SpotLightObj.SetActive(isOn);

    }

    private void Update()
    {
        if (!canUpdate || flashlightData == null || !initialized) return;

        if (isOn)
            DrainBattery();

        if (stateTracker != null && stateTracker.IsEquipped)
        {
            if (Input.GetMouseButtonUp(0) && currentBattery > 0)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                ToggleFlashlight();
            }
        }
    }

    private void FlickerLight()
    {
        if (flashlightLight == null || !isOn) return;

        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= 0)
        {
            flashlightLight.intensity = Random.Range(minIntensity, maxIntensity);
            flickerTimer = flickerSpeed;
        }
    }

    private void DrainBattery()
    {
        if (flashlightData == null || slot == null) return;

        float drainPerSecond = flashlightData.batteryDrainage;
        float deltaBattery = drainPerSecond * Time.deltaTime;
        currentBattery -= drainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0);

        if (slot != null)
            slot.UpdateUsage(Mathf.RoundToInt(currentBattery));

        if (currentBattery <= 0)
        {
            isOn = false;
            if (SpotLightObj != null)
                SpotLightObj.SetActive(isOn);
        }

        if (currentBattery / flashlightData.maxBattery <= flashlightData.flickerThreshhold && isOn)
        {
            FlickerLight();
        }
        else
        {
            if (flashlightLight != null)
                flashlightLight.intensity = maxIntensity;
        }
    }

    public void Recharge(float amount)
    {
        if (flashlightData == null)
        {
            Debug.LogWarning("FlashlightItemData is not assigned!");
            return;
        }

        currentBattery += amount;
        currentBattery = Mathf.Min(currentBattery, flashlightData.maxBattery);
        if (slot != null)
            slot.UpdateUsage(Mathf.RoundToInt(currentBattery));
    }

    public float GetCurrentBattery()
    {
       

        return currentBattery;
    }

    private void OnDestroy()
    {
        if (isOn && SpotLightObj != null)
        {
            SpotLightObj.SetActive(false);
        }
    }
}