using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Singleton
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] public float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] public float mouseSensitivity = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Height Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float heightTransitionSpeed = 10f;
    [SerializeField] private float ceilingCheckDistance = 0.3f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraRecoil cameraRecoil;

    [Header("Input Settings")]
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

    private CharacterController characterController;
    public bool canSprint = true;
    private enum MovementState { Walking, Sprinting, Crouching }
    private MovementState currentMovementState = MovementState.Walking;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentHeight;
    private float targetHeight;
    private bool isInputEnabled = true;

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
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHeight = standingHeight;
        targetHeight = standingHeight;
    }

    private void Update()
    {
        // Toggle input with Escape key
        if (Input.GetKeyDown(toggleCursorKey))
        {
            ToggleInput();
        }

        // Check if any input field is focused
        if (IsAnyInputFieldFocused())
        {
            // Keep cursor unlocked and visible when typing
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        // Don't process player input if disabled
        if (!isInputEnabled)
            return;

        HandleMovementState();
        HandleMovement();
        if (Time.timeScale > 0)
            HandleMouseLook();
        HandleHeightTransition();
    }

    private bool IsAnyInputFieldFocused()
    {
        // Check if EventSystem exists and something is selected
        if (UnityEngine.EventSystems.EventSystem.current == null ||
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
            return false;

        GameObject selectedObject = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        // Check for TMP InputField (TextMeshPro)
#if TEXTMESH_PRO
        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;
#endif

        // Check for Legacy InputField (Unity UI)
        if (selectedObject.GetComponent<UnityEngine.UI.InputField>() != null)
            return true;

        return false;
    }

    private void InitializeController()
    {
        characterController.height = standingHeight;
        characterController.center = Vector3.zero;
    }

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
        else if (isSprinting && isMoving)
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

    private void HandleMovement()
    {
        Vector2 input = GetMovementInput();

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
            // Apply camera pitch + recoil
            Vector3 recoilRotation = Vector3.zero;
            if (cameraRecoil != null)
            {
                recoilRotation = cameraRecoil.CurrentRecoilRotation;
            }

            // Combine base camera rotation with recoil
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

    // Public methods to control input state
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

    // Call these methods from your input field events
    public void OnInputFieldFocus()
    {
        DisablePlayerInput();
    }

    public void OnInputFieldUnfocus()
    {
        EnablePlayerInput();
    }

    // Helper property to check if input is currently enabled
    public bool IsInputEnabled
    {
        get { return isInputEnabled && !IsAnyInputFieldFocused(); }
    }
}