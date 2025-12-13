using UnityEngine;
using Doody.GameEvents;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Singleton
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] public float walkSpeed = 5f;
    [SerializeField] public float sprintSpeed = 7.5f;
    [SerializeField] public float crouchSpeed = 2.5f;
    [SerializeField] public float mouseSensitivity = 2f;
    [SerializeField] public float gravity = -9.81f;

    [Header("Stamina Settings")]
    [SerializeField] public float maxStamina = 100f;
    [SerializeField] public float staminaDrainRate = 20f;
    [SerializeField] public float staminaRegenRate = 15f;
    [SerializeField] public float staminaRegenDelay = 1f;
    [SerializeField] public float minStaminaToSprint = 10f;
    [SerializeField] public bool infiniteStamina = false;

    [Header("Height Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float heightTransitionSpeed = 10f;
    [SerializeField] private float ceilingCheckDistance = 0.3f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraRecoil cameraRecoil;

    [Header("Input Settings")]
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.None;

    private CharacterController characterController;
    public bool canSprint = true;
    private enum MovementState { Walking, Sprinting, Crouching }
    private MovementState currentMovementState = MovementState.Walking;

    // Stamina system
    public float currentStamina;
    public float timeSinceLastSprint;
    public bool isExhausted = false;

    // Movement modifiers
    private float baseWalkSpeed;
    private float baseSprintSpeed;
    private float baseCrouchSpeed;
    private float baseMouseSensitivity;
    private float walkSpeedModifier = 1f;
    private float sprintSpeedModifier = 1f;
    private float staminaModifier = 1f;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentHeight;
    private float targetHeight;
    private bool isInputEnabled = true;

    // OPTIMIZATION: Cached input field check
    private bool wasInputFieldFocused = false;
    private float inputFieldCheckTimer = 0f;
    private const float INPUT_FIELD_CHECK_INTERVAL = 0.1f; // Check every 100ms instead of every frame

    // OPTIMIZATION: Cache event system reference
    private UnityEngine.EventSystems.EventSystem cachedEventSystem;

    // Events for stamina changes
    public class StaminaChangedEvent
    {
        public float CurrentStamina { get; set; }
        public float MaxStamina { get; set; }
        public float Percentage { get; set; }
        public bool IsExhausted { get; set; }

        public StaminaChangedEvent(float current, float max, bool exhausted)
        {
            CurrentStamina = current;
            MaxStamina = max;
            Percentage = max > 0 ? current / max : 0;
            IsExhausted = exhausted;
        }
    }

    // Event for movement stat changes
    public class MovementStatsChangedEvent
    {
        public float WalkSpeed { get; }
        public float SprintSpeed { get; }
        public float CrouchSpeed { get; }
        public float MouseSensitivity { get; }
        public float StaminaMultiplier { get; }

        public MovementStatsChangedEvent(float walkSpeed, float sprintSpeed, float crouchSpeed,
            float mouseSensitivity, float staminaMultiplier)
        {
            WalkSpeed = walkSpeed;
            SprintSpeed = sprintSpeed;
            CrouchSpeed = crouchSpeed;
            MouseSensitivity = mouseSensitivity;
            StaminaMultiplier = staminaMultiplier;
        }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }

        if (cameraRecoil == null && cameraTransform != null)
        {
            cameraRecoil = cameraTransform.GetComponent<CameraRecoil>();
        }

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeController();

        // OPTIMIZATION: Cache EventSystem reference
        cachedEventSystem = UnityEngine.EventSystems.EventSystem.current;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHeight = standingHeight;
        targetHeight = standingHeight;

        // Initialize stamina
        currentStamina = maxStamina;
        PublishStaminaUpdate();

        // Cache base values from serialized fields BEFORE any modifications
        CacheBaseMovementValues();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            ToggleInput();
        }

      
        if (!isInputEnabled)
            return;

        HandleStamina();
        HandleMovementState();
        HandleMovement();
        if (Time.timeScale > 0)
            HandleMouseLook();
        HandleHeightTransition();
    }

    private bool IsAnyInputFieldFocused()
    {
        // OPTIMIZATION: Use cached EventSystem
        if (cachedEventSystem == null)
        {
            cachedEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (cachedEventSystem == null)
                return false;
        }

        GameObject selectedObject = cachedEventSystem.currentSelectedGameObject;
        if (selectedObject == null)
            return false;

#if TEXTMESH_PRO
        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;
#endif

        if (selectedObject.GetComponent<UnityEngine.UI.InputField>() != null)
            return true;

        return false;
    }

    private void InitializeController()
    {
        characterController.height = standingHeight;
        characterController.center = Vector3.zero;
    }

    private void CacheBaseMovementValues()
    {
        // Store the original serialized values as base values
        baseWalkSpeed = walkSpeed;
        baseSprintSpeed = sprintSpeed;
        baseCrouchSpeed = crouchSpeed;
        baseMouseSensitivity = mouseSensitivity;

        // Apply initial modifiers (all 1x)
        ApplyMovementModifiers(1f, 1f, 1f, 1f);
    }

    #region Stamina System

    private void HandleStamina()
    {
        bool wasSprinting = currentMovementState == MovementState.Sprinting;
        float previousStamina = currentStamina;

        if (infiniteStamina)
        {
            currentStamina = maxStamina;
            isExhausted = false;
            return;
        }

        if (currentMovementState == MovementState.Sprinting)
        {
            // Drain stamina while sprinting (affected by stamina multiplier)
            float drainAmount = staminaDrainRate / staminaModifier * Time.deltaTime;
            currentStamina -= drainAmount;
            currentStamina = Mathf.Max(0, currentStamina);

            timeSinceLastSprint = 0f;

            // Check if exhausted
            if (currentStamina <= 0)
            {
                isExhausted = true;
                canSprint = false;
            }
        }
        else
        {
            // Regenerate stamina when not sprinting
            timeSinceLastSprint += Time.deltaTime;

            if (timeSinceLastSprint >= staminaRegenDelay)
            {
                // Regenerate faster with stamina effects
                float regenAmount = staminaRegenRate * staminaModifier * Time.deltaTime;
                currentStamina += regenAmount;
                currentStamina = Mathf.Min(maxStamina, currentStamina);

                // Recover from exhaustion
                if (isExhausted && currentStamina >= minStaminaToSprint * 2f)
                {
                    isExhausted = false;
                    canSprint = true;
                }
            }
        }

        // OPTIMIZATION: Only publish if changed significantly or state changed
        if (Mathf.Abs(currentStamina - previousStamina) > 1f || wasSprinting != (currentMovementState == MovementState.Sprinting))
        {
            PublishStaminaUpdate();
        }
    }

    private void PublishStaminaUpdate()
    {
        Events.Publish(new StaminaChangedEvent(currentStamina, maxStamina, isExhausted));
    }

    public float GetCurrentStamina() => currentStamina;
    public float GetMaxStamina() => maxStamina;
    public float GetStaminaPercentage() => maxStamina > 0 ? currentStamina / maxStamina : 0;
    public bool IsExhausted() => isExhausted;

    /// <summary>
    /// Add or remove stamina (useful for pickups or damage)
    /// </summary>
    public void ModifyStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        PublishStaminaUpdate();
    }

    /// <summary>
    /// Set stamina to full
    /// </summary>
    public void RestoreStamina()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        canSprint = true;
        PublishStaminaUpdate();
    }

    #endregion

    #region Movement Modifiers Integration

    /// <summary>
    /// Apply movement modifiers from EffectsManager
    /// Simplified version - only modifies walk, sprint, and stamina
    /// </summary>
    public void ApplyMovementModifiers(float walkModifier, float sprintModifier,
        float staminaModifier, float sensitivityModifier = 1f)
    {
        this.walkSpeedModifier = walkModifier;
        this.sprintSpeedModifier = sprintModifier;
        this.staminaModifier = staminaModifier;

        // Apply modifiers to base values
        walkSpeed = baseWalkSpeed * walkModifier;
        sprintSpeed = baseSprintSpeed * sprintModifier;

        // Apply sensitivity modifier if provided (optional)
        if (sensitivityModifier != 1f)
        {
            mouseSensitivity = baseMouseSensitivity * sensitivityModifier;
        }

        // Publish movement stats update
        PublishMovementStatsUpdate();
    }

    /// <summary>
    /// Set base movement values (used when you want to permanently change base stats)
    /// </summary>
    public void SetBaseMovementValues(float newWalkSpeed, float newSprintSpeed,
        float newCrouchSpeed, float newMouseSensitivity)
    {
        baseWalkSpeed = newWalkSpeed;
        baseSprintSpeed = newSprintSpeed;
        baseCrouchSpeed = newCrouchSpeed;
        baseMouseSensitivity = newMouseSensitivity;

        ApplyMovementModifiers(walkSpeedModifier, sprintSpeedModifier, staminaModifier);
    }

    /// <summary>
    /// Reset all movement values to their original serialized state
    /// </summary>
    public void ResetMovementValues()
    {
        // Restore original serialized values as base
        baseWalkSpeed = walkSpeed; // This gets the serialized value
        baseSprintSpeed = sprintSpeed;
        baseCrouchSpeed = crouchSpeed;
        baseMouseSensitivity = mouseSensitivity;

        // Reset modifiers to 1x
        ApplyMovementModifiers(1f, 1f, 1f);
    }

    /// <summary>
    /// Get current effective movement speeds
    /// </summary>
    public float GetEffectiveWalkSpeed() => walkSpeed;
    public float GetEffectiveSprintSpeed() => sprintSpeed;
    public float GetEffectiveCrouchSpeed() => crouchSpeed;
    public float GetEffectiveMouseSensitivity() => mouseSensitivity;
    public float GetCurrentStaminaModifier() => staminaModifier;

    /// <summary>
    /// Get base (unmodified) movement speeds
    /// </summary>
    public float GetBaseWalkSpeed() => baseWalkSpeed;
    public float GetBaseSprintSpeed() => baseSprintSpeed;
    public float GetBaseCrouchSpeed() => baseCrouchSpeed;
    public float GetBaseMouseSensitivity() => baseMouseSensitivity;

    private void PublishMovementStatsUpdate()
    {
        Events.Publish(new MovementStatsChangedEvent(
            walkSpeed,
            sprintSpeed,
            crouchSpeed,
            mouseSensitivity,
            staminaModifier
        ));
    }

    #endregion

    private void HandleMovementState()
    {
        bool isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && canSprint;
        Vector2 input = GetMovementInput();
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (isCrouching)
        {
            currentMovementState = MovementState.Crouching;
            targetHeight = crouchingHeight;
        }
        else if (isSprinting && isMoving && CanStartSprinting())
        {
            currentMovementState = MovementState.Sprinting;

            if (!CheckCeilingObstruction())
            {
                targetHeight = standingHeight;
            }
        }
        else
        {
            currentMovementState = MovementState.Walking;

            if (!CheckCeilingObstruction())
            {
                targetHeight = standingHeight;
            }
        }
    }

    private bool CanStartSprinting()
    {
        if (infiniteStamina)
            return true;

        if (isExhausted)
            return false;

        if (currentStamina < minStaminaToSprint)
            return false;

        return true;
    }

    private void HandleMovement()
    {
        Vector2 input = GetMovementInput();

        // OPTIMIZATION: Early exit if no movement
        if (input.sqrMagnitude < 0.0001f && characterController.isGrounded && verticalVelocity < 0.1f)
        {
            ApplyGravity();
            characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            return;
        }

        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        moveDirection.Normalize();

        float currentSpeed = GetCurrentSpeed();
        Vector3 horizontalMovement = moveDirection * currentSpeed;

        ApplyGravity();

        Vector3 finalMovement = horizontalMovement + Vector3.up * verticalVelocity;
        characterController.Move(finalMovement * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        if (cameraTransform != null)
        {
            Vector3 recoilRotation = Vector3.zero;
            if (cameraRecoil != null)
            {
                recoilRotation = cameraRecoil.CurrentRecoilRotation;
            }

            cameraTransform.localRotation = Quaternion.Euler(cameraPitch + recoilRotation.x, recoilRotation.y, recoilRotation.z);
        }
    }

    private void HandleHeightTransition()
    {
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight,
                heightTransitionSpeed * Time.deltaTime);

            characterController.height = currentHeight;
            characterController.center = Vector3.zero;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(0, currentHeight / 2f - 0.2f, 0);
            }
        }
    }

    public bool CheckCeilingObstruction()
    {
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight / 2f);
        float checkHeight = standingHeight - currentHeight + ceilingCheckDistance;

        return Physics.Raycast(rayStart, Vector3.up, checkHeight);
    }

    private float GetCurrentSpeed()
    {
        switch (currentMovementState)
        {
            case MovementState.Sprinting:
                return sprintSpeed;
            case MovementState.Crouching:
                return crouchSpeed;
            case MovementState.Walking:
            default:
                return walkSpeed;
        }
    }

    private Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return new Vector2(horizontal, vertical);
    }

    public void ToggleInput()
    {
        isInputEnabled = !isInputEnabled;

        if (isInputEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void DisablePlayerInput()
    {
        isInputEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnablePlayerInput()
    {
        isInputEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnInputFieldFocus()
    {
        DisablePlayerInput();
    }

    public void OnInputFieldUnfocus()
    {
        EnablePlayerInput();
    }

    public bool IsInputEnabled
    {
        get { return isInputEnabled && !wasInputFieldFocused; }
    }
}