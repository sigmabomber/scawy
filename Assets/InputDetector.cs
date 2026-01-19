using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using System.Text.RegularExpressions;

public class InputDetector : MonoBehaviour
{
    public enum InputType { KeyboardMouse, Controller }
    public InputType currentInput = InputType.KeyboardMouse;

    // Tracks which specific controller device is being used
    private Gamepad currentController;
    private string controllerDisplayName = "None";

    public static InputDetector Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to device change events
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Update()
    {
        // Detect keyboard/mouse
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
            Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                      Mouse.current.rightButton.wasPressedThisFrame ||
                                      Mouse.current.middleButton.wasPressedThisFrame ||
                                      Mouse.current.delta.ReadValue().magnitude > 0))
        {
            currentInput = InputType.KeyboardMouse;
            currentController = null;
            controllerDisplayName = "None";
        }

        // Check all connected gamepads
        CheckForControllerInput();
    }

    private void CheckForControllerInput()
    {
        if (Gamepad.current == null) return;

        // Check all connected gamepads
        ReadOnlyArray<Gamepad> gamepads = Gamepad.all;

        foreach (Gamepad gamepad in gamepads)
        {
            bool buttonPressed = false;

            // Check all buttons
            foreach (var control in gamepad.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    buttonPressed = true;
                    break;
                }
            }

            // Check analog sticks movement
            if (!buttonPressed)
            {
                if (gamepad.leftStick.ReadValue().magnitude > 0.1f ||
                    gamepad.rightStick.ReadValue().magnitude > 0.1f ||
                    Mathf.Abs(gamepad.leftTrigger.ReadValue()) > 0.1f ||
                    Mathf.Abs(gamepad.rightTrigger.ReadValue()) > 0.1f)
                {
                    buttonPressed = true;
                }
            }

            if (buttonPressed)
            {
                currentInput = InputType.Controller;
                currentController = gamepad;
                controllerDisplayName = GetControllerDisplayName(gamepad);
                break;
            }
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                Debug.Log($"Device connected: {device.displayName} (Layout: {device.layout})");
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                Debug.Log($"Device disconnected: {device.displayName}");

                // If our current controller was disconnected, revert to keyboard
                if (device is Gamepad gamepadDisconnected && gamepadDisconnected == currentController)
                {
                    currentInput = InputType.KeyboardMouse;
                    currentController = null;
                    controllerDisplayName = "None";
                }
                break;
        }
    }

    private string GetControllerDisplayName(Gamepad gamepad)
    {
        if (gamepad == null) return "None";

        // Check for specific controller types
        if (gamepad is UnityEngine.InputSystem.DualShock.DualShockGamepad)
        {
            return "PlayStation";
        }
        else if (gamepad is UnityEngine.InputSystem.XInput.XInputController)
        {
            return "Xbox";
        }
        else if (IsSteamDeck(gamepad))
        {
            // Return Xbox for Steam Deck - most games use Xbox prompts for Steam Deck
            return "Xbox";
        }

        // Fallback to the device's display name
        return "Generic Controller";
    }

    private bool IsSteamDeck(Gamepad gamepad)
    {
        if (gamepad == null) return false;

        // Method 1: Check by device name/layout
        string deviceName = gamepad.displayName;
        string layout = gamepad.layout;

        // Common Steam Deck identifiers
        if (deviceName.Contains("Steam Deck") ||
            deviceName.Contains("Neptune") || // Steam Deck's codename
            layout.Contains("Steam") ||
            layout.Contains("Neptune"))
        {
            return true;
        }

        // Method 2: Check vendor/product ID patterns
        if (gamepad.description.interfaceName?.Contains("HID") == true)
        {
            // Steam Deck's vendor ID is 0x28de (Valve)
            if (gamepad.description.manufacturer?.Contains("Valve") == true ||
                gamepad.description.product?.Contains("Steam Deck") == true)
            {
                return true;
            }
        }

        // Method 3: Check by name patterns
        return IsSteamDeckByName(deviceName);
    }

    private bool IsSteamDeckByName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName)) return false;

        string lowerName = deviceName.ToLower();

        // Patterns for Steam Deck controller names
        return lowerName.Contains("steam deck") ||
               lowerName.Contains("neptune") ||
               lowerName.Contains("valve") ||
               Regex.IsMatch(lowerName, @"steam.*deck", RegexOptions.IgnoreCase);
    }

    // Public getters
    public bool IsUsingController() => currentInput == InputType.Controller;
    public bool IsUsingKeyboard() => currentInput == InputType.KeyboardMouse;

    public Gamepad GetCurrentController() => currentController;

    public string GetControllerName()
    {
        return controllerDisplayName;
    }

    public string GetDetailedControllerInfo()
    {
        if (!IsUsingController() || currentController == null)
            return "Not using a controller";

        bool isSteamDeck = IsSteamDeck(currentController);
        string controllerType = "Generic";

        if (isSteamDeck)
            controllerType = "Steam Deck (Showing Xbox prompts)";
        else if (IsUsingXboxController())
            controllerType = "Xbox";
        else if (IsUsingPlayStationController())
            controllerType = "PlayStation";

        return $"Controller: {controllerDisplayName}\n" +
               $"Type: {controllerType}\n" +
               $"Device ID: {currentController.deviceId}";
    }

    // Check for specific controller types
    public bool IsUsingXboxController()
    {
        return currentController is UnityEngine.InputSystem.XInput.XInputController || IsSteamDeck(currentController);
    }

    public bool IsUsingPlayStationController()
    {
        return currentController is UnityEngine.InputSystem.DualShock.DualShockGamepad;
    }

    public bool IsUsingSteamDeck()
    {
        return IsSteamDeck(currentController);
    }

    // Utility method to get all connected controllers
    public string[] GetAllConnectedControllers()
    {
        ReadOnlyArray<Gamepad> gamepads = Gamepad.all;
        string[] controllerNames = new string[gamepads.Count];

        for (int i = 0; i < gamepads.Count; i++)
        {
            controllerNames[i] = GetControllerDisplayName(gamepads[i]);
        }

        return controllerNames;
    }
}