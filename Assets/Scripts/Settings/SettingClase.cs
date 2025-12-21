using System;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;

namespace Doody.Settings
{
    // ===== Events =====
    public struct SettingChangedEvent<T>
    {
        public string SettingName;
        public T OldValue;
        public T NewValue;
    }

    public struct KeybindChangedEvent
    {
        public string Action;
        public KeyCode OldKey;
        public KeyCode NewKey;
    }

    public struct SettingsResetEvent
    {
        public SettingsCategory Category;
    }

    public enum SettingsCategory
    {
        Audio,
        Video,
        Keybinds,
        All
    }

    // ===== Base Setting Class =====
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
            Events.Publish(new SettingChangedEvent<T>
            {
                SettingName = Name,
                OldValue = oldValue,
                NewValue = value
            });
        }

        public void Reset()
        {
            SetValue(DefaultValue);
        }

        protected virtual void OnValueChanged(T oldValue, T newValue) { }
    }

    // ===== Specific Settings =====
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

    // ===== Keybind Management =====
    [Serializable]
    public class KeybindPair
    {
        public string action;
        public KeyCode key;
    }

    public class KeybindManager
    {
        private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();
        private Dictionary<string, KeyCode> defaultKeybinds = new Dictionary<string, KeyCode>();

        public void RegisterKeybind(string action, KeyCode defaultKey)
        {
            if (!defaultKeybinds.ContainsKey(action))
            {
                defaultKeybinds[action] = defaultKey;
                keybinds[action] = defaultKey;
            }
        }

        public void SetKeybind(string action, KeyCode newKey)
        {
            if (!keybinds.ContainsKey(action))
            {
                Debug.LogWarning($"[KeybindManager] Action '{action}' not registered");
                return;
            }

            KeyCode oldKey = keybinds[action];
            keybinds[action] = newKey;

            Events.Publish(new KeybindChangedEvent
            {
                Action = action,
                OldKey = oldKey,
                NewKey = newKey
            });
        }

        public KeyCode GetKeybind(string action)
        {
            return keybinds.TryGetValue(action, out KeyCode key) ? key : KeyCode.None;
        }

        public List<string> GetActionsForKey(KeyCode key)
        {
            List<string> actions = new List<string>();
            foreach (var kvp in keybinds)
            {
                if (kvp.Value == key)
                    actions.Add(kvp.Key);
            }
            return actions;
        }

        public Dictionary<string, KeyCode> GetAllKeybinds()
        {
            return new Dictionary<string, KeyCode>(keybinds);
        }

        public List<KeybindPair> GetKeybindsList()
        {
            List<KeybindPair> list = new List<KeybindPair>();
            foreach (var kvp in keybinds)
            {
                list.Add(new KeybindPair { action = kvp.Key, key = kvp.Value });
            }
            return list;
        }

        public void LoadKeybinds(List<KeybindPair> pairs)
        {
            foreach (var pair in pairs)
            {
                if (keybinds.ContainsKey(pair.action))
                {
                    keybinds[pair.action] = pair.key;
                }
            }
        }

        public void ResetToDefaults()
        {
            foreach (var kvp in defaultKeybinds)
            {
                SetKeybind(kvp.Key, kvp.Value);
            }
            Events.Publish(new SettingsResetEvent { Category = SettingsCategory.Keybinds });
        }
    }

    // ===== Main Settings Manager =====
    public class SettingsManager : MonoBehaviour
    {
        private static SettingsManager instance;
        public static SettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("[SettingsManager]");
                    instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
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

        // Keybinds
        public KeybindManager Keybinds { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Audio
            MasterVolume = new FloatSetting("MasterVolume", 1f, 0f, 1f);
            MusicVolume = new FloatSetting("MusicVolume", 1f, 0f, 1f);
            SFXVolume = new FloatSetting("SFXVolume", 1f, 0f, 1f);

            // Video - get native resolution
            Resolution nativeRes = GetNativeResolution();
            ScreenWidth = new IntSetting("ScreenWidth", nativeRes.width, 640, 7680);
            ScreenHeight = new IntSetting("ScreenHeight", nativeRes.height, 480, 4320);
            TargetFrameRate = new IntSetting("TargetFrameRate", Mathf.RoundToInt((float)nativeRes.refreshRateRatio.value), 30, 240);
            FullscreenMode = new EnumSetting<FullScreenMode>("FullscreenMode", UnityEngine.FullScreenMode.FullScreenWindow);
            VSync = new BoolSetting("VSync", true);

            // Subscribe to video setting changes to apply them
            Events.Subscribe<SettingChangedEvent<int>>(OnVideoSettingChanged);
            Events.Subscribe<SettingChangedEvent<FullScreenMode>>(OnFullscreenModeChanged);
            Events.Subscribe<SettingChangedEvent<bool>>(OnVSyncChanged);

            // Keybinds
            Keybinds = new KeybindManager();
            RegisterDefaultKeybinds();

            // Apply initial settings
            ApplyGraphicsSettings();
        }

        private void RegisterDefaultKeybinds()
        {
            Keybinds.RegisterKeybind("up", KeyCode.UpArrow);
            Keybinds.RegisterKeybind("down", KeyCode.DownArrow);
            Keybinds.RegisterKeybind("left", KeyCode.LeftArrow);
            Keybinds.RegisterKeybind("right", KeyCode.RightArrow);
            Keybinds.RegisterKeybind("jump", KeyCode.Z);
            Keybinds.RegisterKeybind("dash", KeyCode.C);
            Keybinds.RegisterKeybind("interact", KeyCode.UpArrow);
            Keybinds.RegisterKeybind("attack", KeyCode.X);
            Keybinds.RegisterKeybind("map", KeyCode.M);
        }

        private void OnVideoSettingChanged(SettingChangedEvent<int> evt)
        {
            if (evt.SettingName == "ScreenWidth" || evt.SettingName == "ScreenHeight" || evt.SettingName == "TargetFrameRate")
            {
                ApplyGraphicsSettings();
            }
        }

        private void OnFullscreenModeChanged(SettingChangedEvent<FullScreenMode> evt)
        {
            ApplyGraphicsSettings();
        }

        private void OnVSyncChanged(SettingChangedEvent<bool> evt)
        {
            ApplyGraphicsSettings();
        }

        private void ApplyGraphicsSettings()
        {
            Screen.SetResolution(ScreenWidth.Value, ScreenHeight.Value, FullscreenMode.Value);
            QualitySettings.vSyncCount = VSync.Value ? 1 : 0;
            Application.targetFrameRate = VSync.Value ? -1 : TargetFrameRate.Value;
        }

        public void ResetAudioSettings()
        {
            MasterVolume.Reset();
            MusicVolume.Reset();
            SFXVolume.Reset();
            Events.Publish(new SettingsResetEvent { Category = SettingsCategory.Audio });
        }

        public void ResetVideoSettings()
        {
            ScreenWidth.Reset();
            ScreenHeight.Reset();
            TargetFrameRate.Reset();
            FullscreenMode.Reset();
            VSync.Reset();
            Events.Publish(new SettingsResetEvent { Category = SettingsCategory.Video });
        }

        public void ResetAllSettings()
        {
            ResetAudioSettings();
            ResetVideoSettings();
            Keybinds.ResetToDefaults();
            Events.Publish(new SettingsResetEvent { Category = SettingsCategory.All });
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

        // Serialization helpers
        public SettingsData ToSettingsData()
        {
            SettingsData data = new SettingsData
            {
                MasterVolume = MasterVolume.Value,
                MusicVolume = MusicVolume.Value,
                SFXVolume = SFXVolume.Value,
                ScreenWidth = ScreenWidth.Value,
                ScreenHeight = ScreenHeight.Value,
                TargetFrameRate = TargetFrameRate.Value,
                FullscreenMode = FullscreenMode.Value,
                VSync = VSync.Value,
                KeybindsList = Keybinds.GetKeybindsList()
            };
            return data;
        }

        public void FromSettingsData(SettingsData data)
        {
            MasterVolume.SetValue(data.MasterVolume);
            MusicVolume.SetValue(data.MusicVolume);
            SFXVolume.SetValue(data.SFXVolume);
            ScreenWidth.SetValue(data.ScreenWidth);
            ScreenHeight.SetValue(data.ScreenHeight);
            TargetFrameRate.SetValue(data.TargetFrameRate);
            FullscreenMode.SetValue(data.FullscreenMode);
            VSync.SetValue(data.VSync);
            Keybinds.LoadKeybinds(data.KeybindsList);
        }

        private void OnDestroy()
        {
            Events.Unsubscribe<SettingChangedEvent<int>>(OnVideoSettingChanged);
            Events.Unsubscribe<SettingChangedEvent<FullScreenMode>>(OnFullscreenModeChanged);
            Events.Unsubscribe<SettingChangedEvent<bool>>(OnVSyncChanged);
        }
    }

    // ===== Legacy Data Structure (for serialization compatibility) =====
    [Serializable]
    public class SettingsData
    {
        public List<KeybindPair> KeybindsList = new List<KeybindPair>();
        public float MusicVolume = 1f;
        public float SFXVolume = 1f;
        public float MasterVolume = 1f;
        public int ScreenWidth = 1920;
        public int ScreenHeight = 1080;
        public int TargetFrameRate = 60;
        public FullScreenMode FullscreenMode = FullScreenMode.FullScreenWindow;
        public bool VSync = true;
    }
}