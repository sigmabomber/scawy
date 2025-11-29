using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Item/Weapons/Melee")]
public class MeleeItemData : ItemData
{
    public int damage;
    public float cooldown;
    public float range;

    public GameObject meleePrefab;
}
