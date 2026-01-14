using System.Collections;
using UnityEngine;

/// <summary>
/// Magazine-based gun (pistol, rifle, etc.) with full magazine reload
/// </summary>
public class GunBehavior : BaseGunBehavior
{
    protected int HasBullets;
    
    protected override void Awake()
    {
        base.Awake();
        HasBullets = Animator.StringToHash("HasBullets");
    }

    protected override void Update()
    {
        base.Update();

        float hasBullets = currentAmmoCount > 0 ? 0 : 1;

        animator.SetFloat(HasBullets, hasBullets);
    }


    protected override void StartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        if (isReloading || isShooting || isInspecting) return;
        if (currentAmmoCount >= gunData.maxAmmo) return;

        StartCoroutine(MagazineReloadCoroutine());
    }

    protected override void CancelReload()
    {
        if (!isReloading) return;

        StopAllCoroutines();
        isReloading = false;
        isCancelReloading = false;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(IsReloadingHash, false);
            animator.SetTrigger(EndReloadingHash);
        }
    }

    private IEnumerator MagazineReloadCoroutine()
    {
        isReloading = true;

        if (animator != null)
        {
            animator.SetBool(IsReloadingHash, true);
            animator.SetTrigger(StartReloadingHash);
        }

        // Wait for reload animation/time
        yield return new WaitForSeconds(gunData.reloadTime);

        // Check if we should cancel
        if (isCancelReloading)
        {
            CancelReload();
            yield break;
        }

        // Calculate how many bullets to add
        int bulletsNeeded = gunData.maxAmmo - currentAmmoCount;
        int bulletsToAdd = Mathf.Min(bulletsNeeded, availableAmmoInInventory);

        if (ConsumeAmmoFromInventory(bulletsToAdd))
        {
            currentAmmoCount += bulletsToAdd;
            UpdateAvailableAmmoCount();
        }

        FinishReload();
    }

    protected override void FinishReload()
    {
        base.FinishReload();

       
    }

    protected override void PerformShoot()
    {
      

        base.PerformShoot();
    }

    protected override void ResetShootingFlags()
    {
        base.ResetShootingFlags();

       
    }

    protected override IEnumerator InspectCoroutine()
    {
        currentTimer = 0f;
        isInspecting = true;

        // Exit aiming mode when inspecting
        isAiming = false;
        if (aimReticle != null)
        {
            aimReticle.gameObject.SetActive(false);
        }

        if (animator != null)
        {
            animator.SetTrigger(StartInspectHash);
        }

        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmoCount}/{availableAmmoInInventory}";
            ammoText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(inspectDuration);

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);

        isInspecting = false;

        if (animator != null)
        {
            animator.SetTrigger(EndInspectHash);
        }

        inspectCoroutine = null;
    }

    protected override void CancelInspect()
    {
        if (!isInspecting) return;

        if (inspectCoroutine != null)
        {
            StopCoroutine(inspectCoroutine);
            inspectCoroutine = null;
        }

        isInspecting = false;
        currentTimer = 0f;

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);

        if (animator != null)
        {
            animator.SetTrigger(EndInspectHash);
        }
    }

    /// <summary>
    /// Not used for magazine reload, but required by base class
    /// </summary>
    public override void AddOneBullet()
    {
        // Magazine guns don't reload bullet by bullet
        // This is here for interface compliance but does nothing
    }

    /// <summary>
    /// Called when reload completes
    /// </summary>
    public override void ReloadFinished()
    {
        FinishReload();
    }

    #region Debug
    private void OnGUI()
    {
        if (equippedSlot == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUILayout.BeginVertical();
        GUILayout.Label($"Gun: {gunData.gunName}", style);
        GUILayout.Label($"Ammo: {currentAmmoCount}/{gunData.maxAmmo}", style);
        GUILayout.Label($"Inventory Ammo: {availableAmmoInInventory}", style);
        if (firstAmmoTypeFound != null)
            GUILayout.Label($"Ammo Type: {firstAmmoTypeFound.itemName}", style);
        GUILayout.Label($"Reloading: {isReloading}", style);
        GUILayout.Label($"Shooting: {isShooting}", style);
        GUILayout.Label($"Aiming: {isAiming}", style);
        GUILayout.Label($"Inspecting: {isInspecting}", style);
        GUILayout.Label($"Reload Type: Magazine", style);
        GUILayout.Label($"Fire Rate: {gunData.fireRate}s", style);

        if (stateTracker != null)
        {
            GUILayout.Label($"State: {stateTracker.CurrentState}", style);
            GUILayout.Label($"Can Update: {canUpdate}", style);
        }

        GUILayout.EndVertical();
    }
    #endregion
}