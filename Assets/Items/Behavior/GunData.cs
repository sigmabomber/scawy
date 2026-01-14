using UnityEngine;

public interface IGunHit
{
    void OnGunHit(GunData gunData);
}

[System.Serializable]
public class GunData
{
    [Header("Basic Stats")]
    public string gunName = "Gun";
    public int maxAmmo = 6;
    public int ammoPerShot = 1;
    public float fireRate = 0.5f; // Time between shots
    public float damage = 25f;

    [Header("Reload")]
    public ReloadType reloadType = ReloadType.BulletByBullet;
    public float reloadTime = 2f;
    public bool canCancelReload = true;

    [Header("Recoil")]
    public Vector3 recoilRotation = new Vector3(-4f, 0f, 0f);
    public Vector3 recoilPosition = new Vector3(0f, 0f, -0.15f);
    public float recoilVariation = 0.5f;
    public float aimRecoilMultiplier = 0.6f;

    [Header("Aiming")]
    public float aimFOV = 40f;
    public Vector3 aimPosition = new Vector3(0f, -0.15f, 0.3f);
    public Quaternion aimRotation = Quaternion.identity;
    public float aimMovementSpeedMultiplier = 0.5f;
    public float aimSensitivityMultiplier = 0.6f;

    [Header("Effects")]
    public bool hasTinnitus = true;
    public float tinnitusDuration = 2f;
    public float tinnitusVolume = 0.3f;




}

public enum ReloadType
{
    BulletByBullet,  // Revolver style
    Magazine         // Magazine style
}
