using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Item/Utilities/Flashlight")]
public class FlashlightItemData : ItemData
{
    public float maxBattery;
    public float batteryDrainage; // how much battery is used per second



    public float flickerThreshhold;
    public float flickerSpeed;

    public GameObject flashlightPrefab;

}
