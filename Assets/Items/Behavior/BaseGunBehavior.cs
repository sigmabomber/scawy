using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;


public abstract class BaseGunBehavior : InputScript, IItemUsable
{
    #region Animator Hashes
    protected int StartReloadingHash;
    protected int EndReloadingHash;
    protected int ShootHash;
    protected int StartInspectHash;
    protected int EndInspectHash;
    protected int IsReloadingHash;
    #endregion

    #region Components
    [Header("Components")]
    public Animator animator;
    public TMP_Text ammoText;
    protected InventorySystem inventorySystem;
    protected ItemStateTracker stateTracker;
    protected InventorySlotsUI equippedSlot;
    protected Transform equipPoint;
    #endregion

    #region Gun Configuration
    [Header("Gun Configuration")]
    public GunData gunData;

    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxRaycastDistance = 100f;
    public LayerMask raycastMask = ~0;

    [Header("Inspect Settings")]
    public float holdUntilInspect = 1f;
    public float inspectDuration = 2f;

    [Header("Aiming Settings")]
    [SerializeField] protected KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] protected float aimSpeed = 8f;
    [SerializeField] protected float normalFOV = 60f;
    [SerializeField] protected Vector3 normalPosition = new Vector3(0.5f, -0.3f, 0.5f);
    [SerializeField] protected Quaternion normalRotation;
    [SerializeField] protected Transform aimReticle;
    #endregion

    #region Audio
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tinnitusAudioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip reloadIntroSound;
    public AudioClip reloadOutroSound;
    public AudioClip tinnitusSound;
    public AudioClip emptyClickSound;

    [Header("Tinnitus Settings")]
    public float tinnitusFadeInTime = 0.2f;
    public float tinnitusFadeOutTime = 0.5f;
    [Range(0f, 1f)] public float volumeReduction = 0.3f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    #endregion

    #region Recoil
    [Header("Recoil")]
    public CameraRecoil cameraRecoil;
    #endregion

    #region State Variables
    protected int currentAmmoCount;
    protected bool isReloading = false;
    protected bool isShooting = false;
    protected bool isAiming = false;
    protected bool hasTinnitus = false;
    protected bool initialized = false;
    protected bool isInspecting = false;
    protected bool isCancelReloading = false;
    protected bool canUpdate = false;

    protected float currentTimer = 0f;
    protected float originalAudioSourceVolume;
    protected float tinnitusTimer = 0f;
    protected float tinnitusFadeProgress = 0f;
    protected bool isFadingIn = false;
    protected bool isFadingOut = false;
    protected float lastShotTime = 0f;

    protected Coroutine inspectCoroutine;
    #endregion

    #region Ammo Tracking
    protected int availableAmmoInInventory = 0;
    protected AmmoItemData firstAmmoTypeFound = null;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitializeAnimatorHashes();
    }

    protected virtual void Start()
    {
        InitializeComponents();
        InitializeAudio();
        InitializeSettings();
        UpdateBehaviorBasedOnState();
    }

    protected override void HandleInput()
    {
        if (!canUpdate || equippedSlot == null) return;

        HandleAimInput();
        HandleReloadInput();
        HandleShootInput();
        UpdateTinnitusFade();
        UpdateAiming();
    }
    #endregion

    #region Initialization
    protected virtual void InitializeAnimatorHashes()
    {
        StartReloadingHash = Animator.StringToHash("StartReloading");
        EndReloadingHash = Animator.StringToHash("EndReload");
        ShootHash = Animator.StringToHash("Shoot");
        StartInspectHash = Animator.StringToHash("Inspect");
        EndInspectHash = Animator.StringToHash("EndInspect");
        IsReloadingHash = Animator.StringToHash("IsReloading");
    }

    protected virtual void InitializeComponents()
    {
        if (ammoText != null)
            ammoText.gameObject.SetActive(false);

        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
            stateTracker = gameObject.AddComponent<ItemStateTracker>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (inventorySystem == null)
            inventorySystem = FindFirstObjectByType<InventorySystem>();

        if (cameraRecoil == null)
        {
            cameraRecoil = Camera.main?.GetComponent<CameraRecoil>();
            if (cameraRecoil == null)
                Debug.LogWarning("CameraRecoil not found! Add CameraRecoil script to main camera.");
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
                normalPosition = equipPoint.localPosition;
        }
    }

    protected virtual void InitializeAudio()
    {
        if (playerCamera != null)
            normalFOV = playerCamera.fieldOfView;

        if (tinnitusAudioSource == null)
            tinnitusAudioSource = gameObject.AddComponent<AudioSource>();

        tinnitusAudioSource.spatialBlend = 0f;
        tinnitusAudioSource.loop = true;
        tinnitusAudioSource.volume = 0f;
        tinnitusAudioSource.playOnAwake = false;

        if (tinnitusSound != null)
            tinnitusAudioSource.clip = tinnitusSound;

        if (audioSource != null)
            originalAudioSourceVolume = audioSource.volume;
    }

    protected virtual void InitializeSettings()
    {
        // Override in derived classes for specific initialization
    }
    #endregion

    #region Input Handling
    protected virtual void HandleAimInput()
    {
        if (isInspecting) return;

        if (Input.GetKey(aimKey) && !isReloading)
        {
            isAiming = true;
            if (aimReticle != null)
                aimReticle.gameObject.SetActive(true);

            if (PlayerController.Instance != null)
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

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.walkSpeed = 5f;
                PlayerController.Instance.mouseSensitivity = 2f;
                PlayerController.Instance.canSprint = true;
            }
        }
    }

    protected virtual void HandleReloadInput()
    {
        if (isInspecting || isReloading || isShooting) return;

        if (Input.GetKey(KeyCode.R))
        {
            currentTimer += Time.deltaTime;
            if (currentTimer >= holdUntilInspect)
            {
                StartInspect();
                currentTimer = 0f;
            }
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            if (currentTimer < holdUntilInspect)
                TryStartReload();
            currentTimer = 0f;
        }
    }

    protected virtual void HandleShootInput()
    {
        if (isInspecting) return;

        if (Input.GetMouseButtonDown(0) && !isShooting)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (isReloading && gunData.canCancelReload)
            {
                isCancelReloading = true;
                return;
            }

            TryShoot();
        }
    }
    #endregion

    #region Shooting - Abstract Methods
    protected virtual void TryShoot()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        if (isShooting || isReloading || isInspecting) return;

        // Check fire rate
        if (Time.time - lastShotTime < gunData.fireRate) return;

        if (currentAmmoCount < gunData.ammoPerShot)
        {
            if (!isReloading && !isShooting)
                TryStartReload();
            return;
        }

        PerformShoot();
    }

    protected virtual void PerformShoot()
    {
        isShooting = true;
        lastShotTime = Time.time;
        currentAmmoCount -= gunData.ammoPerShot;

        if (animator != null)
            animator.SetTrigger(ShootHash);

        PlaySound(shootSound);

        CancelInvoke(nameof(ResetShootingFlags));
        Invoke(nameof(ResetShootingFlags), gunData.fireRate);
    }

    protected virtual void PerformRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, raycastMask))
            HandleHit(hit);
    }

    protected virtual void HandleHit(RaycastHit hit)
    {
        IGunHit gunHit = hit.collider.GetComponent<IGunHit>();
        gunHit?.OnGunHit(gunData);
    }

    public virtual void OnShootComplete()
    {
        ResetShootingFlags();
    }

    protected virtual void ResetShootingFlags()
    {
        isShooting = false;
        CancelInvoke(nameof(ResetShootingFlags));
    }
    #endregion

    #region Reloading - Abstract Methods
    protected virtual void TryStartReload()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        if (isReloading || isShooting || isInspecting) return;
        if (currentAmmoCount >= gunData.maxAmmo) return;

        UpdateAvailableAmmoCount();

        if (availableAmmoInInventory <= 0)
        {
            PlaySound(emptyClickSound);
            return;
        }

        StartReload();
    }

    protected abstract void StartReload();
    protected abstract void CancelReload();
    public abstract void AddOneBullet(); // For bullet-by-bullet reload
    public abstract void ReloadFinished(); // For magazine reload

    protected virtual void FinishReload()
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
    #endregion

    #region Aiming
    private bool IsValidQuaternion(Quaternion q)
    {
        if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
            return false;

        if (float.IsInfinity(q.x) || float.IsInfinity(q.y) ||
            float.IsInfinity(q.z) || float.IsInfinity(q.w))
            return false;

        // Zero-length quaternion check (this is what breaks Quaternion.Lerp)
        float magSq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        if (magSq < 1e-6f)
            return false;

        return true;
    }

    protected virtual void UpdateAiming()
    {
        if (equipPoint == null || playerCamera == null) return;

        float t = aimSpeed * Time.deltaTime;
        if (t <= 0f || float.IsNaN(t) || float.IsInfinity(t)) return;

        float targetFOV = isAiming ? gunData.aimFOV : normalFOV;
        playerCamera.fieldOfView =
            Mathf.Lerp(playerCamera.fieldOfView, targetFOV, t);

        Vector3 targetPosition = isAiming ? gunData.aimPosition : normalPosition;
        equipPoint.localPosition =
            Vector3.Lerp(equipPoint.localPosition, targetPosition, t);

        Quaternion targetRotation = isAiming ? gunData.aimRotation : normalRotation;
        Quaternion currentRotation = equipPoint.localRotation;

        if (IsValidQuaternion(targetRotation) && IsValidQuaternion(currentRotation))
        {
            equipPoint.localRotation =
                Quaternion.Lerp(currentRotation, targetRotation, t);
        }
    }


    public bool IsAiming() => isAiming;
    public float GetAimMovementMultiplier() => isAiming ? gunData.aimMovementSpeedMultiplier : 1f;
    public float GetAimSensitivityMultiplier() => isAiming ? gunData.aimSensitivityMultiplier : 1f;
    #endregion

    #region Recoil
    public virtual void ApplyRecoil()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        PerformRaycast();

        if (cameraRecoil != null)
        {
            float recoilMultiplier = isAiming ? gunData.aimRecoilMultiplier : 1f;

            float randomX = Random.Range(-gunData.recoilVariation, gunData.recoilVariation) * recoilMultiplier;
            float randomY = Random.Range(-gunData.recoilVariation * 0.5f, gunData.recoilVariation * 0.5f) * recoilMultiplier;

            Vector3 finalRotationRecoil = new Vector3(
                (gunData.recoilRotation.x + randomX) * recoilMultiplier,
                (gunData.recoilRotation.y + randomY) * recoilMultiplier,
                gunData.recoilRotation.z * recoilMultiplier
            );

            Vector3 finalPositionRecoil = new Vector3(
                gunData.recoilPosition.x * recoilMultiplier,
                gunData.recoilPosition.y * recoilMultiplier,
                (gunData.recoilPosition.z + Random.Range(-0.02f, 0.02f)) * recoilMultiplier
            );

            cameraRecoil.ApplyRecoil(finalRotationRecoil, finalPositionRecoil);
        }

        if (gunData.hasTinnitus)
            StartCoroutine(StartTinnitus());
    }
    #endregion

    #region Inspect
    protected virtual void StartInspect()
    {
        if (isInspecting || isReloading || isShooting) return;
        inspectCoroutine = StartCoroutine(InspectCoroutine());
    }

    protected virtual IEnumerator InspectCoroutine()
    {
        currentTimer = 0f;
        isInspecting = true;

        isAiming = false;
        if (aimReticle != null)
            aimReticle.gameObject.SetActive(false);

        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmoCount}/{availableAmmoInInventory}";
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

    protected virtual void CancelInspect()
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
    #endregion

    #region Tinnitus Effect
    protected virtual IEnumerator StartTinnitus()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) yield break;

        yield return new WaitForSeconds(0.2f);

        if (hasTinnitus)
        {
            tinnitusTimer = 0f;
            CancelInvoke(nameof(StartFadeOut));
            Invoke(nameof(StartFadeOut), gunData.tinnitusDuration - tinnitusFadeOutTime);
            yield break;
        }

        hasTinnitus = true;
        tinnitusTimer = 0f;
        StartFadeIn();

        CancelInvoke(nameof(StartFadeOut));
        Invoke(nameof(StartFadeOut), gunData.tinnitusDuration - tinnitusFadeOutTime);
    }

    protected virtual void StartFadeIn()
    {
        isFadingIn = true;
        isFadingOut = false;
        tinnitusFadeProgress = 0f;

        if (tinnitusAudioSource != null && tinnitusSound != null)
        {
            if (tinnitusAudioSource.clip == null)
                tinnitusAudioSource.clip = tinnitusSound;

            if (!tinnitusAudioSource.isPlaying)
            {
                tinnitusAudioSource.volume = 0f;
                tinnitusAudioSource.Play();
            }
        }
    }

    protected virtual void StartFadeOut()
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        isFadingIn = false;
        isFadingOut = true;
        tinnitusFadeProgress = 0f;
    }

    protected virtual void UpdateTinnitusFade()
    {
        if (!hasTinnitus) return;

        tinnitusTimer += Time.deltaTime;

        if (isFadingIn)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeInTime;
            float fadeAmount = fadeCurve.Evaluate(Mathf.Clamp01(tinnitusFadeProgress));

            if (tinnitusAudioSource != null)
                tinnitusAudioSource.volume = Mathf.Lerp(0f, gunData.tinnitusVolume, fadeAmount);

            ApplyVolumeReduction(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
            {
                isFadingIn = false;
                if (tinnitusAudioSource != null)
                    tinnitusAudioSource.volume = gunData.tinnitusVolume;
                ApplyVolumeReduction(1f);
            }
        }

        if (isFadingOut)
        {
            tinnitusFadeProgress += Time.deltaTime / tinnitusFadeOutTime;
            float fadeAmount = Mathf.Clamp01(tinnitusFadeProgress);

            if (tinnitusAudioSource != null)
                tinnitusAudioSource.volume = Mathf.Lerp(gunData.tinnitusVolume, 0f, fadeAmount);

            RestoreVolume(fadeAmount);

            if (tinnitusFadeProgress >= 1f)
                StopTinnitus();
        }

        if (tinnitusTimer >= gunData.tinnitusDuration && !isFadingOut)
            StartFadeOut();
    }

    protected virtual void ApplyVolumeReduction(float progress)
    {
        if (audioSource != null)
        {
            float targetVolume = originalAudioSourceVolume * (1f - volumeReduction);
            audioSource.volume = Mathf.Lerp(originalAudioSourceVolume, targetVolume, progress);
        }
    }

    protected virtual void RestoreVolume(float progress)
    {
        if (audioSource != null)
        {
            float reducedVolume = originalAudioSourceVolume * (1f - volumeReduction);
            audioSource.volume = Mathf.Lerp(reducedVolume, originalAudioSourceVolume, progress);
        }
    }

    protected virtual void StopTinnitus()
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
            audioSource.volume = originalAudioSourceVolume;

        CancelInvoke(nameof(StartFadeOut));
    }
    #endregion

    #region Ammo Management
    protected virtual void UpdateAvailableAmmoCount()
    {
        if (inventorySystem == null)
        {
            availableAmmoInInventory = 0;
            firstAmmoTypeFound = null;
            return;
        }

        var allSlots = inventorySystem.GetAllSlots();
        availableAmmoInInventory = 0;
        firstAmmoTypeFound = null;

        foreach (var slot in allSlots)
        {
            if (slot.itemData != null && slot.itemData is AmmoItemData ammoData)
            {
                availableAmmoInInventory += slot.quantity;

                if (firstAmmoTypeFound == null)
                    firstAmmoTypeFound = ammoData;
            }
        }
    }

    protected virtual bool ConsumeAmmoFromInventory(int amount)
    {
        if (inventorySystem == null || amount <= 0 || availableAmmoInInventory <= 0)
            return false;

        if (firstAmmoTypeFound != null)
            return inventorySystem.RemoveItem(firstAmmoTypeFound, amount);

        return false;
    }
    #endregion

    #region Audio
    protected virtual void PlaySound(AudioClip clip)
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void PlayReloadSound() => PlaySound(reloadSound);
    public void PlayReloadIntroSound() => PlaySound(reloadIntroSound);
    public void PlayReloadOutroSound() => PlaySound(reloadOutroSound);
    #endregion

    #region IItemUsable Implementation
    public virtual void OnEquip(InventorySlotsUI slotUI)
    {
        equippedSlot = slotUI;

        if (equippedSlot.itemData is GunItemData data)
        {
            // Sync GunData with GunItemData from inventory
            gunData.maxAmmo = data.maxAmmo;
            gunData.ammoPerShot = data.ammoPerShot;
            gunData.fireRate = data.fireRate;
            gunData.reloadTime = data.reloadTime;

            if (!initialized)
            {
                currentAmmoCount = gunData.maxAmmo;
                initialized = true;
            }

            if (stateTracker != null)
                stateTracker.SetState(ItemState.Equipped);

            gameObject.SetActive(true);
            UpdateAvailableAmmoCount();
        }
    }

    public virtual void OnUse(InventorySlotsUI slotUI) { }

    public virtual void OnUnequip(InventorySlotsUI slotUI)
    {
        if (equippedSlot == slotUI)
        {
            if (stateTracker != null)
                stateTracker.SetState(ItemState.InInventory);

            isReloading = false;
            isShooting = false;
            isAiming = false;
            CancelInspect();
            StopTinnitus();
            CancelInvoke();

            if (playerCamera != null)
                playerCamera.fieldOfView = normalFOV;

            gameObject.SetActive(false);
            equippedSlot = null;
        }
    }

    public virtual void OnItemStateChanged(ItemState previousState, ItemState newState)
    {
        UpdateBehaviorBasedOnState();

        if (newState == ItemState.Equipped)
        {
            canUpdate = true;

            if (isReloading) CancelReload();
            if (hasTinnitus) StopTinnitus();
            CancelInspect();

            isAiming = false;
            if (playerCamera != null)
                playerCamera.fieldOfView = normalFOV;

            if (gameObject != null)
                gameObject.SetActive(true);

            UpdateAvailableAmmoCount();
        }
        else if (newState == ItemState.InWorld || newState == ItemState.InInventory)
        {
            canUpdate = false;

            if (isReloading) CancelReload();
            if (hasTinnitus) StopTinnitus();
            CancelInspect();

            isShooting = false;
            isAiming = false;
            CancelInvoke();

            if (newState == ItemState.InWorld)
                equippedSlot = null;

            if (gameObject != null && newState == ItemState.InWorld)
                gameObject.SetActive(false);
        }
    }

    public virtual void OnPickedUp() { }

    public virtual void OnDroppedInWorld()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (tinnitusAudioSource != null && tinnitusAudioSource.isPlaying)
            tinnitusAudioSource.Stop();
    }

    protected virtual void UpdateBehaviorBasedOnState()
    {
        if (stateTracker == null) return;

        if (stateTracker.IsEquipped)
        {
            canUpdate = true;
        }
        else
        {
            canUpdate = false;
            if (isReloading) CancelReload();
            if (hasTinnitus) StopTinnitus();
            CancelInspect();
            isShooting = false;
            isAiming = false;
        }
    }
    #endregion

    #region Public Getters
    public int GetCurrentAmmo() => currentAmmoCount;
    public int GetMaxAmmo() => gunData.maxAmmo;
    public int GetAvailableAmmoInInventory() => availableAmmoInInventory;
    public float GetAmmoPercent() => gunData.maxAmmo > 0 ? (float)currentAmmoCount / gunData.maxAmmo : 0f;
    public bool IsReloading() => isReloading;
    public bool IsShooting() => isShooting;
    public bool HasTinnitus() => hasTinnitus;
    public bool IsInspecting() => isInspecting;
    public bool CanShoot() => !isShooting && !isReloading && !isInspecting && currentAmmoCount >= gunData.ammoPerShot;
    #endregion
}