using Doody.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Controller Settings")]
    [SerializeField] private float controllerLookSensitivity = 1.5f;
    [SerializeField] private float controllerDeadzone = 0.1f;
    [SerializeField] private float controllerLookDeadzone = 0.05f;

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
    private bool isInventoryOpen = false;

    // OPTIMIZATION: Cached values
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
    private float staminaUpdateInterval = 0.1f;

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
        // Start with cursor locked for FPS gameplay
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

        if (!isInputEnabled || isInventoryOpen)
            return;

        // Update input cache - NOW ACCEPTS INPUT FROM BOTH SOURCES
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

    #region Input Handling

    private Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        // GET INPUT FROM BOTH KEYBOARD AND CONTROLLER SIMULTANEOUSLY

        // Keyboard input
        input.x += Input.GetAxisRaw("Horizontal");
        input.y += Input.GetAxisRaw("Vertical");

        // Controller input (if connected)
        if (Gamepad.current != null)
        {
            Vector2 gamepadInput = Gamepad.current.leftStick.ReadValue();

            // Apply deadzone
            if (gamepadInput.magnitude > controllerDeadzone)
            {
                input.x += gamepadInput.x;
                input.y += gamepadInput.y;
            }
        }

        // Clamp magnitude to prevent faster diagonal movement
        if (input.magnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    private void HandleMouseLook()
    {
        Vector2 lookInput = Vector2.zero;

        // GET LOOK INPUT FROM BOTH MOUSE AND CONTROLLER SIMULTANEOUSLY

        // Mouse input
        lookInput.x += Input.GetAxis("Mouse X");
        lookInput.y += Input.GetAxis("Mouse Y");

        // Controller input (if connected)
        if (Gamepad.current != null)
        {
            Vector2 gamepadLook = Gamepad.current.rightStick.ReadValue();

            // Apply deadzone
            if (gamepadLook.magnitude > controllerLookDeadzone)
            {
                lookInput.x += gamepadLook.x;
                lookInput.y += gamepadLook.y;
            }
        }

        // Apply sensitivity - controller gets different sensitivity
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Scale controller sensitivity separately if needed
        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().magnitude > controllerLookDeadzone)
        {
            mouseX = lookInput.x * controllerLookSensitivity;
            mouseY = lookInput.y * controllerLookSensitivity;
        }

        // Apply rotation
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

    // Input check methods - check BOTH sources
    private bool IsSprintPressed()
    {
        // Keyboard
        if (Input.GetKey(KeyCode.LeftShift))
            return true;

        // Controller
        if (Gamepad.current != null)
        {
            if ( Gamepad.current.leftStickButton.isPressed)
                return true;
        }

        return false;
    }

    private bool IsCrouchPressed()
    {
        // Keyboard
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            return true;

        // Controller
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightStickButton.isPressed)
                return true;
        }

        return false;
    }


    #endregion

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

        // Only update UI periodically
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
            controllerLookSensitivity = mouseSensitivity * 0.75f;
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

        // Check crouch input from BOTH sources
        bool crouchPressed = IsCrouchPressed();

        // Check sprint input from BOTH sources
        bool sprintPressed = IsSprintPressed();

        if (crouchPressed)
        {
            currentMovementState = MovementState.Crouching;
            targetHeight = crouchingHeight;
        }
        else if (sprintPressed && canSprint && isMoving && CanStartSprinting())
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
        // Early exit if no movement and grounded
        bool isGrounded = characterController.isGrounded;

        if (cachedInput.sqrMagnitude < movementInputThreshold && isGrounded && verticalVelocity < 0.1f)
        {
            ApplyGravity(isGrounded);
            characterController.Move(cachedUpVector * verticalVelocity * Time.deltaTime);
            lastFrameGrounded = isGrounded;
            return;
        }

        // Calculate movement direction
        cachedMoveDirection.x = transform.right.x * cachedInput.x + transform.forward.x * cachedInput.y;
        cachedMoveDirection.y = 0;
        cachedMoveDirection.z = transform.right.z * cachedInput.x + transform.forward.z * cachedInput.y;

        // Normalize
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

    // Cache ceiling check results
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
        if (currentMovementState == MovementState.Sprinting)
            return sprintSpeed;
        if (currentMovementState == MovementState.Crouching)
            return crouchSpeed;
        return walkSpeed;
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (isInventoryOpen)
        {
            // When inventory opens, unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isInputEnabled = false;
        }
        else
        {
            // When inventory closes, lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isInputEnabled = true;
        }
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