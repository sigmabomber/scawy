using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private int StartInspectHash;
    private int EndInspectHash;
    private int IsReloadingHash;

    [Header("Components")]
    public Animator animator;
    public TMP_Text ammoText;
    public AnimationClip reloadClip;

    [Header("Gun Data")]
    public int maxAmmo = 6;
    public int currentAmmoCount;
    public int ammoPerShot = 1;
    public float reloadTimePerBullet = 0.5f;

    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxRaycastDistance = 100f;
    public LayerMask raycastMask = ~0;

    [Header("Inspect Settings")]
    public float holdUntilInspect = 1f;
    public float inspectDuration = 2f;
    private float currentTimer = 0f;

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
    [SerializeField] private Transform aimReticle;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tinnitusAudioSource;
    public AudioClip reloadIntroSound;
    public AudioClip reloadOutroSound;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip tinnitusSound;

    [Header("Tinnitus Settings")]
    [Tooltip("How long the tinnitus effect lasts after shooting")]
    public float tinnitusDuration = 2f;
    [Tooltip("Fade in time for tinnitus effect")]
    public float tinnitusFadeInTime = 0.2f;
    [Tooltip("Fade out time for tinnitus effect")]
    public float tinnitusFadeOutTime = 0.5f;
    [Tooltip("Volume of the tinnitus sound (0-1)")]
    [Range(0f, 1f)] public float tinnitusVolume = 0.3f;
    [Tooltip("How much to reduce volume of other sounds during tinnitus (0-1, where 0.3 = 70% volume)")]
    [Range(0f, 1f)] public float volumeReduction = 0.3f;
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
    private bool isInspecting = false;

    private InventorySlotsUI equippedSlot;
    private Transform equipPoint;
    private float shootingTimeout = 1f;

    // Original values
    private float originalAudioSourceVolume;

    // Tinnitus fade tracking
    private float tinnitusTimer = 0f;
    private float tinnitusFadeProgress = 0f;
    private bool isFadingIn = false;
    private bool isFadingOut = false;

    // State tracking
    private ItemStateTracker stateTracker;
    private bool canUpdate = false;

    // Coroutine reference for proper cleanup
    private Coroutine inspectCoroutine;

    private void Awake()
    {
        StartReloadingHash = Animator.StringToHash("StartReloading");
        EndReloadingHash = Animator.StringToHash("EndReload");
        ShootHash = Animator.StringToHash("Shoot");
        StartInspectHash = Animator.StringToHash("Inspect");
        EndInspectHash = Animator.StringToHash("EndInspect");
        IsReloadingHash = Animator.StringToHash("IsReloading");
    }

    private void Start()
    {
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(false);
        }
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

        // Initialize tinnitus audio source properly
        if (tinnitusAudioSource == null)
        {
            tinnitusAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure tinnitus audio source
        tinnitusAudioSource.spatialBlend = 0f;
        tinnitusAudioSource.loop = true;
        tinnitusAudioSource.volume = 0f;
        tinnitusAudioSource.playOnAwake = false;

        // Assign the tinnitus clip if it exists
        if (tinnitusSound != null)
        {
            tinnitusAudioSource.clip = tinnitusSound;
        }
        else
        {
            Debug.LogWarning("Tinnitus sound clip is not assigned!");
        }

        if (audioSource != null)
        {
            originalAudioSourceVolume = audioSource.volume;
        }

        if (cameraRecoil == null)
        {
            cameraRecoil = Camera.main?.GetComponent<CameraRecoil>();
            if (cameraRecoil == null)
            {
                Debug.LogWarning("CameraRecoil not found! Please add CameraRecoil script to your main camera.");
            }
        }

        if (aimReticle == null)
        {
            GameObject temp = GameObject.Find("PlayerHud");
            if (temp != null)
                aimReticle = temp.transform.Find("AimReticle");
        }

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
        // Don't allow aiming during inspect
        if (isInspecting) return;

        if (Input.GetKey(aimKey) && !isReloading)
        {
            isAiming = true;
            if (aimReticle != null)
                aimReticle.gameObject.SetActive(true);

            if (PlayerController.Instance != null && isAiming)
            {
                PlayerController.Instance.walkSpeed = 2f;
                PlayerController.Instance.canSprint = false;
                PlayerController.Instance.mouseSensitivity = .5f;
            }
        }
        else
        {
            isAiming = false;
            if (aimReticle != null)
                aimReticle.gameObject.SetActive(false);

            if (PlayerController.Instance != null && !isAiming)
            {
                PlayerController.Instance.walkSpeed = 5f;
                PlayerController.Instance.mouseSensitivity = 2f;
                PlayerController.Instance.canSprint = true;
            }
        }
    }

    private void UpdateAiming()
    {
        if (equipPoint == null || playerCamera == null) return;

        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimSpeed * Time.deltaTime);

        Vector3 targetPosition = isAiming ? aimPosition : normalPosition;
        Quaternion targetRotation = isAiming ? aimRotation : normalRotation;
        equipPoint.localPosition = Vector3.Lerp(equipPoint.localPosition, targetPosition, aimSpeed * Time.deltaTime);
        equipPoint.localRotation = Quaternion.Lerp(equipPoint.localRotation, targetRotation, aimSpeed * Time.deltaTime);
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

            // Cancel inspect if running
            CancelInspect();

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

            CancelInspect();

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

            CancelInspect();

            isShooting = false;
            isAiming = false;
            CancelInvoke();
        }
    }

    public void OnPickedUp()
    {
    }

    public void OnDroppedInWorld()
    {

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

            CancelInspect();

            isShooting = false;
            isAiming = false;
        }
    }

    #endregion

    #region Input Handling

    private void HandleReloadInput()
    {
        // Don't allow reload input during inspect or while already reloading or shooting
        if (isInspecting || isReloading || isShooting) return;

        // Hold down R to inspect
        if (Input.GetKey(KeyCode.R))
        {
            currentTimer += Time.deltaTime;

            if (currentTimer >= holdUntilInspect)
            {
                StartInspect();
                currentTimer = 0f;
            }
        }

        // Release R before inspect threshold = reload
        if (Input.GetKeyUp(KeyCode.R))
        {
            if (currentTimer < holdUntilInspect && currentAmmoCount < maxAmmo)
            {
                StartReload();
            }

            currentTimer = 0f;
        }
    }

    private void StartInspect()
    {
        if (isInspecting || isReloading || isShooting) return;

        inspectCoroutine = StartCoroutine(InspectCoroutine());
    }

    private IEnumerator InspectCoroutine()
    {
        currentTimer = 0f;
        isInspecting = true;

        // Exit aiming mode when inspecting
        isAiming = false;
        if (aimReticle != null)
        {
            aimReticle.gameObject.SetActive(false);
        }

        if (ammoText != null)
        {
            ammoText.text = currentAmmoCount.ToString();
            ammoText.gameObject.SetActive(true);
        }

        if (animator != null)
            animator.SetTrigger(StartInspectHash);

        yield return new WaitForSeconds(inspectDuration);

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);

        isInspecting = false;

        if (animator != null)
            animator.SetTrigger(EndInspectHash);

        inspectCoroutine = null;
    }

    private void CancelInspect()
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
            animator.SetTrigger(EndInspectHash);
    }

    private void HandleShootInput()
    {
        // Don't allow shooting during inspect or reload
        if (isInspecting || isReloading) return;

        if (Input.GetMouseButtonDown(0) && !isShooting)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

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
            CancelInspect();
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

        // Strict checks to prevent shooting during invalid states
        if (isShooting || isReloading || isInspecting) return;

        if (currentAmmoCount < ammoPerShot)
        {
            // Don't start reload if already reloading or shooting
            if (!isReloading && !isShooting)
            {
                StartReload();
            }
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

        if (tinnitusAudioSource != null && tinnitusSound != null)
        {
            // Make sure the clip is assigned
            if (tinnitusAudioSource.clip == null)
            {
                tinnitusAudioSource.clip = tinnitusSound;
            }

            if (!tinnitusAudioSource.isPlaying)
            {
                tinnitusAudioSource.volume = 0f;
                tinnitusAudioSource.Play();
            }
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

            ApplyVolumeReduction(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
            {
                isFadingIn = false;
                if (tinnitusAudioSource != null)
                    tinnitusAudioSource.volume = tinnitusVolume;
                ApplyVolumeReduction(1f);
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

            RestoreVolume(fadeAmount);

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

    private void ApplyVolumeReduction(float progress)
    {
        if (audioSource != null)
        {
            // Calculate target volume: original volume * (1 - volumeReduction)
            // For example, if volumeReduction is 0.3, sound will be at 70% of original
            float targetVolume = originalAudioSourceVolume * (1f - volumeReduction);
            audioSource.volume = Mathf.Lerp(originalAudioSourceVolume, targetVolume, progress);
        }
    }

    private void RestoreVolume(float progress)
    {
        if (audioSource != null)
        {
            float reducedVolume = originalAudioSourceVolume * (1f - volumeReduction);
            audioSource.volume = Mathf.Lerp(reducedVolume, originalAudioSourceVolume, progress);
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
        }

        CancelInvoke(nameof(StartFadeOut));
    }
    #endregion

    #region Reload Logic

    private void StartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        // Prevent reload during other actions or if already at max ammo
        if (isReloading || isShooting || isInspecting || currentAmmoCount >= maxAmmo) return;

        isReloading = true;

        // Set animator speed based on reload time
        if (animator != null)
        {
            // Calculate how many bullets need to be loaded
            int bulletsToLoad = maxAmmo - currentAmmoCount;

   
            float animLength = reloadClip != null ? reloadClip.length : 1f;

            
            if (animLength > 0f)
            {
                float desiredDuration = bulletsToLoad * reloadTimePerBullet;
                float speedMultiplier = animLength / desiredDuration;
                animator.speed = speedMultiplier;
            }

            animator.SetBool(IsReloadingHash, true);
            animator.SetTrigger(StartReloadingHash);
        }
    }

    private void CancelReload()
    {
        if (!isReloading) return;

        isReloading = false;
        if (animator != null)
        {
            // Reset animator speed to normal
            animator.speed = 1f;

            animator.SetBool(IsReloadingHash, false);
            animator.SetTrigger(EndReloadingHash);
        }
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
        if (animator != null)
            animator.SetTrigger(EndReloadingHash);
        FinishReload();
    }

    private void FinishReload()
    {
        isReloading = false;
        if (animator != null)
        {
            // Reset animator speed to normal
            animator.speed = 1f;

            animator.SetBool(IsReloadingHash, false);
            animator.SetTrigger(EndReloadingHash);
        }
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
    public bool IsInspecting() => isInspecting;
    public bool CanShoot() => !isShooting && !isReloading && !isInspecting && currentAmmoCount >= ammoPerShot;

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
        GUILayout.Label($"Inspecting: {isInspecting}", style);
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