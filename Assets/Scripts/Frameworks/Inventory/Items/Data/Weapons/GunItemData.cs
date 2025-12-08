using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Item/Weapons/Gun")]
public class GunItemData : ItemData
{

    public int maxAmmo;

    public int ammoPerShot;

    public float fireRate;
    public float reloadTime;

    public GameObject gunPrefab;
}
