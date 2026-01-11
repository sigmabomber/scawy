using UnityEngine;

[CreateAssetMenu(menuName = "Item/Weapons/Gun")]
public class GunItemData : ItemData
{
    [Header("Basic Stats")]
    public int maxAmmo;
    public int ammoPerShot;
    public float fireRate;
    public float reloadTime;
    public float damage;

    [Header("Reload Type")]
    public ReloadType reloadType = ReloadType.Magazine;

    [Header("Recoil")]
    public Vector3 recoilRotation = new Vector3(-4f, 0f, 0f);
    public Vector3 recoilPosition = new Vector3(0f, 0f, -0.15f);
    public float recoilVariation = 0.5f;
}