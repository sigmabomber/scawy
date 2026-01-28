using Doody.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : InputScript
{
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

    [Header("Footstep Sounds")]
    [SerializeField] private float walkFootstepStrength = 0.3f;
    [SerializeField] private float sprintFootstepStrength = 0.7f;
    [SerializeField] private float minFootstepDistance = 0.3f;
    [SerializeField] private float minTimeBetweenFootsteps = 0.15f;
    [SerializeField] private float runFootstepMultiplier = 2f;
    [SerializeField] private float runPitchMultiplier = 1.1f;
    [SerializeField] private float footstepRaycastDistance = 0.5f;
    [SerializeField] private LayerMask groundDetectionMask = ~0;

    [Header("Floor Audio Clips")]
    [SerializeField]
    private FloorAudioSet[] floorAudioSets = new FloorAudioSet[]
    {
        new FloorAudioSet {
            materialTag = "Concrete",
            footstepClips = new AudioClip[0],
            pitchVariation = 0.1f
        },
    };

    [System.Serializable]
    public class FloorAudioSet
    {
        public string materialTag;
        public AudioClip[] footstepClips;
        public float pitchVariation = 0.1f;
    }

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraRecoil cameraRecoil;
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Input Settings")]
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.None;

    private CharacterController characterController;
    public bool canSprint = true;
    private enum MovementState { Walking, Sprinting, Crouching }
    private MovementState currentMovementState = MovementState.Walking;

    public float currentStamina;
    public float timeSinceLastSprint;
    public bool isExhausted = false;

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

    private float distanceSinceLastFootstep = 0f;
    private float timeSinceLastFootstep = 0f;
    private string currentGroundTag = "Concrete";
    private bool isGrounded;
    private Vector3 lastPosition;
    private Dictionary<string, FloorAudioSet> floorAudioDictionary;

    private Vector3 cachedMoveDirection;
    private Vector2 cachedInput;
    private float cachedCurrentSpeed;
    private bool lastFrameGrounded;
    private Vector3 cachedUpVector = Vector3.up;
    private Vector3 cachedZeroVector = Vector3.zero;
    private Quaternion cachedCameraRotation;
    private Vector3 cachedCameraPosition;

    private float staminaUpdateThreshold = 1f;
    private float heightTransitionThreshold = 0.01f;
    private float movementInputThreshold = 0.0001f;
    private float gravityGroundClamp = -2f;

    private float lastStaminaUpdateTime;
    private float staminaUpdateInterval = 0.1f;

    private float lastCeilingCheckTime;
    private float ceilingCheckInterval = 0.2f;
    private bool lastCeilingCheckResult;

    private float lastGroundCheckTime;
    private float groundCheckInterval = 0.5f;

    private UnityEngine.EventSystems.EventSystem cachedEventSystem;

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

        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.spatialBlend = 1f;
            footstepAudioSource.minDistance = 1f;
            footstepAudioSource.maxDistance = 50f;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
        }

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeController();
        InitializeFloorAudioDictionary();

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
        lastPosition = transform.position;
    }

    private void InitializeFloorAudioDictionary()
    {
        floorAudioDictionary = new Dictionary<string, FloorAudioSet>();

        foreach (var audioSet in floorAudioSets)
        {
            if (!string.IsNullOrEmpty(audioSet.materialTag))
            {
                string tag = audioSet.materialTag.Trim();
                floorAudioDictionary[tag] = audioSet;
            }
        }
    }

    protected override void HandleInput()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            ToggleInput();
        }

        if (!isInputEnabled || isInventoryOpen)
            return;

        cachedInput = GetMovementInput();

        HandleStamina();
        HandleMovementState();
        HandleMovement();
        HandleFootsteps();

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

    private Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        input.x += Input.GetAxisRaw("Horizontal");
        input.y += Input.GetAxisRaw("Vertical");

        if (Gamepad.current != null)
        {
            Vector2 gamepadInput = Gamepad.current.leftStick.ReadValue();

            if (gamepadInput.magnitude > controllerDeadzone)
            {
                input.x += gamepadInput.x;
                input.y += gamepadInput.y;
            }
        }

        if (input.magnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    private void HandleMouseLook()
    {
        Vector2 lookInput = Vector2.zero;

        lookInput.x += Input.GetAxis("Mouse X");
        lookInput.y += Input.GetAxis("Mouse Y");

        if (Gamepad.current != null)
        {
            Vector2 gamepadLook = Gamepad.current.rightStick.ReadValue();

            if (gamepadLook.magnitude > controllerLookDeadzone)
            {
                lookInput.x += gamepadLook.x;
                lookInput.y += gamepadLook.y;
            }
        }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().magnitude > controllerLookDeadzone)
        {
            mouseX = lookInput.x * controllerLookSensitivity;
            mouseY = lookInput.y * controllerLookSensitivity;
        }

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

    private bool IsSprintPressed()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            return true;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStickButton.isPressed)
                return true;
        }

        return false;
    }

    private bool IsCrouchPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            return true;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightStickButton.isPressed)
                return true;
        }

        return false;
    }

    private void HandleFootsteps()
    {
        isGrounded = characterController.isGrounded;

        if (!isGrounded)
        {
            distanceSinceLastFootstep = 0f;
            timeSinceLastFootstep = 0f;
            return;
        }

        if (currentMovementState == MovementState.Crouching)
        {
            distanceSinceLastFootstep = 0f;
            timeSinceLastFootstep = 0f;
            return;
        }

        float speed = characterController.velocity.magnitude;
        bool isMoving = speed > 0.1f;

        if (!isMoving)
        {
            distanceSinceLastFootstep = 0f;
            timeSinceLastFootstep += Time.deltaTime;
            lastPosition = transform.position;
            return;
        }

        if (Time.time - lastGroundCheckTime >= groundCheckInterval)
        {
            DetectGroundMaterialByTag();
            lastGroundCheckTime = Time.time;
        }

        Vector3 currentPosition = transform.position;
        float frameDistance = Vector3.Distance(currentPosition, lastPosition);
        lastPosition = currentPosition;

        distanceSinceLastFootstep += frameDistance;
        timeSinceLastFootstep += Time.deltaTime;

        bool shouldPlayFootstep = CheckFootstepCondition();

        if (shouldPlayFootstep)
        {
            PlayFootstep();
            distanceSinceLastFootstep = 0f;
            timeSinceLastFootstep = 0f;
        }
    }

    private bool CheckFootstepCondition()
    {
        if (timeSinceLastFootstep < minTimeBetweenFootsteps)
            return false;

        float requiredDistance = GetRequiredFootstepDistance();
        if (distanceSinceLastFootstep >= requiredDistance)
            return true;

        float speed = characterController.velocity.magnitude;
        if (speed > 0.1f && timeSinceLastFootstep >= GetTimeBasedThreshold())
            return true;

        return false;
    }

    private float GetRequiredFootstepDistance()
    {
        float baseDistance = minFootstepDistance;
        return currentMovementState == MovementState.Sprinting ?
               baseDistance / runFootstepMultiplier :
               baseDistance;
    }

    private float GetTimeBasedThreshold()
    {
        float baseTime = 0.6f;
        return currentMovementState == MovementState.Sprinting ?
               baseTime / runFootstepMultiplier :
               baseTime;
    }

    private float GetFootstepStrength()
    {
        return currentMovementState == MovementState.Sprinting ? sprintFootstepStrength : walkFootstepStrength;
    }

    private void PlayFootstep()
    {
        if (floorAudioDictionary.TryGetValue(currentGroundTag, out FloorAudioSet audioSet))
        {
            if (audioSet.footstepClips != null && audioSet.footstepClips.Length > 0 && footstepAudioSource != null)
            {
                int randomIndex = Random.Range(0, audioSet.footstepClips.Length);
                AudioClip clip = audioSet.footstepClips[randomIndex];

                if (clip != null)
                {
                    bool isRunning = currentMovementState == MovementState.Sprinting;

                    float basePitch = isRunning ? runPitchMultiplier : 1f;
                    float pitchVariation = Random.Range(-audioSet.pitchVariation, audioSet.pitchVariation);
                    footstepAudioSource.pitch = Mathf.Clamp(basePitch + pitchVariation, 0.5f, 3f);

                    footstepAudioSource.PlayOneShot(clip);

                    float strength = GetFootstepStrength();
                    string soundTag = isRunning ? "footstep_run" : "footstep";

                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.EmitPlayerSound(transform.position, strength, soundTag);
                    }
                }
            }
        }
    }

    private void DetectGroundMaterialByTag()
    {
        Vector3 rayStart = transform.position;

        rayStart.y = transform.position.y - (characterController.height * 0.5f) + 0.1f;

        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, footstepRaycastDistance, groundDetectionMask, QueryTriggerInteraction.Ignore))
        {
            string newTag = hit.collider.tag;
            print(newTag);

            if (!string.IsNullOrEmpty(newTag) && floorAudioDictionary.ContainsKey(newTag))
            {
                if (newTag != currentGroundTag)
                {
                    currentGroundTag = newTag;
                }
            }
            else
            {
                if (currentGroundTag != "Concrete")
                {
                    currentGroundTag = "Concrete";
                }
            }
        }
        else
        {
            if (currentGroundTag != "Concrete")
            {
                currentGroundTag = "Concrete";
            }
        }
    }

    public void PlayLandingSound(float strengthMultiplier = 2.0f)
    {
        if (floorAudioDictionary.TryGetValue(currentGroundTag, out FloorAudioSet audioSet))
        {
            if (audioSet.footstepClips != null && audioSet.footstepClips.Length > 0 && footstepAudioSource != null)
            {
                int randomIndex = Random.Range(0, audioSet.footstepClips.Length);
                AudioClip clip = audioSet.footstepClips[randomIndex];

                if (clip != null)
                {
                    footstepAudioSource.pitch = 1f + Random.Range(-0.05f, 0.05f);
                    footstepAudioSource.volume = 1f * strengthMultiplier;
                    footstepAudioSource.PlayOneShot(clip);

                    distanceSinceLastFootstep = 0f;
                    timeSinceLastFootstep = 0f;

                    float strength = GetFootstepStrength() * strengthMultiplier;
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.EmitPlayerSound(transform.position, strength, "footstep_land");
                    }
                }
            }
        }
    }

    public void AddFloorAudioSet(string materialTag, AudioClip[] footstepClips, float pitchVariation = 0.1f)
    {
        FloorAudioSet newSet = new FloorAudioSet
        {
            materialTag = materialTag,
            footstepClips = footstepClips,
            pitchVariation = pitchVariation
        };

        System.Array.Resize(ref floorAudioSets, floorAudioSets.Length + 1);
        floorAudioSets[floorAudioSets.Length - 1] = newSet;

        floorAudioDictionary[materialTag] = newSet;
    }

    public string GetCurrentGroundTag() => currentGroundTag;

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

    private void HandleMovementState()
    {
        bool isMoving = cachedInput.sqrMagnitude > 0.01f;

        bool crouchPressed = IsCrouchPressed();

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
        bool isGrounded = characterController.isGrounded;

        if (cachedInput.sqrMagnitude < movementInputThreshold && isGrounded && verticalVelocity < 0.1f)
        {
            ApplyGravity(isGrounded);
            characterController.Move(cachedUpVector * verticalVelocity * Time.deltaTime);
            lastFrameGrounded = isGrounded;
            return;
        }

        cachedMoveDirection.x = transform.right.x * cachedInput.x + transform.forward.x * cachedInput.y;
        cachedMoveDirection.y = 0;
        cachedMoveDirection.z = transform.right.z * cachedInput.x + transform.forward.z * cachedInput.y;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isInputEnabled = false;
        }
        else
        {
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