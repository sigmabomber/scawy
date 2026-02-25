
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class ControllerInputManager : MonoBehaviour
{
    public static ControllerInputManager Instance { get; private set; }

    [Header("Navigation Settings")]
    public float navigationCooldown = 0.2f;
    public float analogDeadzone = 0.2f;

    [Header("References")]
    public GameObject selectedGameObject;

    private PlayerInput playerInput;
    private float lastNavigationTime;
    private Vector2 lastStickInput;
    private bool isUsingController = false;

    public bool IsUsingController => isUsingController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        // Use InputSystem.onActionChange instead of onDeviceChange
        InputSystem.onActionChange += OnActionChange;
        CheckCurrentDevice();
    }

    private void OnDestroy()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        // Check when control scheme changes
        if (change == InputActionChange.ActionPerformed && playerInput != null)
        {
            CheckCurrentDevice();
        }
    }

    private void CheckCurrentDevice()
    {
        if (playerInput != null)
        {
            // Check current control scheme
            string currentScheme = playerInput.currentControlScheme;
            isUsingController = currentScheme != null &&
                               (currentScheme.Contains("Gamepad") ||
                                currentScheme.Contains("Joystick") ||
                                IsGamepadConnected());

            Debug.Log($"Using controller: {isUsingController}, Scheme: {currentScheme}");
        }
    }

    private bool IsGamepadConnected()
    {
        // Check if any gamepad is connected
        return Gamepad.current != null ||
               Joystick.current != null ||
               Gamepad.all.Count > 0;
    }

    public void UpdateSelectedGameObject(GameObject newSelection)
    {
        if (isUsingController && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(newSelection);
            selectedGameObject = newSelection;
        }
    }

    public void ClearSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            selectedGameObject = null;
        }
    }

    // Alternative method to check device changes manually
    public void OnControlsChanged()
    {
        CheckCurrentDevice();
    }
}