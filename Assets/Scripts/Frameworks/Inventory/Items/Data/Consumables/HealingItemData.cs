using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName ="Item/Consumables/Healing")]
public class HealingItemData : ItemData
{
    public float healing;
    public int maxUses;

    public float timeToUse; 


}
