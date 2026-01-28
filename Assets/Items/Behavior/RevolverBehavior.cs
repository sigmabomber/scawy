using System.Collections;
using UnityEngine;

/// <summary>
/// Revolver-style gun with bullet-by-bullet reload
/// </summary>
public class RevolverBehavior : BaseGunBehavior
{
    [Header("Sound Emission for AI")]
    [SerializeField] private float gunshotSoundStrength = 1.0f;
    [SerializeField] private float reloadSoundStrength = 0.3f;
    [SerializeField] private float cockSoundStrength = 0.2f;
    [SerializeField] private float dryFireSoundStrength = 0.1f;
    [SerializeField] private float bulletLoadSoundStrength = 0.15f;

    protected override void InitializeSettings()
    {
        base.InitializeSettings();
        // Initialize any revolver-specific settings here
    }

    protected override void StartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        if (isReloading || isShooting || isInspecting) return;
        if (currentAmmoCount >= gunData.maxAmmo) return;

        isReloading = true;

        // Emit reload sound for AI detection
        EmitAISound("reload", reloadSoundStrength);

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

            // Emit bullet loading sound for AI
            EmitAISound("bullet_load", bulletLoadSoundStrength);

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

    /// <summary>
    /// Override shooting to add AI sound detection
    /// </summary>
    protected override void PerformShoot()
    {
        base.PerformShoot(); // This will trigger animation and recoil

        // Emit gunshot sound for AI detection
        EmitAISound("gunshot", gunshotSoundStrength);
    }

    /// <summary>
    /// Handle dry fire (when gun is empty)
    /// </summary>
    public override void OnShootComplete()
    {
        base.OnShootComplete();

        // If we tried to shoot but had no ammo (dry fire)
        if (currentAmmoCount < gunData.ammoPerShot && !isReloading)
        {
            // Emit dry fire sound
            EmitAISound("dry_fire", dryFireSoundStrength);
        }
    }

    /// <summary>
    /// Called by animation events for cocking sound
    /// </summary>
    public void PlayCockingSound()
    {
        // Emit cocking sound for AI
        EmitAISound("cock", cockSoundStrength);
    }

    /// <summary>
    /// Helper method to emit sounds for AI detection
    /// </summary>
    private void EmitAISound(string soundTag, float strength)
    {
        // Check if we're equipped and in a valid state to make noise
        if (stateTracker == null || !stateTracker.IsEquipped) return;
        if (isInspecting) return; // Don't make noise while inspecting

        // Use SoundManager if available
        if (SoundManager.Instance != null)
        {
            // Determine if this is a player gun
            bool isPlayerGun = false;

            // Check if the parent or holder is the player
            if (transform.root.CompareTag("Player") ||
                (stateTracker != null && stateTracker.transform.CompareTag("Player")))
            {
                isPlayerGun = true;
            }

            if (isPlayerGun)
            {
                // Emit as player sound
                SoundManager.Instance.EmitPlayerSound(transform.position, strength, soundTag);
            }
            else
            {
                // Emit as regular sound
                SoundManager.Instance.EmitSound(gameObject, transform.position, strength, soundTag, isPlayerGun);
            }
        }
        else
        {
            // Fallback: Use the existing event system if SoundManager isn't available
            Debug.LogWarning("SoundManager not found! AI won't hear gun sounds.");
        }
    }

    /// <summary>
    /// Override to handle gun-specific unequip behavior
    /// </summary>
    public override void OnUnequip(InventorySlotsUI slotUI)
    {
        base.OnUnequip(slotUI);

        // Stop any ongoing AI sounds
        // (Add any revolver-specific cleanup here)
    }

    /// <summary>
    /// Animation event method for gunshot sound (called by animation)
    /// </summary>
    public void AnimationEvent_Gunshot()
    {
        // This is called by the shooting animation
        // You can use this if you want the sound to sync exactly with the animation
        EmitAISound("gunshot", gunshotSoundStrength);
    }

    /// <summary>
    /// Animation event method for reload sound (called by animation)
    /// </summary>
    public void AnimationEvent_Reload()
    {
        EmitAISound("reload", reloadSoundStrength);
    }

    /// <summary>
    /// Animation event method for cocking sound (called by animation)
    /// </summary>
    public void AnimationEvent_Cock()
    {
        EmitAISound("cock", cockSoundStrength);
    }

    /// <summary>
    /// Animation event method for bullet load sound (called by animation)
    /// </summary>
    public void AnimationEvent_BulletLoad()
    {
        EmitAISound("bullet_load", bulletLoadSoundStrength);
    }
}