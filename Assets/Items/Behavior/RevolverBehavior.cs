using UnityEngine;

/// <summary>
/// Revolver-style gun with bullet-by-bullet reload
/// </summary>
public class RevolverBehavior : BaseGunBehavior
{
    protected override void StartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        if (isReloading || isShooting || isInspecting) return;
        if (currentAmmoCount >= gunData.maxAmmo) return;

        isReloading = true;

        if (animator != null)
        {
            animator.SetBool(IsReloadingHash, true);
            animator.SetTrigger(StartReloadingHash);
        }
    }

    protected override void CancelReload()
    {
        if (!isReloading) return;

        isReloading = false;
        isCancelReloading = false;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(IsReloadingHash, false);
            animator.SetTrigger(EndReloadingHash);
        }
    }

    /// <summary>
    /// Called by animation event - adds one bullet at a time
    /// </summary>
    public override void AddOneBullet()
    {
        if (!isReloading) return;

        bool shouldStopReload = false;

        if (isCancelReloading)
        {
            shouldStopReload = true;
            isCancelReloading = false;
        }
        else if (availableAmmoInInventory <= 0)
        {
            shouldStopReload = true;
        }
        else if (currentAmmoCount >= gunData.maxAmmo)
        {
            shouldStopReload = true;
        }

        if (shouldStopReload)
        {
            FinishReload();
            return;
        }

        if (ConsumeAmmoFromInventory(1))
        {
            currentAmmoCount++;
            UpdateAvailableAmmoCount();

            if (currentAmmoCount >= gunData.maxAmmo || availableAmmoInInventory <= 0)
            {
                FinishReload();
            }
        }
        else
        {
            FinishReload();
        }
    }

    /// <summary>
    /// Called when reload animation completes
    /// </summary>
    public override void ReloadFinished()
    {
        if (animator != null)
            animator.SetTrigger(EndReloadingHash);

        FinishReload();
    }

   
}