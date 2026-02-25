using Doody.Settings;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputBindUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text actionText;
    public Button bindButton;
    public TMP_Text bindText;
    public GameObject listeningIndicator;

    [Header("Sprite Support")]
    public TMP_Text spriteText;

    private string actionName;
    private bool isControllerBinding;
    private System.Action<string> onBindChanged;
    private List<KeyCode> availableKeyCodes;
    private List<string> availableControllerBindings;

    private bool isListening = false;
    private string currentSpriteName = "";
    private string currentDisplayName = "";

    void Start()
    {
        // Subscribe to device changes if SettingsManager exists
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;
        }
    }

    private void OnInputDeviceChanged(bool isController)
    {
        UpdateDisplayForCurrentDevice();
    }

    // Setup for keyboard
    public void SetupForKeyboard(string action, string displayName, string spriteName,
                               System.Action<string> onChangeCallback, List<KeyCode> keyCodes)
    {
        actionName = action;
        isControllerBinding = false;
        onBindChanged = onChangeCallback;
        availableKeyCodes = keyCodes;

        // Store the display name
        currentDisplayName = displayName;
        currentSpriteName = spriteName;

        actionText.text = FormatActionName(action);
        UpdateDisplay(displayName, spriteName);

        bindButton.onClick.RemoveAllListeners();
        bindButton.onClick.AddListener(StartListening);
    }

    // Setup for controller
    public void SetupForController(string action, string displayName, string spriteName,
                                 System.Action<string> onChangeCallback, List<string> bindings)
    {
        actionName = action;
        isControllerBinding = true;
        onBindChanged = onChangeCallback;
        availableControllerBindings = bindings;

        // Store the display name and sprite name
        currentDisplayName = displayName;
        currentSpriteName = spriteName;

        actionText.text = FormatActionName(action);
        UpdateDisplay(displayName, spriteName);

        bindButton.onClick.RemoveAllListeners();
        bindButton.onClick.AddListener(StartListening);
    }

    public void UpdateDisplayForCurrentDevice()
    {
        if (isControllerBinding)
        {
            // Get current controller binding from SettingsManager
            string currentBinding = "";
            if (SettingsManager.Instance != null)
            {
                currentBinding = SettingsManager.Instance.GetControllerBinding(actionName);
            }

            if (!string.IsNullOrEmpty(currentBinding))
            {
                // Update stored sprite and display names
                if (SettingsManager.Instance != null)
                {
                    currentSpriteName = SettingsManager.Instance.GetBindingSpriteName(currentBinding);
                    currentDisplayName = SettingsManager.Instance.GetBindingDisplayName(currentBinding);
                }

                // Use sprite if we have one and are using controller
                if (!string.IsNullOrEmpty(currentSpriteName) && IsUsingController())
                {
                    bindText.text = $"<sprite name=\"{currentSpriteName}\">";
                }
                else
                {
                    bindText.text = currentDisplayName;
                }
            }
        }
        else
        {
            // Update keyboard display if needed
            KeyCode currentKey = KeyCode.None;
            if (SettingsManager.Instance != null)
            {
                currentKey = SettingsManager.Instance.GetKeyBinding(actionName);
            }

            if (currentKey != KeyCode.None)
            {
                if (SettingsManager.Instance != null)
                {
                    currentDisplayName = SettingsManager.Instance.GetKeyCodeDisplayName(currentKey);
                }
                bindText.text = currentDisplayName;
            }
        }
    }

    private bool IsUsingController()
    {
        // Check if we're using a controller
        if (InputDetector.Instance != null)
        {
            return InputDetector.Instance.IsUsingController();
        }
        return false;
    }

    private void UpdateDisplay(string displayName, string spriteName)
    {
        // Store the values
        currentDisplayName = displayName;
        currentSpriteName = spriteName;

        // Check if we're using controller and have a sprite name
        if (isControllerBinding && IsUsingController() && !string.IsNullOrEmpty(spriteName))
        {
            // Use sprite for controller prompts
            bindText.text = $"<sprite name=\"{spriteName}\">";
        }
        else
        {
            // Use text display for keyboard or fallback
            if (bindText != null)
            {
                bindText.text = displayName;
            }
        }
    }

    private void Update()
    {
        if (!isListening) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
            return;
        }

        if (isControllerBinding)
            CheckForControllerInput();
        else
            CheckForKeyboardInput();
    }

    private void StartListening()
    {
        isListening = true;

        if (listeningIndicator != null)
            listeningIndicator.SetActive(true);

        // Clear the display while listening
        if (spriteText != null)
        {
            spriteText.text = isControllerBinding ? "Press any button..." : "Press any key...";
        }

        if (bindText != null)
        {
            bindText.text = isControllerBinding ? "Press any button..." : "Press any key...";
        }

        bindButton.interactable = false;
    }

    private void CheckForKeyboardInput()
    {
        if (!Input.anyKeyDown || availableKeyCodes == null) return;

        foreach (KeyCode keyCode in availableKeyCodes)
        {
            if (Input.GetKeyDown(keyCode) && keyCode != KeyCode.Escape)
            {
                SetBinding(keyCode.ToString());
                return;
            }
        }
    }

    private void CheckForControllerInput()
    {
        if (availableControllerBindings == null) return;

        foreach (string binding in availableControllerBindings)
        {
            if (IsControllerButtonDown(binding))
            {
                SetBinding(binding);
                return;
            }
        }
    }

    private bool IsControllerButtonDown(string binding)
    {
        // Try to use the Input System first
        if (Gamepad.current != null)
        {
            return binding switch
            {
                "ButtonSouth" => Gamepad.current.buttonSouth.wasPressedThisFrame,
                "ButtonEast" => Gamepad.current.buttonEast.wasPressedThisFrame,
                "ButtonWest" => Gamepad.current.buttonWest.wasPressedThisFrame,
                "ButtonNorth" => Gamepad.current.buttonNorth.wasPressedThisFrame,
                "LeftShoulder" => Gamepad.current.leftShoulder.wasPressedThisFrame,
                "RightShoulder" => Gamepad.current.rightShoulder.wasPressedThisFrame,
                "LeftTrigger" => Gamepad.current.leftTrigger.wasPressedThisFrame,
                "RightTrigger" => Gamepad.current.rightTrigger.wasPressedThisFrame,
                "Back" => Gamepad.current.selectButton?.wasPressedThisFrame ?? false,
                "Start" => Gamepad.current.startButton?.wasPressedThisFrame ?? false,
                "LeftStick" => Gamepad.current.leftStickButton?.wasPressedThisFrame ?? false,
                "RightStick" => Gamepad.current.rightStickButton?.wasPressedThisFrame ?? false,
                "DPad_Up" => Gamepad.current.dpad.up.wasPressedThisFrame,
                "DPad_Down" => Gamepad.current.dpad.down.wasPressedThisFrame,
                "DPad_Left" => Gamepad.current.dpad.left.wasPressedThisFrame,
                "DPad_Right" => Gamepad.current.dpad.right.wasPressedThisFrame,
                _ => false
            };
        }

        // Fallback to old input system
        return binding switch
        {
            "ButtonSouth" => Input.GetKeyDown(KeyCode.JoystickButton0),
            "ButtonEast" => Input.GetKeyDown(KeyCode.JoystickButton1),
            "ButtonWest" => Input.GetKeyDown(KeyCode.JoystickButton2),
            "ButtonNorth" => Input.GetKeyDown(KeyCode.JoystickButton3),
            "LeftShoulder" => Input.GetKeyDown(KeyCode.JoystickButton4),
            "RightShoulder" => Input.GetKeyDown(KeyCode.JoystickButton5),
            "Back" => Input.GetKeyDown(KeyCode.JoystickButton6),
            "Start" => Input.GetKeyDown(KeyCode.JoystickButton7),
            "LeftStick" => Input.GetKeyDown(KeyCode.JoystickButton8),
            "RightStick" => Input.GetKeyDown(KeyCode.JoystickButton9),
            "DPad_Up" => Input.GetAxis("DPad Vertical") > 0.5f,
            "DPad_Down" => Input.GetAxis("DPad Vertical") < -0.5f,
            "DPad_Left" => Input.GetAxis("DPad Horizontal") < -0.5f,
            "DPad_Right" => Input.GetAxis("DPad Horizontal") > 0.5f,
            _ => false
        };
    }

    private void SetBinding(string binding)
    {
        isListening = false;

        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);

        bindButton.interactable = true;

        // Update the display with the new binding
        if (isControllerBinding)
        {
            // For controller, get the sprite name and display name
            if (SettingsManager.Instance != null)
            {
                currentSpriteName = SettingsManager.Instance.GetBindingSpriteName(binding);
                currentDisplayName = SettingsManager.Instance.GetBindingDisplayName(binding);
            }

            // Use sprite if we have one and are using controller
            if (!string.IsNullOrEmpty(currentSpriteName) && IsUsingController())
            {
                bindText.text = $"<sprite name=\"{currentSpriteName}\">";
            }
            else
            {
                bindText.text = currentDisplayName;
            }
        }
        else
        {
            // For keyboard, just show the display name
            if (SettingsManager.Instance != null)
            {
                currentDisplayName = SettingsManager.Instance.GetKeyCodeDisplayName((KeyCode)System.Enum.Parse(typeof(KeyCode), binding));
            }
            bindText.text = currentDisplayName;
        }

        onBindChanged?.Invoke(binding);
    }

    private void CancelListening()
    {
        isListening = false;

        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);

        bindButton.interactable = true;

        // Restore the original binding display
        UpdateDisplayForCurrentDevice();
    }

    private string FormatActionName(string action)
    {
        // Simple formatting: "Jump" stays "Jump", "MoveForward" becomes "Move Forward"
        // Add space before capital letters (except first letter)
        if (string.IsNullOrEmpty(action))
            return action;

        System.Text.StringBuilder formatted = new System.Text.StringBuilder();
        formatted.Append(action[0]);

        for (int i = 1; i < action.Length; i++)
        {
            if (char.IsUpper(action[i]) && !char.IsUpper(action[i - 1]))
            {
                formatted.Append(' ');
            }
            formatted.Append(action[i]);
        }

        return formatted.ToString();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
        }
    }

    // Helper method to refresh the display with current binding
    public void RefreshDisplay()
    {
        if (isControllerBinding)
        {
            // Get current controller binding
            string currentBinding = "";
            if (SettingsManager.Instance != null)
            {
                currentBinding = SettingsManager.Instance.GetControllerBinding(actionName);
            }

            if (!string.IsNullOrEmpty(currentBinding))
            {
                if (SettingsManager.Instance != null)
                {
                    currentSpriteName = SettingsManager.Instance.GetBindingSpriteName(currentBinding);
                    currentDisplayName = SettingsManager.Instance.GetBindingDisplayName(currentBinding);
                }

                UpdateDisplay(currentDisplayName, currentSpriteName);
            }
        }
        else
        {
            // Get current keyboard binding
            KeyCode currentKey = KeyCode.None;
            if (SettingsManager.Instance != null)
            {
                currentKey = SettingsManager.Instance.GetKeyBinding(actionName);
            }

            if (currentKey != KeyCode.None && SettingsManager.Instance != null)
            {
                currentDisplayName = SettingsManager.Instance.GetKeyCodeDisplayName(currentKey);
                UpdateDisplay(currentDisplayName, "");
            }
        }
    }
}