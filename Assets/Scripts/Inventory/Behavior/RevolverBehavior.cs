using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RevolverBehavior : MonoBehaviour, IItemUsable
{
    // Animator hashes
    private int StartReloadingHash;
    private int EndReloadingHash;
    private int ShootHash;

    [Header("Components")]
    public Animator animator;

    [Header("Gun Data")]
    public int maxAmmo = 6;
    public int currentAmmoCount;
    public int ammoPerShot = 1;
    public float reloadTimePerBullet = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tinnitusAudioSource; // Separate audio source for tinnitus

    public AudioClip reloadIntroSound;
    public AudioClip reloadOutroSound;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip tinnitusSound; // Tinnitus sound effect

    [Header("Tinnitus Settings")]
    [Tooltip("How long the tinnitus effect lasts after shooting")]
    public float tinnitusDuration = 2f;
    [Tooltip("Fade in time for tinnitus effect")]
    public float tinnitusFadeInTime = 0.2f;
    [Tooltip("Fade out time for tinnitus effect")]
    public float tinnitusFadeOutTime = 0.5f;
    [Tooltip("Volume of the tinnitus sound (0-1)")]
    [Range(0f, 1f)] public float tinnitusVolume = 0.3f;
    [Tooltip("How much to dampen other sounds during tinnitus (0-1)")]
    [Range(0f, 1f)] public float soundDampening = 0.3f;
    [Tooltip("Low-pass filter cutoff frequency during tinnitus (Hz)")]
    public float lowPassCutoff = 1500f;
    [Tooltip("How much to reduce pitch of other sounds during tinnitus")]
    [Range(0f, 1f)] public float pitchReduction = 0.2f;
    [Tooltip("Curve for fade in/out effect")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Recoil Settings")]
    public CameraRecoil cameraRecoil;
    [Tooltip("Camera rotation kick (X = up/down, Y = left/right)")]
    public Vector3 recoilRotation = new Vector3(-4f, 0f, 0f);
    [Tooltip("Camera position kickback (Z = backward)")]
    public Vector3 recoilPosition = new Vector3(0f, 0f, -0.15f);
    [Tooltip("Random variation in recoil")]
    public float recoilVariation = 0.5f;

    [Header("State")]
    public bool isReloading = false;
    public bool isShooting = false;
    private bool hasTinnitus = false;
    private bool initialized = false;

    private InventorySlotsUI equippedSlot;

    // Safety timeout for shooting
    private float shootingTimeout = 1f;

    // Original audio settings
    private float originalAudioSourceVolume;
    private float originalAudioSourcePitch;
    private AudioLowPassFilter lowPassFilter;

    // Tinnitus fade coroutine tracking
    private float tinnitusTimer = 0f;
    private float tinnitusFadeProgress = 0f;
    private bool isFadingIn = false;
    private bool isFadingOut = false;

    private void Awake()
    {
        // Cache animator hashes
        StartReloadingHash = Animator.StringToHash("StartReloading");
        EndReloadingHash = Animator.StringToHash("EndReload");
        ShootHash = Animator.StringToHash("Shoot");
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Create tinnitus audio source if not assigned
        if (tinnitusAudioSource == null)
        {
            tinnitusAudioSource = gameObject.AddComponent<AudioSource>();
            tinnitusAudioSource.spatialBlend = 0f; // Make it 2D
            tinnitusAudioSource.loop = true;
            tinnitusAudioSource.volume = 0f; // Start at 0
        }

        // Add low-pass filter for distant sound effect
        if (audioSource != null)
        {
            lowPassFilter = audioSource.GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null)
            {
                lowPassFilter = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPassFilter.enabled = false;

            // Store original audio settings
            originalAudioSourceVolume = audioSource.volume;
            originalAudioSourcePitch = audioSource.pitch;
        }

        // Try to find CameraRecoil if not assigned
        if (cameraRecoil == null)
        {
            cameraRecoil = Camera.main?.GetComponent<CameraRecoil>();
            if (cameraRecoil == null)
            {
                Debug.LogWarning("CameraRecoil not found! Please add CameraRecoil script to your main camera.");
            }
        }
    }

    private void Update()
    {
        if (equippedSlot == null) return;

        HandleReloadInput();
        HandleShootInput();
        UpdateTinnitusFade();
    }

    #region Input Handling

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoCount < maxAmmo)
        {
            StartReload();
        }
    }

    private void HandleShootInput()
    {
        if (Input.GetMouseButtonDown(0) && !isShooting)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (isReloading)
            {
                CancelReload();
                return;
            }

            TryShoot();
        }
    }

    #endregion

    #region Equip/Unequip

    public void OnEquip(InventorySlotsUI slotUI)
    {
        equippedSlot = slotUI;

        if (equippedSlot.itemData is GunItemData data)
        {
            maxAmmo = data.maxAmmo;
            ammoPerShot = data.ammoPerShot;
            reloadTimePerBullet = data.reloadTime;

            if (!initialized)
            {
                currentAmmoCount = maxAmmo;
                initialized = true;
            }

            gameObject.SetActive(true);
        }
    }

    public void OnUse(InventorySlotsUI slotUI) { }

    public void OnUnequip(InventorySlotsUI slotUI)
    {
        if (equippedSlot == slotUI)
        {
            isReloading = false;
            isShooting = false;
            StopTinnitus(); // Stop tinnitus when unequipping
            CancelInvoke();
            gameObject.SetActive(false);
            equippedSlot = null;
        }
    }

    #endregion

    #region Shooting Logic

    private void TryShoot()
    {
        if (isShooting || isReloading) return;

        if (currentAmmoCount < ammoPerShot)
        {
            StartReload();
            return;
        }

        isShooting = true;
        currentAmmoCount -= ammoPerShot;

        if (animator != null)
            animator.SetTrigger(ShootHash);

        PlaySound(shootSound);


        CancelInvoke(nameof(ResetShootingFlags));
        Invoke(nameof(ResetShootingFlags), shootingTimeout);
    }

    public void OnShootComplete()
    {
        ResetShootingFlags();
    }

    private void ResetShootingFlags()
    {
        isShooting = false;
        CancelInvoke(nameof(ResetShootingFlags));
    }

    #endregion

    #region Tinnitus Effect

    private IEnumerator StartTinnitus()
    {
        yield return new WaitForSeconds(0.2f);

        if (hasTinnitus)
        {
            // Reset the timer to extend the duration
            tinnitusTimer = 0f;

            // Cancel any existing fade out and schedule a new one
            CancelInvoke(nameof(StartFadeOut));
            Invoke(nameof(StartFadeOut), tinnitusDuration - tinnitusFadeOutTime);

            yield break;
        }

        // First time - start fresh
        hasTinnitus = true;
        tinnitusTimer = 0f;

        // Start fade in
        StartFadeIn();

        // Schedule fade out
        CancelInvoke(nameof(StartFadeOut));
        Invoke(nameof(StartFadeOut), tinnitusDuration - tinnitusFadeOutTime);
    }

    private void StartFadeIn()
    {
        isFadingIn = true;
        isFadingOut = false;
        tinnitusFadeProgress = 0f;

        // Start tinnitus sound if not already playing
        if (tinnitusAudioSource != null && tinnitusSound != null && !tinnitusAudioSource.isPlaying)
        {
            tinnitusAudioSource.clip = tinnitusSound;
            tinnitusAudioSource.volume = 0f;
            tinnitusAudioSource.Play();
        }

        // Enable low-pass filter for distant sound effect
        if (lowPassFilter != null)
        {
            lowPassFilter.enabled = true;
            lowPassFilter.cutoffFrequency = 22000f; // Start at normal
        }
    }

    private void StartFadeOut()
    {
        isFadingIn = false;
        isFadingOut = true;
        tinnitusFadeProgress = 0f;
    }

    private void UpdateTinnitusFade()
    {
        if (!hasTinnitus) return;

        tinnitusTimer += Time.deltaTime;

        // Handle fade in
        if (isFadingIn)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeInTime;
            float fadeAmount = fadeCurve.Evaluate(Mathf.Clamp01(tinnitusFadeProgress));

            // Fade in tinnitus volume
            if (tinnitusAudioSource != null)
            {
                tinnitusAudioSource.volume = Mathf.Lerp(0f, tinnitusVolume, fadeAmount);
            }

            // Apply sound dampening effects
            ApplySoundEffects(fadeAmount);

            // Check if fade in is complete
            if (tinnitusFadeProgress >= 1f)
            {
                isFadingIn = false;
                tinnitusAudioSource.volume = tinnitusVolume;
                ApplySoundEffects(1f); // Apply full effect
            }
        }

        if (isFadingOut)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeOutTime;
            float fadeAmount = Mathf.Clamp01(tinnitusFadeProgress);

            // Fade out tinnitus volume
            if (tinnitusAudioSource != null)
            {
                tinnitusAudioSource.volume = Mathf.Lerp(tinnitusVolume, 0f, fadeAmount);
            }

            // Remove sound dampening effects
            RemoveSoundEffects(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
            {
                StopTinnitus();
            }
        }

        // Safety check - if tinnitus duration exceeded, start fade out
        if (tinnitusTimer >= tinnitusDuration && !isFadingOut)
        {
            StartFadeOut();
        }
    }

    private void ApplySoundEffects(float progress)
    {
        if (audioSource != null)
        {
            // Reduce volume
            audioSource.volume = Mathf.Lerp(originalAudioSourceVolume, originalAudioSourceVolume * soundDampening, progress);

            // Slightly lower pitch for muffled/distant effect
            audioSource.pitch = Mathf.Lerp(originalAudioSourcePitch, originalAudioSourcePitch * (1f - pitchReduction), progress);
        }

        // Apply low-pass filter (makes sounds feel more distant)
        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, lowPassCutoff, progress);
        }
    }

    private void RemoveSoundEffects(float progress)
    {
        if (audioSource != null)
        {
            // progress goes from 0 to 1, so we interpolate from dampened to original
            audioSource.volume = Mathf.Lerp(originalAudioSourceVolume * soundDampening, originalAudioSourceVolume, progress);
            audioSource.pitch = Mathf.Lerp(originalAudioSourcePitch * (1f - pitchReduction), originalAudioSourcePitch, progress);
        }

        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassCutoff, 22000f, progress);
        }
    }

    private void StopTinnitus()
    {
        if (!hasTinnitus) return;

        hasTinnitus = false;
        isFadingIn = false;
        isFadingOut = false;
        tinnitusTimer = 0f;

        // Stop tinnitus sound FIRST, before restoring audio
        if (tinnitusAudioSource != null && tinnitusAudioSource.isPlaying)
        {
            tinnitusAudioSource.Stop();
            tinnitusAudioSource.volume = 0f;
        }

        // THEN restore original audio settings
        if (audioSource != null)
        {
            audioSource.volume = originalAudioSourceVolume;
            audioSource.pitch = originalAudioSourcePitch;
        }

        // Disable low-pass filter
        if (lowPassFilter != null)
        {
            lowPassFilter.enabled = false;
            lowPassFilter.cutoffFrequency = 22000f; // Reset to default
        }

        CancelInvoke(nameof(StartFadeOut));
    }
    #endregion

    #region Reload Logic

    private void StartReload()
    {
        if (isReloading || currentAmmoCount >= maxAmmo) return;

        isReloading = true;

        if (animator != null)
            animator.SetTrigger(StartReloadingHash);
    }

    private void CancelReload()
    {
        if (!isReloading) return;

        isReloading = false;

        if (animator != null)
            animator.SetTrigger(EndReloadingHash);
    }

    public void StartReloadingFinished()
    {
        if (!isReloading) return;
    }

    public void ApplyRecoil()
    {
        if (cameraRecoil != null)
        {
            float randomX = Random.Range(-recoilVariation, recoilVariation);
            float randomY = Random.Range(-recoilVariation * 0.5f, recoilVariation * 0.5f);

            Vector3 finalRotationRecoil = new Vector3(
                recoilRotation.x + randomX,
                recoilRotation.y + randomY,
                recoilRotation.z
            );

            Vector3 finalPositionRecoil = new Vector3(
                recoilPosition.x,
                recoilPosition.y,
                recoilPosition.z + Random.Range(-0.02f, 0.02f)
            );

            cameraRecoil.ApplyRecoil(finalRotationRecoil, finalPositionRecoil);
        }

        // Start tinnitus effect
        StartCoroutine(StartTinnitus());
    }

    public void AddOneBullet()
    {
        if (!isReloading) return;

        if (currentAmmoCount < maxAmmo)
        {
            currentAmmoCount++;
        }

        if (currentAmmoCount >= maxAmmo)
        {
            FinishReload();
        }
    }

    public void ReloadFinished()
    {
        animator.SetTrigger(EndReloadingHash);
        FinishReload();
    }

    private void FinishReload()
    {
        isReloading = false;

        if (animator != null)
            animator.SetTrigger(EndReloadingHash);
    }

    public void PlayReloadSound()
    {
        PlaySound(reloadSound);
    }

    public void PlayReloadIntroSound()
    {
        PlaySound(reloadIntroSound);
    }

    public void PlayReloadOutroSound()
    {
        PlaySound(reloadOutroSound);
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public Getters

    public int GetCurrentAmmo() => currentAmmoCount;
    public int GetMaxAmmo() => maxAmmo;
    public float GetAmmoPercent() => maxAmmo > 0 ? (float)currentAmmoCount / maxAmmo : 0f;
    public bool IsReloading() => isReloading;
    public bool IsShooting() => isShooting;
    public bool HasTinnitus() => hasTinnitus;
    public bool CanShoot() => !isShooting && !isReloading && currentAmmoCount >= ammoPerShot;

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (equippedSlot == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUILayout.BeginVertical();
        GUILayout.Label($"Ammo: {currentAmmoCount}/{maxAmmo}", style);
        GUILayout.Label($"Reloading (R): {isReloading}", style);
        GUILayout.Label($"Shooting (M1): {isShooting}", style);
        GUILayout.Label($"Tinnitus: {hasTinnitus}", style);
        GUILayout.Label($"Fade In: {isFadingIn}", style);
        GUILayout.Label($"Fade Out: {isFadingOut}", style);
        GUILayout.Label($"Tinnitus Timer: {tinnitusTimer:F2}", style);

        GUILayout.EndVertical();
    }

    #endregion
}