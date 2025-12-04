using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IRevolverHit
{
    void OnRevolverHit();
}

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

    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxRaycastDistance = 100f;
    public LayerMask raycastMask = ~0;

    [Header("Aiming Settings")]
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimSpeed = 8f;
    [SerializeField] private Vector3 aimPosition = new Vector3(0f, -0.15f, 0.3f);
    [SerializeField] private Quaternion aimRotation = new Quaternion(0.795179367f, 180, 6.1392501e-07f, 0);
    [SerializeField] private Quaternion normalRotation;
    [SerializeField] private Vector3 normalPosition = new Vector3(0.5f, -0.3f, 0.5f);
    [SerializeField] private float aimMovementSpeedMultiplier = 0.5f;
    [SerializeField] private float aimSensitivityMultiplier = 0.6f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tinnitusAudioSource;
    public AudioClip reloadIntroSound;
    public AudioClip reloadOutroSound;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip tinnitusSound;
    public AudioClip headshotSound;

    [Header("Tinnitus Settings")]
    [Tooltip("How long the tinnitus effect lasts after shooting")]
    public float tinnitusDuration = 2f;
    [Tooltip("Fade in time for tinnitus effect")]
    public float tinnitusFadeInTime = 0.2f;
    [Tooltip("Fade out time for tinnitus effect")]
    public float tinnitusFadeOutTime = 0.5f;
    [Tooltip("Volume of the tinnitus sound (0-1)")]
    [Range(0f, 1f)] public float tinnitusVolume = 0.3f;
    public float soundDampening = 0.3f;
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
    [Tooltip("Reduced recoil when aiming")]
    public float aimRecoilMultiplier = 0.6f;

    [Header("State")]
    public bool isReloading = false;
    public bool isShooting = false;
    private bool isAiming = false;
    private bool hasTinnitus = false;
    private bool initialized = false;

    private InventorySlotsUI equippedSlot;
    private Transform equipPoint;
    private float shootingTimeout = 1f;

    // Original values
    private float originalAudioSourceVolume;
    private float originalAudioSourcePitch;
    private AudioLowPassFilter lowPassFilter;

    // Tinnitus fade tracking
    private float tinnitusTimer = 0f;
    private float tinnitusFadeProgress = 0f;
    private bool isFadingIn = false;
    private bool isFadingOut = false;

    // State tracking
    private ItemStateTracker stateTracker;
    private bool canUpdate = false;

    private void Awake()
    {
        StartReloadingHash = Animator.StringToHash("StartReloading");
        EndReloadingHash = Animator.StringToHash("EndReload");
        ShootHash = Animator.StringToHash("Shoot");
    }

    private void Start()
    {
        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
        {
            stateTracker = gameObject.AddComponent<ItemStateTracker>();
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        // Store normal FOV
        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
        }

        if (tinnitusAudioSource == null)
        {
            tinnitusAudioSource = gameObject.AddComponent<AudioSource>();
            tinnitusAudioSource.spatialBlend = 0f;
            tinnitusAudioSource.loop = true;
            tinnitusAudioSource.volume = 0f;
        }

        if (audioSource != null)
        {
            lowPassFilter = audioSource.GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null)
            {
                lowPassFilter = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPassFilter.enabled = false;

            originalAudioSourceVolume = audioSource.volume;
            originalAudioSourcePitch = audioSource.pitch;
        }

        if (cameraRecoil == null)
        {
            cameraRecoil = Camera.main?.GetComponent<CameraRecoil>();
            if (cameraRecoil == null)
            {
                Debug.LogWarning("CameraRecoil not found! Please add CameraRecoil script to your main camera.");
            }
        }

        // Get equip point from EquipmentManager
        if (EquipmentManager.Instance != null)
        {
            equipPoint = EquipmentManager.Instance.GetEquipPoint();
            if (equipPoint != null)
            {
                normalPosition = equipPoint.localPosition;
            }
        }

        UpdateBehaviorBasedOnState();
    }

    private void Update()
    {
        if (!canUpdate || equippedSlot == null) return;

        HandleAimInput();
        HandleReloadInput();
        HandleShootInput();
        UpdateTinnitusFade();
        UpdateAiming();
    }

    #region Aiming

    private void HandleAimInput()
    {
        if (Input.GetKey(aimKey) && !isReloading)
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }
    }

    private void UpdateAiming()
    {
        if (equipPoint == null || playerCamera == null) return;

        // Smoothly transition FOV
        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimSpeed * Time.deltaTime);

        // Smoothly transition weapon position
        Vector3 targetPosition = isAiming ? aimPosition : normalPosition;
        Quaternion targetRotation = isAiming ? aimRotation : normalRotation;
        equipPoint.localPosition = Vector3.Lerp(equipPoint.localPosition, targetPosition, aimSpeed * Time.deltaTime);
        equipPoint.localRotation = Quaternion.Lerp(equipPoint.localRotation, targetRotation, aimSpeed * Time.deltaTime);
      
        
        if (PlayerController.Instance != null && isAiming)
        {
           
        }
    }

    public bool IsAiming() => isAiming;
    public float GetAimMovementMultiplier() => isAiming ? aimMovementSpeedMultiplier : 1f;
    public float GetAimSensitivityMultiplier() => isAiming ? aimSensitivityMultiplier : 1f;

    #endregion

    #region IItemUsable State Tracking Methods

    public void OnItemStateChanged(ItemState previousState, ItemState newState)
    {
        UpdateBehaviorBasedOnState();

        if (newState == ItemState.Equipped)
        {
            canUpdate = true;

            if (isReloading)
            {
                CancelReload();
            }

            if (hasTinnitus)
            {
                StopTinnitus();
            }

            // Reset aiming state
            isAiming = false;
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }

            if (gameObject != null)
                gameObject.SetActive(true);
        }
        else if (newState == ItemState.InWorld)
        {
            canUpdate = false;

            if (isReloading)
            {
                CancelReload();
            }

            if (hasTinnitus)
            {
                StopTinnitus();
            }

            isShooting = false;
            isAiming = false;
            CancelInvoke();

            equippedSlot = null;

            if (gameObject != null)
                gameObject.SetActive(false);
        }
        else if (newState == ItemState.InInventory)
        {
            canUpdate = false;

            if (isReloading)
            {
                CancelReload();
            }

            if (hasTinnitus)
            {
                StopTinnitus();
            }

            isShooting = false;
            isAiming = false;
            CancelInvoke();
        }
    }

    public void OnPickedUp()
    {
        Debug.Log("Revolver picked up");
    }

    public void OnDroppedInWorld()
    {
        Debug.Log("Revolver dropped in world");

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (tinnitusAudioSource != null && tinnitusAudioSource.isPlaying)
        {
            tinnitusAudioSource.Stop();
        }
    }

    private void UpdateBehaviorBasedOnState()
    {
        if (stateTracker == null) return;

        if (stateTracker.IsEquipped)
        {
            canUpdate = true;
        }
        else
        {
            canUpdate = false;

            if (isReloading)
            {
                CancelReload();
            }

            if (hasTinnitus)
            {
                StopTinnitus();
            }

            isShooting = false;
            isAiming = false;
        }
    }

    #endregion

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

            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.Equipped);
            }

            gameObject.SetActive(true);
        }
    }

    public void OnUse(InventorySlotsUI slotUI) { }

    public void OnUnequip(InventorySlotsUI slotUI)
    {
        if (equippedSlot == slotUI)
        {
            if (stateTracker != null)
            {
                stateTracker.SetState(ItemState.InInventory);
            }

            isReloading = false;
            isShooting = false;
            isAiming = false;
            StopTinnitus();
            CancelInvoke();

            // Reset FOV when unequipping
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }

            gameObject.SetActive(false);
            equippedSlot = null;
        }
    }

    #endregion

    #region Shooting Logic

    private void TryShoot()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

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

    private void PerformRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, raycastMask))
        {
            HandleHit(hit);
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        IRevolverHit revolverHit = hit.collider.GetComponent<IRevolverHit>();

        if (revolverHit != null)
        {
            revolverHit.OnRevolverHit();
        }
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
        if (stateTracker != null && !stateTracker.IsEquipped) yield break;

        yield return new WaitForSeconds(0.2f);

        if (hasTinnitus)
        {
            tinnitusTimer = 0f;
            CancelInvoke(nameof(StartFadeOut));
            Invoke(nameof(StartFadeOut), tinnitusDuration - tinnitusFadeOutTime);
            yield break;
        }

        hasTinnitus = true;
        tinnitusTimer = 0f;

        StartFadeIn();

        CancelInvoke(nameof(StartFadeOut));
        Invoke(nameof(StartFadeOut), tinnitusDuration - tinnitusFadeOutTime);
    }

    private void StartFadeIn()
    {
        isFadingIn = true;
        isFadingOut = false;
        tinnitusFadeProgress = 0f;

        if (tinnitusAudioSource != null && tinnitusSound != null && !tinnitusAudioSource.isPlaying)
        {
            tinnitusAudioSource.clip = tinnitusSound;
            tinnitusAudioSource.volume = 0f;
            tinnitusAudioSource.Play();
        }

        if (lowPassFilter != null)
        {
            lowPassFilter.enabled = true;
            lowPassFilter.cutoffFrequency = 22000f;
        }
    }

    private void StartFadeOut()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        isFadingIn = false;
        isFadingOut = true;
        tinnitusFadeProgress = 0f;
    }

    private void UpdateTinnitusFade()
    {
        if (!hasTinnitus) return;

        tinnitusTimer += Time.deltaTime;

        if (isFadingIn)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeInTime;
            float fadeAmount = fadeCurve.Evaluate(Mathf.Clamp01(tinnitusFadeProgress));

            if (tinnitusAudioSource != null)
            {
                tinnitusAudioSource.volume = Mathf.Lerp(0f, tinnitusVolume, fadeAmount);
            }

            ApplySoundEffects(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
            {
                isFadingIn = false;
                tinnitusAudioSource.volume = tinnitusVolume;
                ApplySoundEffects(1f);
            }
        }

        if (isFadingOut)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeOutTime;
            float fadeAmount = Mathf.Clamp01(tinnitusFadeProgress);

            if (tinnitusAudioSource != null)
            {
                tinnitusAudioSource.volume = Mathf.Lerp(tinnitusVolume, 0f, fadeAmount);
            }

            RemoveSoundEffects(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
            {
                StopTinnitus();
            }
        }

        if (tinnitusTimer >= tinnitusDuration && !isFadingOut)
        {
            StartFadeOut();
        }
    }

    private void ApplySoundEffects(float progress)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Lerp(originalAudioSourceVolume, originalAudioSourceVolume * soundDampening, progress);
            audioSource.pitch = Mathf.Lerp(originalAudioSourcePitch, originalAudioSourcePitch * (1f - pitchReduction), progress);
        }

        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, lowPassCutoff, progress);
        }
    }

    private void RemoveSoundEffects(float progress)
    {
        if (audioSource != null)
        {
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

        if (tinnitusAudioSource != null && tinnitusAudioSource.isPlaying)
        {
            tinnitusAudioSource.Stop();
            tinnitusAudioSource.volume = 0f;
        }

        if (audioSource != null)
        {
            audioSource.volume = originalAudioSourceVolume;
            audioSource.pitch = originalAudioSourcePitch;
        }

        if (lowPassFilter != null)
        {
            lowPassFilter.enabled = false;
            lowPassFilter.cutoffFrequency = 22000f;
        }

        CancelInvoke(nameof(StartFadeOut));
    }
    #endregion

    #region Reload Logic

    private void StartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

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
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        PerformRaycast();
        if (cameraRecoil != null)
        {
            // Reduce recoil when aiming
            float recoilMultiplier = isAiming ? aimRecoilMultiplier : 1f;

            float randomX = Random.Range(-recoilVariation, recoilVariation) * recoilMultiplier;
            float randomY = Random.Range(-recoilVariation * 0.5f, recoilVariation * 0.5f) * recoilMultiplier;

            Vector3 finalRotationRecoil = new Vector3(
                (recoilRotation.x + randomX) * recoilMultiplier,
                (recoilRotation.y + randomY) * recoilMultiplier,
                recoilRotation.z * recoilMultiplier
            );

            Vector3 finalPositionRecoil = new Vector3(
                recoilPosition.x * recoilMultiplier,
                recoilPosition.y * recoilMultiplier,
                (recoilPosition.z + Random.Range(-0.02f, 0.02f)) * recoilMultiplier
            );

            cameraRecoil.ApplyRecoil(finalRotationRecoil, finalPositionRecoil);
        }

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
        if (stateTracker != null && !stateTracker.IsEquipped) return;

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
        GUILayout.Label($"Aiming (M2): {isAiming}", style);
        GUILayout.Label($"Tinnitus: {hasTinnitus}", style);

        if (stateTracker != null)
        {
            GUILayout.Label($"State: {stateTracker.CurrentState}", style);
            GUILayout.Label($"Can Update: {canUpdate}", style);
        }

        GUILayout.EndVertical();
    }

    #endregion
}