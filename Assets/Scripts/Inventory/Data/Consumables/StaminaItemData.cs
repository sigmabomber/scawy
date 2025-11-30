using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Item/Consumables/Stamina")]
public class StaminaItemData : ItemData
{
    public float amountToReplenish;
    public int maxUses;

    public GameObject staminaItemPrefab;
}
