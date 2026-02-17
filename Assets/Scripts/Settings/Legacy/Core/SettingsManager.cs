using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting.FullSerializer;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Doody.Settings
{
    // ===== ENUMS & STRUCTS =====
    public enum SettingsCategory
    {
        Audio,
        Video,
        Controls,
        Input,
        All
    }

    // Remove old InputDeviceType enum, we'll use InputDetector.InputType
    public enum ControllerType
    {
        Xbox,
        PlayStation,
        Nintendo,
        Generic,
        Keyboard,
        SteamDeck
    }

    // ===== CONTROLLER BINDING MAPPING =====
    // ===== CONTROLLER BINDING MAPPING =====
    public static class ControllerBinding
    {
        // Button to sprite name mapping
        public static string GetSpriteName(string binding, ControllerType controllerType)
        {
            if (controllerType == ControllerType.Keyboard)
                return GetKeySpriteName(binding);

            string prefix = controllerType switch
            {
                ControllerType.PlayStation => "ps_",
                ControllerType.Nintendo => "nintendo_",
                _ => "xbox_"
            };

            // Steam Deck shows Xbox prompts by default
            if (controllerType == ControllerType.SteamDeck)
                prefix = "xbox_";

            return binding switch
            {
                "ButtonSouth" => prefix + (controllerType == ControllerType.PlayStation ? "cross" :
                                          controllerType == ControllerType.Nintendo ? "b" : "a"),
                "ButtonEast" => prefix + (controllerType == ControllerType.PlayStation ? "circle" :
                                         controllerType == ControllerType.Nintendo ? "a" : "b"),
                "ButtonWest" => prefix + (controllerType == ControllerType.PlayStation ? "square" :
                                         controllerType == ControllerType.Nintendo ? "y" : "x"),
                "ButtonNorth" => prefix + (controllerType == ControllerType.PlayStation ? "triangle" :
                                          controllerType == ControllerType.Nintendo ? "x" : "y"),
                "LeftShoulder" => prefix + (controllerType == ControllerType.Nintendo ? "l" : "lb"),
                "RightShoulder" => prefix + (controllerType == ControllerType.Nintendo ? "r" : "rb"),
                "LeftTrigger" => prefix + (controllerType == ControllerType.Nintendo ? "zl" : "lt"),
                "RightTrigger" => prefix + (controllerType == ControllerType.Nintendo ? "zr" : "rt"),
                "Back" => prefix + (controllerType == ControllerType.PlayStation ? "share" :
                                   controllerType == ControllerType.Nintendo ? "minus" : "view"),
                "Start" => prefix + (controllerType == ControllerType.PlayStation ? "options" :
                                    controllerType == ControllerType.Nintendo ? "plus" : "menu"),
                "LeftStick" => prefix + (controllerType == ControllerType.Nintendo ? "ls_press" : "ls"),
                "RightStick" => prefix + (controllerType == ControllerType.Nintendo ? "rs_press" : "rs"),
                "DPad_Up" => prefix + (controllerType == ControllerType.Nintendo ? "dpad_up" : "dpad_up"),
                "DPad_Down" => prefix + (controllerType == ControllerType.Nintendo ? "dpad_down" : "dpad_down"),
                "DPad_Left" => prefix + (controllerType == ControllerType.Nintendo ? "dpad_left" : "dpad_left"),
                "DPad_Right" => prefix + (controllerType == ControllerType.Nintendo ? "dpad_right" : "dpad_right"),

                // Joystick axes and buttons
                "LeftStick_Up" => prefix + (controllerType == ControllerType.Nintendo ? "ls_up" : "ls_up"),
                "LeftStick_Down" => prefix + (controllerType == ControllerType.Nintendo ? "ls_down" : "ls_down"),
                "LeftStick_Left" => prefix + (controllerType == ControllerType.Nintendo ? "ls_left" : "ls_left"),
                "LeftStick_Right" => prefix + (controllerType == ControllerType.Nintendo ? "ls_right" : "ls_right"),
                "RightStick_Up" => prefix + (controllerType == ControllerType.Nintendo ? "rs_up" : "rs_up"),
                "RightStick_Down" => prefix + (controllerType == ControllerType.Nintendo ? "rs_down" : "rs_down"),
                "RightStick_Left" => prefix + (controllerType == ControllerType.Nintendo ? "rs_left" : "rs_left"),
                "RightStick_Right" => prefix + (controllerType == ControllerType.Nintendo ? "rs_right" : "rs_right"),
                "LeftStick_Click" => prefix + (controllerType == ControllerType.Nintendo ? "ls_press" : "ls"),
                "RightStick_Click" => prefix + (controllerType == ControllerType.Nintendo ? "rs_press" : "rs"),

                // Additional joystick buttons (common on flight sticks, etc.)
                "Joystick1" => prefix + "button1",
                "Joystick2" => prefix + "button2",
                "Joystick3" => prefix + "button3",
                "Joystick4" => prefix + "button4",
                "Joystick5" => prefix + "button5",
                "Joystick6" => prefix + "button6",
                "Joystick7" => prefix + "button7",
                "Joystick8" => prefix + "button8",
                "Joystick9" => prefix + "button9",
                "Joystick10" => prefix + "button10",
                "Joystick11" => prefix + "button11",
                "Joystick12" => prefix + "button12",
                "Joystick13" => prefix + "button13",
                "Joystick14" => prefix + "button14",
                "Joystick15" => prefix + "button15",
                "Joystick16" => prefix + "button16",
                "Joystick17" => prefix + "button17",
                "Joystick18" => prefix + "button18",
                "Joystick19" => prefix + "button19",
                "Joystick20" => prefix + "button20",

                // Joystick hats/POV
                "Hat_Up" => prefix + "hat_up",
                "Hat_Down" => prefix + "hat_down",
                "Hat_Left" => prefix + "hat_left",
                "Hat_Right" => prefix + "hat_right",

                // Throttle/Flaps controls
                "Throttle" => prefix + "throttle",
                "Flaps" => prefix + "flaps",
                "Brake" => prefix + "brake",

                _ => prefix + "unknown"
            };
        }

        private static string GetKeySpriteName(string keyName)
        {
            // Handle joystick-specific key names
            if (keyName.StartsWith("Joystick"))
            {
                // Extract button number from "JoystickButtonX" or just "JoystickX"
                if (keyName.Contains("Button"))
                {
                    string buttonNum = keyName.Replace("JoystickButton", "");
                    return "joystick_" + buttonNum;
                }
                else
                {
                    string buttonNum = keyName.Replace("Joystick", "");
                    return "joystick_" + buttonNum;
                }
            }

            // Handle axes
            if (keyName.StartsWith("JoystickAxis"))
            {
                string axisNum = keyName.Replace("JoystickAxis", "");
                return "joystick_axis_" + axisNum;
            }

            return "key_" + keyName.ToLower();
        }

        // Button to display text mapping
        public static string GetDisplayName(string binding, ControllerType controllerType)
        {
            if (controllerType == ControllerType.Keyboard)
            {
                // Handle joystick keyboard bindings
                if (binding.StartsWith("JoystickButton"))
                {
                    string buttonNum = binding.Replace("JoystickButton", "");
                    return $"Joy Btn {buttonNum}";
                }
                else if (binding.StartsWith("Joystick"))
                {
                    string buttonNum = binding.Replace("Joystick", "");
                    return $"Joy {buttonNum}";
                }
                else if (binding.StartsWith("JoystickAxis"))
                {
                    string axisNum = binding.Replace("JoystickAxis", "");
                    return $"Joy Axis {axisNum}";
                }
                return binding;
            }

            return binding switch
            {
                "ButtonSouth" => controllerType == ControllerType.PlayStation ? "Cross" :
                               controllerType == ControllerType.Nintendo ? "B" : "A",
                "ButtonEast" => controllerType == ControllerType.PlayStation ? "Circle" :
                              controllerType == ControllerType.Nintendo ? "A" : "B",
                "ButtonWest" => controllerType == ControllerType.PlayStation ? "Square" :
                              controllerType == ControllerType.Nintendo ? "Y" : "X",
                "ButtonNorth" => controllerType == ControllerType.PlayStation ? "Triangle" :
                               controllerType == ControllerType.Nintendo ? "X" : "Y",
                "LeftShoulder" => controllerType == ControllerType.PlayStation ? "L1" :
                                controllerType == ControllerType.Nintendo ? "L" : "LB",
                "RightShoulder" => controllerType == ControllerType.PlayStation ? "R1" :
                                 controllerType == ControllerType.Nintendo ? "R" : "RB",
                "LeftTrigger" => controllerType == ControllerType.PlayStation ? "L2" :
                               controllerType == ControllerType.Nintendo ? "ZL" : "LT",
                "RightTrigger" => controllerType == ControllerType.PlayStation ? "R2" :
                                controllerType == ControllerType.Nintendo ? "ZR" : "RT",
                "Back" => controllerType == ControllerType.PlayStation ? "Share" :
                         controllerType == ControllerType.Nintendo ? "Minus" : "View",
                "Start" => controllerType == ControllerType.PlayStation ? "Options" :
                          controllerType == ControllerType.Nintendo ? "Plus" : "Menu",
                "LeftStick" => "L3",
                "RightStick" => "R3",
                "DPad_Up" => "D-Pad Up",
                "DPad_Down" => "D-Pad Down",
                "DPad_Left" => "D-Pad Left",
                "DPad_Right" => "D-Pad Right",

                // Joystick axes
                "LeftStick_Up" => "Left Stick Up",
                "LeftStick_Down" => "Left Stick Down",
                "LeftStick_Left" => "Left Stick Left",
                "LeftStick_Right" => "Left Stick Right",
                "RightStick_Up" => "Right Stick Up",
                "RightStick_Down" => "Right Stick Down",
                "RightStick_Left" => "Right Stick Left",
                "RightStick_Right" => "Right Stick Right",
                "LeftStick_Click" => "Left Stick Click",
                "RightStick_Click" => "Right Stick Click",

                // Joystick buttons
                "Joystick1" => "Joy Button 1",
                "Joystick2" => "Joy Button 2",
                "Joystick3" => "Joy Button 3",
                "Joystick4" => "Joy Button 4",
                "Joystick5" => "Joy Button 5",
                "Joystick6" => "Joy Button 6",
                "Joystick7" => "Joy Button 7",
                "Joystick8" => "Joy Button 8",
                "Joystick9" => "Joy Button 9",
                "Joystick10" => "Joy Button 10",
                "Joystick11" => "Joy Button 11",
                "Joystick12" => "Joy Button 12",
                "Joystick13" => "Joy Button 13",
                "Joystick14" => "Joy Button 14",
                "Joystick15" => "Joy Button 15",
                "Joystick16" => "Joy Button 16",
                "Joystick17" => "Joy Button 17",
                "Joystick18" => "Joy Button 18",
                "Joystick19" => "Joy Button 19",
                "Joystick20" => "Joy Button 20",

                // Joystick hat/POV
                "Hat_Up" => "Hat Up",
                "Hat_Down" => "Hat Down",
                "Hat_Left" => "Hat Left",
                "Hat_Right" => "Hat Right",

                // Flight controls
                "Throttle" => "Throttle",
                "Flaps" => "Flaps",
                "Brake" => "Brake",

                _ => binding
            };
        }

        // Get all available controller bindings (including joystick)
        public static List<string> GetAllControllerBindings()
        {
            var bindings = new List<string>
        {
            "ButtonSouth",
            "ButtonEast",
            "ButtonWest",
            "ButtonNorth",
            "LeftShoulder",
            "RightShoulder",
            "LeftTrigger",
            "RightTrigger",
            "Back",
            "Start",
            "LeftStick",
            "RightStick",
            "DPad_Up",
            "DPad_Down",
            "DPad_Left",
            "DPad_Right",
            
            // Joystick axes
            "LeftStick_Up",
            "LeftStick_Down",
            "LeftStick_Left",
            "LeftStick_Right",
            "RightStick_Up",
            "RightStick_Down",
            "RightStick_Left",
            "RightStick_Right",
            "LeftStick_Click",
            "RightStick_Click",
            
            // Joystick buttons
            "Joystick1",
            "Joystick2",
            "Joystick3",
            "Joystick4",
            "Joystick5",
            "Joystick6",
            "Joystick7",
            "Joystick8",
            "Joystick9",
            "Joystick10",
            "Joystick11",
            "Joystick12",
            "Joystick13",
            "Joystick14",
            "Joystick15",
            "Joystick16",
            "Joystick17",
            "Joystick18",
            "Joystick19",
            "Joystick20",
            
            // Joystick hat/POV
            "Hat_Up",
            "Hat_Down",
            "Hat_Left",
            "Hat_Right",
            
            // Flight controls
            "Throttle",
            "Flaps",
            "Brake"
        };

            return bindings;
        }

        // Get all KeyCodes (including joystick buttons)
        public static List<KeyCode> GetAllKeyCodes()
        {
            List<KeyCode> keyCodes = new List<KeyCode>();
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                // Include joystick buttons for joystick/hotas support
                if ((int)key >= (int)KeyCode.JoystickButton0 && (int)key <= (int)KeyCode.JoystickButton19)
                {
                    keyCodes.Add(key);
                    continue;
                }

                // Skip weird system keys
                if ((int)key >= (int)KeyCode.Joystick1Button0 && (int)key <= (int)KeyCode.Joystick8Button19)
                    continue;

                // Skip other system/reserved keys
                if (key == KeyCode.None ||
                    key == KeyCode.Escape ||
                    key.ToString().Contains("Sys") ||
                    key.ToString().Contains("Break") ||
                    key.ToString().Contains("Menu"))
                    continue;

                keyCodes.Add(key);
            }
            return keyCodes;
        }

        // Get display name for KeyCode (updated for joystick support)
        public static string GetKeyCodeDisplayName(KeyCode key)
        {
            string keyName = key.ToString();

            // Handle joystick buttons
            if (keyName.StartsWith("JoystickButton"))
            {
                string buttonNum = keyName.Replace("JoystickButton", "");
                return $"Joy Btn {buttonNum}";
            }

            // Handle standard joystick buttons (0-19)
            if (keyName.StartsWith("Joystick") && !keyName.Contains("1Button") && !keyName.Contains("2Button") &&
                !keyName.Contains("3Button") && !keyName.Contains("4Button") && !keyName.Contains("5Button") &&
                !keyName.Contains("6Button") && !keyName.Contains("7Button") && !keyName.Contains("8Button"))
            {
                string buttonNum = keyName.Replace("Joystick", "");
                return $"Joy {buttonNum}";
            }

            if (keyName.StartsWith("Alpha"))
                return keyName.Substring(5);
            if (keyName.StartsWith("Keypad"))
                return "Num " + keyName.Substring(6);

            return key switch
            {
                KeyCode.LeftControl => "L Ctrl",
                KeyCode.RightControl => "R Ctrl",
                KeyCode.LeftShift => "L Shift",
                KeyCode.RightShift => "R Shift",
                KeyCode.LeftAlt => "L Alt",
                KeyCode.RightAlt => "R Alt",
                KeyCode.Mouse0 => "Left Click",
                KeyCode.Mouse1 => "Right Click",
                KeyCode.Mouse2 => "Middle Click",
                KeyCode.Mouse3 => "Mouse 3",
                KeyCode.Mouse4 => "Mouse 4",
                KeyCode.Mouse5 => "Mouse 5",
                KeyCode.Mouse6 => "Mouse 6",
                KeyCode.Space => "Space",
                KeyCode.Return => "Enter",
                KeyCode.Tab => "Tab",
                KeyCode.Backspace => "Backspace",
                KeyCode.Delete => "Del",
                KeyCode.Insert => "Ins",
                KeyCode.Home => "Home",
                KeyCode.End => "End",
                KeyCode.PageUp => "PgUp",
                KeyCode.PageDown => "PgDown",
                KeyCode.UpArrow => "↑",
                KeyCode.DownArrow => "↓",
                KeyCode.LeftArrow => "←",
                KeyCode.RightArrow => "→",
                _ => keyName
            };
        }

        // Get axes for joystick/hotas input
        public static List<string> GetJoystickAxes()
        {
            return new List<string>
        {
            "LeftStick_Up",
            "LeftStick_Down",
            "LeftStick_Left",
            "LeftStick_Right",
            "RightStick_Up",
            "RightStick_Down",
            "RightStick_Left",
            "RightStick_Right",
            "Throttle",
            "Brake"
        };
        }

        // Check if a binding is a joystick axis
        public static bool IsJoystickAxis(string binding)
        {
            return binding.Contains("Stick_") ||
                   binding == "Throttle" ||
                   binding == "Flaps" ||
                   binding == "Brake";
        }

        // Check if a binding is a joystick button
        public static bool IsJoystickButton(string binding)
        {
            return binding.StartsWith("Joystick") ||
                   binding == "Hat_Up" ||
                   binding == "Hat_Down" ||
                   binding == "Hat_Left" ||
                   binding == "Hat_Right";
        }
    }
    // ===== BASE SETTING CLASS =====
    public abstract class Setting<T>
    {
        public string Name { get; protected set; }
        public T DefaultValue { get; protected set; }
        protected T currentValue;

        public T Value
        {
            get => currentValue;
            set => SetValue(value);
        }

        public Setting(string name, T defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
            currentValue = defaultValue;
        }

        public virtual void SetValue(T value)
        {
            T oldValue = currentValue;
            currentValue = value;
            OnValueChanged(oldValue, value);
        }

        public void Reset()
        {
            SetValue(DefaultValue);
        }

        protected virtual void OnValueChanged(T oldValue, T newValue) { }
    }

    // ===== SPECIFIC SETTING TYPES =====
    public class FloatSetting : Setting<float>
    {
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }

        public FloatSetting(string name, float defaultValue, float min = 0f, float max = 1f)
            : base(name, defaultValue)
        {
            MinValue = min;
            MaxValue = max;
        }

        public override void SetValue(float value)
        {
            base.SetValue(Mathf.Clamp(value, MinValue, MaxValue));
        }
    }

    public class IntSetting : Setting<int>
    {
        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }

        public IntSetting(string name, int defaultValue, int min = int.MinValue, int max = int.MaxValue)
            : base(name, defaultValue)
        {
            MinValue = min;
            MaxValue = max;
        }

        public override void SetValue(int value)
        {
            base.SetValue(Mathf.Clamp(value, MinValue, MaxValue));
        }
    }

    public class BoolSetting : Setting<bool>
    {
        public BoolSetting(string name, bool defaultValue) : base(name, defaultValue) { }

        public void Toggle()
        {
            SetValue(!Value);
        }
    }

    public class EnumSetting<TEnum> : Setting<TEnum> where TEnum : Enum
    {
        private TEnum[] values;

        public EnumSetting(string name, TEnum defaultValue) : base(name, defaultValue)
        {
            values = (TEnum[])Enum.GetValues(typeof(TEnum));
        }

        public TEnum GetNext()
        {
            int currentIndex = Array.IndexOf(values, Value);
            int nextIndex = (currentIndex + 1) % values.Length;
            return values[nextIndex];
        }

        public void Cycle()
        {
            SetValue(GetNext());
        }
    }

    // ===== INPUT BIND =====
    [Serializable]
    public class InputBind
    {
        public string action;
        public KeyCode keyboardKey;
        public string controllerBinding;
        public bool supportsController = true;

        public InputBind(string action, KeyCode keyboardKey, string controllerBinding = "", bool supportsController = true)
        {
            this.action = action;
            this.keyboardKey = keyboardKey;
            this.controllerBinding = controllerBinding;
            this.supportsController = supportsController;
        }
    }

    // ===== INPUT BIND MANAGER =====
    [Serializable]
    public class InputBindManager
    {
        private Dictionary<string, InputBind> inputBinds = new Dictionary<string, InputBind>();
        private Dictionary<string, InputBind> defaultInputBinds = new Dictionary<string, InputBind>();

        private ControllerType detectedControllerType = ControllerType.Keyboard;
        private bool usingController = false;

        public event Action OnInputBindsChanged;

        public void RegisterInputBind(string action, KeyCode defaultKey, string defaultControllerBinding = "", bool supportsController = true)
        {
            if (!defaultInputBinds.ContainsKey(action))
            {
                var bind = new InputBind(action, defaultKey, defaultControllerBinding, supportsController);
                defaultInputBinds[action] = bind;
                inputBinds[action] = new InputBind(action, defaultKey, defaultControllerBinding, supportsController);
            }
        }

        public void SetKeyboardBind(string action, KeyCode newKey)
        {
            if (!inputBinds.ContainsKey(action))
            {
                Debug.LogWarning($"[InputBindManager] Action '{action}' not registered");
                return;
            }

            inputBinds[action].keyboardKey = newKey;
            OnInputBindsChanged?.Invoke();
        }

        public void SetControllerBind(string action, string newBinding)
        {
            if (!inputBinds.ContainsKey(action))
            {
                Debug.LogWarning($"[InputBindManager] Action '{action}' not registered");
                return;
            }

            inputBinds[action].controllerBinding = newBinding;
            OnInputBindsChanged?.Invoke();
        }

        public KeyCode GetKeyboardBind(string action)
        {
            return inputBinds.TryGetValue(action, out InputBind bind) ? bind.keyboardKey : KeyCode.None;
        }

        public string GetControllerBind(string action)
        {
            return inputBinds.TryGetValue(action, out InputBind bind) ? bind.controllerBinding : "";
        }

        public InputBind GetInputBind(string action)
        {
            return inputBinds.TryGetValue(action, out InputBind bind) ? bind : null;
        }

        public List<InputBind> GetInputBindsForDevice(bool forController)
        {
            List<InputBind> binds = new List<InputBind>();

            foreach (var kvp in inputBinds)
            {
                var bind = kvp.Value;

                if (!forController)
                {
                    binds.Add(bind);
                }
                else if (forController && bind.supportsController)
                {
                    binds.Add(bind);
                }
            }

            return binds;
        }

        public Dictionary<string, InputBind> GetAllInputBinds()
        {
            return new Dictionary<string, InputBind>(inputBinds);
        }

        public List<InputBind> GetInputBindsList()
        {
            List<InputBind> list = new List<InputBind>();
            foreach (var kvp in inputBinds)
            {
                list.Add(new InputBind(kvp.Key, kvp.Value.keyboardKey, kvp.Value.controllerBinding, kvp.Value.supportsController));
            }
            return list;
        }

        public void LoadInputBinds(List<InputBind> binds)
        {
            foreach (var bind in binds)
            {
                if (inputBinds.ContainsKey(bind.action))
                {
                    inputBinds[bind.action].keyboardKey = bind.keyboardKey;
                    inputBinds[bind.action].controllerBinding = bind.controllerBinding;
                }
            }
            OnInputBindsChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            foreach (var kvp in defaultInputBinds)
            {
                if (inputBinds.ContainsKey(kvp.Key))
                {
                    inputBinds[kvp.Key].keyboardKey = kvp.Value.keyboardKey;
                    inputBinds[kvp.Key].controllerBinding = kvp.Value.controllerBinding;
                }
            }
            OnInputBindsChanged?.Invoke();
        }

        // Updated detection using InputDetector
        public void UpdateInputDetection()
        {
            if (InputDetector.Instance == null) return;

            usingController = InputDetector.Instance.IsUsingController();

            if (usingController)
            {
                string controllerName = InputDetector.Instance.GetControllerName();
                detectedControllerType = controllerName switch
                {
                    "PlayStation" => ControllerType.PlayStation,
                    "Xbox" => ControllerType.Xbox,
                    "Steam Deck" => ControllerType.SteamDeck,
                    _ => ControllerType.Generic
                };

                // Check specific controller types
                if (InputDetector.Instance.IsUsingSteamDeck())
                    detectedControllerType = ControllerType.SteamDeck;
                else if (InputDetector.Instance.IsUsingXboxController())
                    detectedControllerType = ControllerType.Xbox;
                else if (InputDetector.Instance.IsUsingPlayStationController())
                    detectedControllerType = ControllerType.PlayStation;
            }
            else
            {
                detectedControllerType = ControllerType.Keyboard;
            }
        }

        public bool IsControllerConnected()
        {
            return InputDetector.Instance?.IsUsingController() ?? false;
        }

        public ControllerType GetControllerType()
        {
            return detectedControllerType;
        }

        public bool IsUsingController()
        {
            return usingController;
        }

        public bool IsActionPressed(string action)
        {
            if (usingController && IsControllerConnected())
            {
                string binding = GetControllerBind(action);
                if (!string.IsNullOrEmpty(binding))
                {
                    return IsControllerButtonPressed(binding);
                }
            }

            return Input.GetKey(GetKeyboardBind(action));
        }

        public bool IsActionDown(string action)
        {
            if (usingController && IsControllerConnected())
            {
                string binding = GetControllerBind(action);
                if (!string.IsNullOrEmpty(binding))
                {
                    return IsControllerButtonDown(binding);
                }
            }

            return Input.GetKeyDown(GetKeyboardBind(action));
        }

        private bool IsControllerButtonPressed(string binding)
        {
            // Use Input System for more accurate detection
            if (Gamepad.current == null) return false;

            return binding switch
            {
                "ButtonSouth" => Gamepad.current.buttonSouth.isPressed,
                "ButtonEast" => Gamepad.current.buttonEast.isPressed,
                "ButtonWest" => Gamepad.current.buttonWest.isPressed,
                "ButtonNorth" => Gamepad.current.buttonNorth.isPressed,
                "LeftShoulder" => Gamepad.current.leftShoulder.isPressed,
                "RightShoulder" => Gamepad.current.rightShoulder.isPressed,
                "LeftTrigger" => Gamepad.current.leftTrigger.isPressed,
                "RightTrigger" => Gamepad.current.rightTrigger.isPressed,
                "Back" => Gamepad.current.selectButton.isPressed,
                "Start" => Gamepad.current.startButton.isPressed,
                "LeftStick" => Gamepad.current.leftStickButton.isPressed,
                "RightStick" => Gamepad.current.rightStickButton.isPressed,
                "DPad_Up" => Gamepad.current.dpad.up.isPressed,
                "DPad_Down" => Gamepad.current.dpad.down.isPressed,
                "DPad_Left" => Gamepad.current.dpad.left.isPressed,
                "DPad_Right" => Gamepad.current.dpad.right.isPressed,
                _ => false
            };
        }

        private bool IsControllerButtonDown(string binding)
        {
            if (Gamepad.current == null) return false;

            return binding switch
            {
                "ButtonSouth" => Gamepad.current.buttonSouth.wasPressedThisFrame,
                "ButtonEast" => Gamepad.current.buttonEast.wasPressedThisFrame,
                "ButtonWest" => Gamepad.current.buttonWest.wasPressedThisFrame,
                "ButtonNorth" => Gamepad.current.buttonNorth.wasPressedThisFrame,
                "LeftShoulder" => Gamepad.current.leftShoulder.wasPressedThisFrame,
                "RightShoulder" => Gamepad.current.rightShoulder.wasPressedThisFrame,
                "Back" => Gamepad.current.selectButton.wasPressedThisFrame,
                "Start" => Gamepad.current.startButton.wasPressedThisFrame,
                "LeftStick" => Gamepad.current.leftStickButton.wasPressedThisFrame,
                "RightStick" => Gamepad.current.rightStickButton.wasPressedThisFrame,
                _ => false
            };
        }

        public Vector2 GetMovementInput()
        {
            if (usingController && IsControllerConnected() && Gamepad.current != null)
            {
                // Use Input System for controller movement
                Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
                Vector2 dpad = new Vector2(
                    (Gamepad.current.dpad.right.isPressed ? 1 : 0) - (Gamepad.current.dpad.left.isPressed ? 1 : 0),
                    (Gamepad.current.dpad.up.isPressed ? 1 : 0) - (Gamepad.current.dpad.down.isPressed ? 1 : 0)
                );

                // Combine stick and dpad input
                Vector2 input = leftStick.magnitude > dpad.magnitude ? leftStick : dpad;
                return Vector2.ClampMagnitude(input, 1f);
            }

            // Fallback to keyboard
            Vector2 keyboardInput = Vector2.zero;
            if (Input.GetKey(GetKeyboardBind("MoveForward"))) keyboardInput.y += 1;
            if (Input.GetKey(GetKeyboardBind("MoveBackward"))) keyboardInput.y -= 1;
            if (Input.GetKey(GetKeyboardBind("MoveLeft"))) keyboardInput.x -= 1;
            if (Input.GetKey(GetKeyboardBind("MoveRight"))) keyboardInput.x += 1;

            return Vector2.ClampMagnitude(keyboardInput, 1f);
        }

        public Vector2 GetMouseLookInput(float sensitivity = 2.0f, bool invertY = false)
        {
            float x = Input.GetAxis("Mouse X") * sensitivity;
            float y = Input.GetAxis("Mouse Y") * sensitivity * (invertY ? -1 : 1);
            return new Vector2(x, y);
        }

        public Vector2 GetControllerLookInput(float sensitivity = 2.0f, bool invertY = false)
        {
            if (Gamepad.current == null) return Vector2.zero;

            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            float x = rightStick.x * sensitivity;
            float y = rightStick.y * sensitivity * (invertY ? -1 : 1);
            return new Vector2(x, y);
        }

        // Get sprite name for binding
        public string GetBindingSpriteName(string binding)
        {
           // Debug.Log($"{binding} / {ControllerBinding.GetSpriteName(binding, detectedControllerType)}");
            return ControllerBinding.GetSpriteName(binding, detectedControllerType);
        }

        // Get display name for binding
        public string GetBindingDisplayName(string binding)
        {
            return ControllerBinding.GetDisplayName(binding, detectedControllerType);
        }

        // Get display name for KeyCode
        public string GetKeyCodeDisplayName(KeyCode key)
        {
            return ControllerBinding.GetKeyCodeDisplayName(key);
        }

        // Get all available controller bindings
        public List<string> GetAllControllerBindings()
        {
            return ControllerBinding.GetAllControllerBindings();
        }

        // Get all KeyCodes
        public List<KeyCode> GetAllKeyCodes()
        {
            return ControllerBinding.GetAllKeyCodes();
        }
    }

    // ===== SETTINGS SAVE DATA =====
    [Serializable]
    public class SettingsSaveData
    {
        public List<InputBind> InputBindsList = new List<InputBind>();
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SFXVolume = 1f;
        public int ScreenWidth = 1920;
        public int ScreenHeight = 1080;
        public int TargetFrameRate = 60;
        public FullScreenMode FullscreenMode = FullScreenMode.FullScreenWindow;
        public bool VSync = true;
        public float MouseSensitivity = 2.0f;
        public float ControllerSensitivity = 2.0f;
        public bool InvertYAxis = false;
        public bool InvertXAxis = false;
        public float VibrationIntensity = 1.0f;
    }

    // ===== MAIN SETTINGS MANAGER =====
    public class SettingsManager : MonoBehaviour
    {
        private static SettingsManager instance;
        public static SettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SettingsManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[SettingsManager]");
                        instance = go.AddComponent<SettingsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        // Audio Settings
        public FloatSetting MasterVolume { get; private set; }
        public FloatSetting MusicVolume { get; private set; }
        public FloatSetting SFXVolume { get; private set; }

        // Video Settings
        public IntSetting ScreenWidth { get; private set; }
        public IntSetting ScreenHeight { get; private set; }
        public IntSetting TargetFrameRate { get; private set; }
        public EnumSetting<FullScreenMode> FullscreenMode { get; private set; }
        public BoolSetting VSync { get; private set; }

        // Control Settings
        public FloatSetting MouseSensitivity { get; private set; }
        public FloatSetting ControllerSensitivity { get; private set; }
        public BoolSetting InvertYAxis { get; private set; }
        public BoolSetting InvertXAxis { get; private set; }
        public FloatSetting VibrationIntensity { get; private set; }

        // Input
        public InputBindManager InputBinds { get; private set; }

        // Events
        public event Action OnSettingsChanged;
        public event Action OnGraphicsSettingsApplied;
        public event Action<bool> OnInputDeviceChanged; // true = controller, false = keyboard

        private string savePath;
        private bool autoSave = true;
        private bool lastControllerState = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Application.persistentDataPath + "/settings.json";
            InitializeSettings();
            LoadSettings();

            // Initialize InputDetector if it doesn't exist
            if (InputDetector.Instance == null)
            {
                GameObject detectorObj = new GameObject("[InputDetector]");
                detectorObj.AddComponent<InputDetector>();
                DontDestroyOnLoad(detectorObj);
            }
        }

        private void Update()
        {
            // Update input detection
            InputBinds.UpdateInputDetection();

            // Check for input device changes
            bool currentControllerState = InputBinds.IsUsingController();
            if (currentControllerState != lastControllerState)
            {
                lastControllerState = currentControllerState;
                OnInputDeviceChanged?.Invoke(currentControllerState);
            }
        }

        private void InitializeSettings()
        {
            // Audio
            MasterVolume = new FloatSetting("MasterVolume", 1f, 0f, 1f);
            MusicVolume = new FloatSetting("MusicVolume", 1f, 0f, 1f);
            SFXVolume = new FloatSetting("SFXVolume", 1f, 0f, 1f);

            // Video
            Resolution nativeRes = GetNativeResolution();
            ScreenWidth = new IntSetting("ScreenWidth", nativeRes.width, 640, 7680);
            ScreenHeight = new IntSetting("ScreenHeight", nativeRes.height, 480, 4320);
#pragma warning disable CS0618 // Type or member is obsolete
            TargetFrameRate = new IntSetting("TargetFrameRate", nativeRes.refreshRate, 30, 240);
#pragma warning restore CS0618 // Type or member is obsolete
            FullscreenMode = new EnumSetting<FullScreenMode>("FullscreenMode", FullScreenMode.FullScreenWindow);
            VSync = new BoolSetting("VSync", true);

            // Control Settings
            MouseSensitivity = new FloatSetting("MouseSensitivity", 2.0f, 0.1f, 10f);
            ControllerSensitivity = new FloatSetting("ControllerSensitivity", 2.0f, 0.1f, 10f);
            InvertYAxis = new BoolSetting("InvertYAxis", false);
            InvertXAxis = new BoolSetting("InvertXAxis", false);
            VibrationIntensity = new FloatSetting("VibrationIntensity", 1.0f, 0f, 1f);

            // Input Binds
            InputBinds = new InputBindManager();
            RegisterDefaultInputBinds();

            // Subscribe to changes
            InputBinds.OnInputBindsChanged += () => OnSettingsChanged?.Invoke();
        }

        private void RegisterDefaultInputBinds()
        {
            // Movement
            InputBinds.RegisterInputBind("MoveForward", KeyCode.W, "DPad_Up");
            InputBinds.RegisterInputBind("MoveBackward", KeyCode.S, "DPad_Down");
            InputBinds.RegisterInputBind("MoveLeft", KeyCode.A, "DPad_Left");
            InputBinds.RegisterInputBind("MoveRight", KeyCode.D, "DPad_Right");

            // Actions
            InputBinds.RegisterInputBind("Jump", KeyCode.Space, "ButtonSouth");
            InputBinds.RegisterInputBind("Attack", KeyCode.Mouse0, "ButtonWest");
            InputBinds.RegisterInputBind("Interact", KeyCode.E, "ButtonEast");
            InputBinds.RegisterInputBind("Sprint", KeyCode.LeftShift, "LeftStick");
            InputBinds.RegisterInputBind("Crouch", KeyCode.LeftControl, "RightStick");
            InputBinds.RegisterInputBind("Aim", KeyCode.Mouse1, "LeftTrigger");

            // Gameplay
            InputBinds.RegisterInputBind("Reload", KeyCode.R, "ButtonNorth");
            InputBinds.RegisterInputBind("Pause", KeyCode.Escape, "Start");
            InputBinds.RegisterInputBind("Map", KeyCode.M, "Back");

            // Weapons
            InputBinds.RegisterInputBind("Weapon1", KeyCode.Alpha1, "LeftShoulder");
            InputBinds.RegisterInputBind("Weapon2", KeyCode.Alpha2, "RightShoulder");
        }

        private Resolution GetNativeResolution()
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions.Length == 0)
                return Screen.currentResolution;

            Resolution highest = resolutions[0];
            foreach (var res in resolutions)
            {
                if (res.width > highest.width || (res.width == highest.width && res.height > highest.height))
                {
                    highest = res;
                }
            }
            return highest;
        }

        // ===== SMART CATEGORY METHODS =====
        public void ApplyCurrentCategory(SettingsCategory category)
        {
            switch (category)
            {
                case SettingsCategory.Audio:
                    SaveSettings();
                    break;
                case SettingsCategory.Video:
                    ApplyGraphicsSettings();
                    break;
                case SettingsCategory.Controls:
                    SaveSettings();
                    break;
                case SettingsCategory.Input:
                    SaveSettings();
                    break;
                case SettingsCategory.All:
                    ApplyGraphicsSettings();
                    SaveSettings();
                    break;
            }
        }

        public void ResetCurrentCategory(SettingsCategory category)
        {
            switch (category)
            {
                case SettingsCategory.Audio:
                    ResetAudioSettings();
                    break;
                case SettingsCategory.Video:
                    ResetVideoSettings();
                    break;
                case SettingsCategory.Controls:
                    ResetControlSettings();
                    break;
                case SettingsCategory.Input:
                    InputBinds.ResetToDefaults();
                    break;
                case SettingsCategory.All:
                    ResetAllSettings();
                    break;
            }
            OnSettingsChanged?.Invoke();
        }

        public void ApplyGraphicsSettings()
        {
            Screen.SetResolution(ScreenWidth.Value, ScreenHeight.Value, FullscreenMode.Value);
            QualitySettings.vSyncCount = VSync.Value ? 1 : 0;
            Application.targetFrameRate = VSync.Value ? -1 : TargetFrameRate.Value;
            OnGraphicsSettingsApplied?.Invoke();
        }

        public void ResetAudioSettings()
        {
            MasterVolume.Reset();
            MusicVolume.Reset();
            SFXVolume.Reset();
        }

        public void ResetVideoSettings()
        {
            ScreenWidth.Reset();
            ScreenHeight.Reset();
            TargetFrameRate.Reset();
            FullscreenMode.Reset();
            VSync.Reset();
            ApplyGraphicsSettings();
        }

        public void ResetControlSettings()
        {
            MouseSensitivity.Reset();
            ControllerSensitivity.Reset();
            InvertYAxis.Reset();
            InvertXAxis.Reset();
            VibrationIntensity.Reset();
        }

        public void ResetAllSettings()
        {
            ResetAudioSettings();
            ResetVideoSettings();
            ResetControlSettings();
            InputBinds.ResetToDefaults();
        }

        public void SaveSettings()
        {
            try
            {
                SettingsSaveData data = new SettingsSaveData
                {
                    InputBindsList = InputBinds.GetInputBindsList(),
                    MasterVolume = MasterVolume.Value,
                    MusicVolume = MusicVolume.Value,
                    SFXVolume = SFXVolume.Value,
                    ScreenWidth = ScreenWidth.Value,
                    ScreenHeight = ScreenHeight.Value,
                    TargetFrameRate = TargetFrameRate.Value,
                    FullscreenMode = FullscreenMode.Value,
                    VSync = VSync.Value,
                    MouseSensitivity = MouseSensitivity.Value,
                    ControllerSensitivity = ControllerSensitivity.Value,
                    InvertYAxis = InvertYAxis.Value,
                    InvertXAxis = InvertXAxis.Value,
                    VibrationIntensity = VibrationIntensity.Value
                };

                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(savePath, json);
                OnSettingsChanged?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        public void LoadSettings()
        {
            if (!System.IO.File.Exists(savePath))
            {
                SaveSettings();
                return;
            }

            try
            {
                string json = System.IO.File.ReadAllText(savePath);
                SettingsSaveData data = JsonUtility.FromJson<SettingsSaveData>(json);

                MasterVolume.SetValue(data.MasterVolume);
                MusicVolume.SetValue(data.MusicVolume);
                SFXVolume.SetValue(data.SFXVolume);
                ScreenWidth.SetValue(data.ScreenWidth);
                ScreenHeight.SetValue(data.ScreenHeight);
                TargetFrameRate.SetValue(data.TargetFrameRate);
                FullscreenMode.SetValue(data.FullscreenMode);
                VSync.SetValue(data.VSync);
                MouseSensitivity.SetValue(data.MouseSensitivity);
                ControllerSensitivity.SetValue(data.ControllerSensitivity);
                InvertYAxis.SetValue(data.InvertYAxis);
                InvertXAxis.SetValue(data.InvertXAxis);
                VibrationIntensity.SetValue(data.VibrationIntensity);

                InputBinds.LoadInputBinds(data.InputBindsList);

                ApplyGraphicsSettings();
                OnSettingsChanged?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                ResetAllSettings();
            }
        }

        // ===== CONVENIENCE METHODS =====
        public bool IsControllerConnected() => InputBinds.IsControllerConnected();
        public ControllerType GetControllerType() => InputBinds.GetControllerType();
        public Vector2 GetMovementInput() => InputBinds.GetMovementInput();
        public Vector2 GetMouseLookInput() => InputBinds.GetMouseLookInput(MouseSensitivity.Value, InvertYAxis.Value);
        public Vector2 GetControllerLookInput() => InputBinds.GetControllerLookInput(ControllerSensitivity.Value, InvertYAxis.Value);

        public bool IsActionPressed(string action) => InputBinds.IsActionPressed(action);
        public bool IsActionDown(string action) => InputBinds.IsActionDown(action);

        public KeyCode GetKeyBinding(string action) => InputBinds.GetKeyboardBind(action);
        public string GetControllerBinding(string action) => InputBinds.GetControllerBind(action);

        // Sprite support methods
        public string GetBindingSpriteName(string binding) => InputBinds.GetBindingSpriteName(binding);
        public string GetBindingDisplayName(string binding) => InputBinds.GetBindingDisplayName(binding);
        public string GetKeyCodeDisplayName(KeyCode key) => InputBinds.GetKeyCodeDisplayName(key);

        // Quick Actions
        public bool Jump => IsActionDown("Jump");
        public bool Attack => IsActionPressed("Attack");
        public bool Interact => IsActionDown("Interact");
        public bool Sprint => IsActionPressed("Sprint");
        public bool Aim => IsActionPressed("Aim");
        public bool Pause => IsActionDown("Pause");
        public bool UsingController => InputBinds.IsUsingController();

        // Get all bindings and keys
        public List<string> GetAllControllerBindings() => InputBinds.GetAllControllerBindings();
        public List<KeyCode> GetAllKeyCodes() => InputBinds.GetAllKeyCodes();

        private void OnApplicationQuit()
        {
            if (autoSave)
                SaveSettings();
        }

        private void OnDestroy()
        {
            if (autoSave)
                SaveSettings();
        }
    }
}