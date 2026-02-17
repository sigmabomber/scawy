using UnityEngine;

public class InputWrapper : MonoBehaviour
{
    private static InputWrapper instance;
    public static InputWrapper Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InputWrapper>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[InputWrapper]");
                    instance = go.AddComponent<InputWrapper>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private Doody.Settings.SettingsManager settings;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        settings = Doody.Settings.SettingsManager.Instance;

        if (settings == null)
            Debug.LogError("SettingsManager not found!");
    }

    // Movement
    public Vector2 GetMovement()
    {
        return settings.GetMovementInput();
    }

    public Vector3 GetMovement3D()
    {
        Vector2 input = GetMovement();
        return new Vector3(input.x, 0, input.y);
    }

    // Looking/Camera
    public Vector2 GetLook()
    {
        if (UsingController)
            return settings.GetControllerLookInput();
        else
            return settings.GetMouseLookInput();
    }

    // Actions
    public bool GetAction(string action)
    {
        return settings.IsActionPressed(action);
    }

    public bool GetActionDown(string action)
    {
        return settings.IsActionDown(action);
    }

    // Common actions
    public bool MoveForward => GetAction("MoveForward");
    public bool MoveBackward => GetAction("MoveBackward");
    public bool MoveLeft => GetAction("MoveLeft");
    public bool MoveRight => GetAction("MoveRight");

    public bool Jump => GetActionDown("Jump");
    public bool Attack => GetAction("Attack");
    public bool Interact => GetActionDown("Interact");
    public bool Sprint => GetAction("Sprint");
    public bool Crouch => GetAction("Crouch");
    public bool Aim => GetAction("Aim");
    public bool Reload => GetActionDown("Reload");
    public bool Pause => GetActionDown("Pause");
    public bool Map => GetActionDown("Map");
    public bool Weapon1 => GetActionDown("Weapon1");
    public bool Weapon2 => GetActionDown("Weapon2");

    // Controller Status
    public bool UsingController => settings.UsingController;
    public bool ControllerConnected => settings.IsControllerConnected();
    public Doody.Settings.ControllerType ControllerType => settings.GetControllerType();

    // Settings Access
    public float MouseSensitivity => settings.MouseSensitivity.Value;
    public float ControllerSensitivity => settings.ControllerSensitivity.Value;
    public bool InvertY => settings.InvertYAxis.Value;
    public bool InvertX => settings.InvertXAxis.Value;

    // Volume
    public float MasterVolume => settings.MasterVolume.Value;
    public float MusicVolume => settings.MusicVolume.Value;
    public float SFXVolume => settings.SFXVolume.Value;

    // Cursor control
    public void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Get specific bindings
    public KeyCode GetKeyBinding(string action) => settings.GetKeyBinding(action);
    public string GetControllerBinding(string action) => settings.GetControllerBinding(action);
}