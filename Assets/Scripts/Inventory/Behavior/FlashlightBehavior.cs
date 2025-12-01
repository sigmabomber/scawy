using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlashlightBehavior : MonoBehaviour, IItemUsable
{
    private float currentBattery;
    private bool isOn = false;
    private FlashlightItemData flashlightData;
    private InventorySlotsUI slot;

    private float flickerTimer = 0f;
    private GameObject SpotLightObj;
    private Light flashlightLight;

    private bool initialized = false;

    private float maxIntensity;
    private float minIntensity = 0.3f;

    private float flickerSpeed;
    private void Start()
    {
        SpotLightObj = transform.Find("Spot Light").gameObject;

        if(SpotLightObj != null)
        {
            SpotLightObj.SetActive(false);
            flashlightLight = SpotLightObj.GetComponent<Light>();

            maxIntensity = flashlightLight.intensity;
        }
    }


    public void OnUse(InventorySlotsUI slotUI)
    {

        if (!initialized)
        {
            OnEquip(slotUI);
        }
        else
        {

            ToggleFlashlight();
        }
    }

    void ToggleFlashlight()
    {
        if (SpotLightObj == null) return;
        isOn = !isOn;
        SpotLightObj.SetActive(isOn);
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
                currentBattery = flashlight.maxBattery;
                initialized = true;
            }
            if (currentBattery > 0) 
            {

                ToggleFlashlight();
            }

            slot.UpdateUsage(currentBattery);
        }
    }
    public void OnUnequip(InventorySlotsUI slotUI)
    {
        isOn = false;
        SpotLightObj.SetActive(isOn);
    }

    private void Update()
    {

       
        if ( flashlightData == null || !initialized) return;

        if(isOn)
            DrainBattery();


        if (Input.GetMouseButtonUp(0) && currentBattery > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            ToggleFlashlight();
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
        float drainPerSecond = flashlightData.batteryDrainage;
        float deltaBattery = drainPerSecond * Time.deltaTime;
        currentBattery -= drainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0);
        slot.UpdateUsage(Mathf.RoundToInt(currentBattery));


        if (currentBattery <= 0)
        {
            isOn = false;
            SpotLightObj.SetActive(isOn);
        }

        if(currentBattery / flashlightData.maxBattery <= flashlightData.flickerThreshhold && isOn)
        {
            FlickerLight();

        }
        else
        {
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

}
