using UnityEngine;
using Doody.GameEvents;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : InputScript
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

    // OPTIMIZATION: Cached values to avoid repeated calculations
    private Vector3 cachedMoveDirection;
    private Vector2 cachedInput;
    private float cachedCurrentSpeed;
    private bool lastFrameGrounded;
    private Vector3 cachedUpVector = Vector3.up;
    private Vector3 cachedZeroVector = Vector3.zero;
    private Quaternion cachedCameraRotation;
    private Vector3 cachedCameraPosition;

    // OPTIMIZATION: Pre-calculate values
    private float staminaUpdateThreshold = 1f;
    private float heightTransitionThreshold = 0.01f;
    private float movementInputThreshold = 0.0001f;
    private float gravityGroundClamp = -2f;

    // OPTIMIZATION: Reduce stamina update frequency
    private float lastStaminaUpdateTime;
    private float staminaUpdateInterval = 0.1f; // Update UI every 0.1s instead of every frame

    // OPTIMIZATION: Cache input state
    private bool isCrouchKeyHeld;
    private bool isSprintKeyHeld;

    // OPTIMIZATION: Reduce ceiling check frequency
    private float lastCeilingCheckTime;
    private float ceilingCheckInterval = 0.2f;
    private bool lastCeilingCheckResult;

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

        currentStamina = maxStamina;
        PublishStaminaUpdate();

        CacheBaseMovementValues();

        lastFrameGrounded = characterController.isGrounded;
    }

    protected override void HandleInput()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            ToggleInput();
        }

        if (!isInputEnabled)
            return;

        // OPTIMIZATION: Cache input states once per frame
        isCrouchKeyHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        isSprintKeyHeld = Input.GetKey(KeyCode.LeftShift);
        cachedInput = GetMovementInput();

        HandleStamina();
        HandleMovementState();
        HandleMovement();
        if (Time.timeScale > 0)
            HandleMouseLook();
        HandleHeightTransition();
    }

    private void InitializeController()
    {
        characterController.height = standingHeight;
        characterController.center = cachedZeroVector;
    }

    private void CacheBaseMovementValues()
    {
        baseWalkSpeed = walkSpeed;
        baseSprintSpeed = sprintSpeed;
        baseCrouchSpeed = crouchSpeed;
        baseMouseSensitivity = mouseSensitivity;

        ApplyMovementModifiers(1f, 1f, 1f, 1f);
    }

    #region Stamina System

    private void HandleStamina()
    {
        if (infiniteStamina)
        {
            currentStamina = maxStamina;
            isExhausted = false;
            return;
        }

        bool wasSprinting = currentMovementState == MovementState.Sprinting;
        float previousStamina = currentStamina;

        if (currentMovementState == MovementState.Sprinting)
        {
            float drainAmount = staminaDrainRate / staminaModifier * Time.deltaTime;
            currentStamina -= drainAmount;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
                canSprint = false;
            }

            timeSinceLastSprint = 0f;
        }
        else
        {
            timeSinceLastSprint += Time.deltaTime;

            if (timeSinceLastSprint >= staminaRegenDelay)
            {
                float regenAmount = staminaRegenRate * staminaModifier * Time.deltaTime;
                currentStamina += regenAmount;

                if (currentStamina >= maxStamina)
                {
                    currentStamina = maxStamina;
                }

                if (isExhausted && currentStamina >= minStaminaToSprint * 2f)
                {
                    isExhausted = false;
                    canSprint = true;
                }
            }
        }

        // OPTIMIZATION: Only update UI periodically, not every frame
        float staminaDelta = Mathf.Abs(currentStamina - previousStamina);
        bool stateChanged = wasSprinting != (currentMovementState == MovementState.Sprinting);

        if ((staminaDelta > staminaUpdateThreshold || stateChanged) &&
            Time.time - lastStaminaUpdateTime >= staminaUpdateInterval)
        {
            lastStaminaUpdateTime = Time.time;
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

    public void ModifyStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        PublishStaminaUpdate();
    }

    public void RestoreStamina()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        canSprint = true;
        PublishStaminaUpdate();
    }

    #endregion

    #region Movement Modifiers Integration

    public void ApplyMovementModifiers(float walkModifier, float sprintModifier,
        float staminaModifier, float sensitivityModifier = 1f)
    {
        this.walkSpeedModifier = walkModifier;
        this.sprintSpeedModifier = sprintModifier;
        this.staminaModifier = staminaModifier;

        walkSpeed = baseWalkSpeed * walkModifier;
        sprintSpeed = baseSprintSpeed * sprintModifier;

        if (sensitivityModifier != 1f)
        {
            mouseSensitivity = baseMouseSensitivity * sensitivityModifier;
        }

        PublishMovementStatsUpdate();
    }

    public void SetBaseMovementValues(float newWalkSpeed, float newSprintSpeed,
        float newCrouchSpeed, float newMouseSensitivity)
    {
        baseWalkSpeed = newWalkSpeed;
        baseSprintSpeed = newSprintSpeed;
        baseCrouchSpeed = newCrouchSpeed;
        baseMouseSensitivity = newMouseSensitivity;

        ApplyMovementModifiers(walkSpeedModifier, sprintSpeedModifier, staminaModifier);
    }

    public void ResetMovementValues()
    {
        baseWalkSpeed = walkSpeed;
        baseSprintSpeed = sprintSpeed;
        baseCrouchSpeed = crouchSpeed;
        baseMouseSensitivity = mouseSensitivity;

        ApplyMovementModifiers(1f, 1f, 1f);
    }

    public float GetEffectiveWalkSpeed() => walkSpeed;
    public float GetEffectiveSprintSpeed() => sprintSpeed;
    public float GetEffectiveCrouchSpeed() => crouchSpeed;
    public float GetEffectiveMouseSensitivity() => mouseSensitivity;
    public float GetCurrentStaminaModifier() => staminaModifier;

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
        bool isMoving = cachedInput.sqrMagnitude > 0.01f;

        if (isCrouchKeyHeld)
        {
            currentMovementState = MovementState.Crouching;
            targetHeight = crouchingHeight;
        }
        else if (isSprintKeyHeld && canSprint && isMoving && CanStartSprinting())
        {
            currentMovementState = MovementState.Sprinting;

            if (!CheckCeilingObstructionCached())
            {
                targetHeight = standingHeight;
            }
        }
        else
        {
            currentMovementState = MovementState.Walking;

            if (!CheckCeilingObstructionCached())
            {
                targetHeight = standingHeight;
            }
        }
    }

    private bool CanStartSprinting()
    {
        if (infiniteStamina)
            return true;

        return !isExhausted && currentStamina >= minStaminaToSprint;
    }

    private void HandleMovement()
    {
        // OPTIMIZATION: Early exit if no movement and grounded
        bool isGrounded = characterController.isGrounded;

        if (cachedInput.sqrMagnitude < movementInputThreshold && isGrounded && verticalVelocity < 0.1f)
        {
            ApplyGravity(isGrounded);
            characterController.Move(cachedUpVector * verticalVelocity * Time.deltaTime);
            lastFrameGrounded = isGrounded;
            return;
        }

        // OPTIMIZATION: Reuse cached direction and avoid repeated transform lookups
        cachedMoveDirection.x = transform.right.x * cachedInput.x + transform.forward.x * cachedInput.y;
        cachedMoveDirection.y = 0; // Keep y at 0 for horizontal movement
        cachedMoveDirection.z = transform.right.z * cachedInput.x + transform.forward.z * cachedInput.y;

        // OPTIMIZATION: Fast normalize without allocation
        float magnitude = Mathf.Sqrt(cachedMoveDirection.x * cachedMoveDirection.x +
                                     cachedMoveDirection.z * cachedMoveDirection.z);
        if (magnitude > 0.0001f)
        {
            float invMag = 1f / magnitude;
            cachedMoveDirection.x *= invMag;
            cachedMoveDirection.z *= invMag;
        }

        cachedCurrentSpeed = GetCurrentSpeed();
        cachedMoveDirection.x *= cachedCurrentSpeed;
        cachedMoveDirection.z *= cachedCurrentSpeed;

        ApplyGravity(isGrounded);

        cachedMoveDirection.y = verticalVelocity;
        characterController.Move(cachedMoveDirection * Time.deltaTime);

        lastFrameGrounded = isGrounded;
    }

    private void ApplyGravity(bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = gravityGroundClamp;
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

        // OPTIMIZATION: Only rotate if there's actual input
        if (Mathf.Abs(mouseX) > 0.001f)
        {
            transform.Rotate(cachedUpVector * mouseX);
        }

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        if (cameraTransform != null)
        {
            if (cameraRecoil != null)
            {
                Vector3 recoilRotation = cameraRecoil.CurrentRecoilRotation;
                cachedCameraRotation = Quaternion.Euler(cameraPitch + recoilRotation.x, recoilRotation.y, recoilRotation.z);
            }
            else
            {
                cachedCameraRotation = Quaternion.Euler(cameraPitch, 0, 0);
            }

            cameraTransform.localRotation = cachedCameraRotation;
        }
    }

    private void HandleHeightTransition()
    {
        float heightDifference = targetHeight - currentHeight;

        if (Mathf.Abs(heightDifference) > heightTransitionThreshold)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, heightTransitionSpeed * Time.deltaTime);

            characterController.height = currentHeight;
            characterController.center = cachedZeroVector;

            if (cameraTransform != null)
            {
                cachedCameraPosition.x = 0;
                cachedCameraPosition.y = currentHeight * 0.5f - 0.2f;
                cachedCameraPosition.z = 0;
                cameraTransform.localPosition = cachedCameraPosition;
            }
        }
    }

    // OPTIMIZATION: Cache ceiling check results
    private bool CheckCeilingObstructionCached()
    {
        if (Time.time - lastCeilingCheckTime < ceilingCheckInterval)
        {
            return lastCeilingCheckResult;
        }

        lastCeilingCheckTime = Time.time;
        lastCeilingCheckResult = CheckCeilingObstruction();
        return lastCeilingCheckResult;
    }

    public bool CheckCeilingObstruction()
    {
        Vector3 rayStart = transform.position;
        rayStart.y += currentHeight * 0.5f;
        float checkHeight = standingHeight - currentHeight + ceilingCheckDistance;

        return Physics.Raycast(rayStart, cachedUpVector, checkHeight);
    }

    private float GetCurrentSpeed()
    {
        // OPTIMIZATION: Direct return instead of switch for better performance
        if (currentMovementState == MovementState.Sprinting)
            return sprintSpeed;
        if (currentMovementState == MovementState.Crouching)
            return crouchSpeed;
        return walkSpeed;
    }

    private Vector2 GetMovementInput()
    {
        // OPTIMIZATION: GetAxisRaw is faster than GetAxis
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
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
}