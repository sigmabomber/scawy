using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Item/Consumables/Stamina")]
public class StaminaItemData : ItemData
{
    public float speedDuration;
    public int maxUses;
    public float speedStrength;
    public float duration;

}
