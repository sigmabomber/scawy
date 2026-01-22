using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Doody.Settings;
using System.Collections.Generic;
using System.Linq;

public class SmartSettingsUI : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject settingsPanel;
    public Button closeButton;

    [Header("Tab System")]
    public Button audioTabButton;
    public Button videoTabButton;
    public Button controlsTabButton;
    public Button inputTabButton;
    public GameObject audioPanel;
    public GameObject videoPanel;
    public GameObject controlsPanel;
    public GameObject inputPanel;

    [Header("Smart Action Buttons")]
    public Button applyButton;
    public Button resetButton;
    public TMP_Text applyButtonText;
    public TMP_Text resetButtonText;

    [Header("Audio Settings")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public TMP_Text masterVolumeText;
    public TMP_Text musicVolumeText;
    public TMP_Text sfxVolumeText;

    [Header("Video Settings - Auto-populated")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fullscreenDropdown;
    public Toggle vSyncToggle;
    public Slider targetFpsSlider;
    public TMP_Text fpsText;

    [Header("Control Settings")]
    public Slider mouseSensitivitySlider;
    public Slider controllerSensitivitySlider;
    public Toggle invertYToggle;
    public Toggle invertXToggle;
    public Slider vibrationSlider;
    public TMP_Text mouseSensText;
    public TMP_Text controllerSensText;
    public TMP_Text vibrationText;

    [Header("Input Settings")]
    public GameObject keyboardBindingsPanel;
    public GameObject controllerBindingsPanel;
    public Transform keyboardBindingsContent;
    public Transform controllerBindingsContent;
    public GameObject inputBindPrefab;

    [Header("Controller Status")]
    public GameObject controllerConnectedIndicator;
    public GameObject controllerDisconnectedIndicator;
    public TMP_Text controllerNameText;
    public TMP_Text controllerTypeText;
    public TMP_Text inputModeText; // Shows "Keyboard/Mouse" or "Controller"

    [Header("Status Message")]
    public GameObject statusMessagePanel;
    public TMP_Text statusMessageText;
    public float statusMessageDuration = 2f;

    // Private variables
    private SettingsManager settings;
    private Resolution[] availableResolutions;
    private List<InputBindUI> keyboardBindUIElements = new List<InputBindUI>();
    private List<InputBindUI> controllerBindUIElements = new List<InputBindUI>();

    private SettingsCategory currentCategory = SettingsCategory.Audio;

    private bool audioModified = false;
    private bool videoModified = false;
    private bool controlsModified = false;
    private bool inputModified = false;

    // KeyCode and binding lists
    private List<KeyCode> allKeyCodes;
    private List<string> allControllerBindings;

    // Auto-populate flags
    private bool dropdownsPopulated = false;

    private void Awake()
    {
        settings = SettingsManager.Instance;
    }

    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("SettingsManager not found!");
            return;
        }

        // Get all available KeyCodes and bindings
        allKeyCodes = settings.GetAllKeyCodes();
        allControllerBindings = settings.GetAllControllerBindings();

        InitializeUI();

        // Auto-populate dropdowns
        AutoPopulateAllDropdowns();

        LoadCurrentSettings();

        // Subscribe to events
        settings.OnSettingsChanged += RefreshUI;
        settings.OnGraphicsSettingsApplied += OnGraphicsApplied;
        settings.OnInputDeviceChanged += OnInputDeviceChanged;

        ShowTab(audioPanel, SettingsCategory.Audio);

        // Initial input mode update
        UpdateInputModeDisplay();
    }

    private void OnDestroy()
    {
        if (settings != null)
        {
            settings.OnSettingsChanged -= RefreshUI;
            settings.OnGraphicsSettingsApplied -= OnGraphicsApplied;
            settings.OnInputDeviceChanged -= OnInputDeviceChanged;
        }
    }

    private void Update()
    {
        UpdateControllerStatus();
    }

    private void InitializeUI()
    {
        // Close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        // Tab buttons
        audioTabButton.onClick.AddListener(() => ShowTab(audioPanel, SettingsCategory.Audio));
        videoTabButton.onClick.AddListener(() => ShowTab(videoPanel, SettingsCategory.Video));
        controlsTabButton.onClick.AddListener(() => ShowTab(controlsPanel, SettingsCategory.Controls));
        inputTabButton.onClick.AddListener(() => ShowTab(inputPanel, SettingsCategory.Input));

        // Smart buttons
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // Audio sliders
        masterVolumeSlider.onValueChanged.AddListener((value) => {
            OnMasterVolumeChanged(value);
            audioModified = true;
            UpdateApplyButtonState();
        });
        musicVolumeSlider.onValueChanged.AddListener((value) => {
            OnMusicVolumeChanged(value);
            audioModified = true;
            UpdateApplyButtonState();
        });
        sfxVolumeSlider.onValueChanged.AddListener((value) => {
            OnSFXVolumeChanged(value);
            audioModified = true;
            UpdateApplyButtonState();
        });

        // Video settings - setup listeners AFTER auto-population
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener((index) => {
                OnResolutionChanged(index);
                videoModified = true;
                UpdateApplyButtonState();
            });

        if (fullscreenDropdown != null)
            fullscreenDropdown.onValueChanged.AddListener((index) => {
                OnFullscreenChanged(index);
                videoModified = true;
                UpdateApplyButtonState();
            });

        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener((value) => {
                OnVSyncChanged(value);
                videoModified = true;
                UpdateApplyButtonState();
            });

        if (targetFpsSlider != null)
            targetFpsSlider.onValueChanged.AddListener((value) => {
                OnTargetFPSChanged(value);
                videoModified = true;
                UpdateApplyButtonState();
            });

        // Control settings
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener((value) => {
                OnMouseSensitivityChanged(value);
                controlsModified = true;
                UpdateApplyButtonState();
            });

        if (controllerSensitivitySlider != null)
            controllerSensitivitySlider.onValueChanged.AddListener((value) => {
                OnControllerSensitivityChanged(value);
                controlsModified = true;
                UpdateApplyButtonState();
            });

        if (invertYToggle != null)
            invertYToggle.onValueChanged.AddListener((value) => {
                OnInvertYChanged(value);
                controlsModified = true;
                UpdateApplyButtonState();
            });

        if (invertXToggle != null)
            invertXToggle.onValueChanged.AddListener((value) => {
                OnInvertXChanged(value);
                controlsModified = true;
                UpdateApplyButtonState();
            });

        if (vibrationSlider != null)
            vibrationSlider.onValueChanged.AddListener((value) => {
                OnVibrationChanged(value);
                controlsModified = true;
                UpdateApplyButtonState();
            });

        // Hide status message
        if (statusMessagePanel != null)
            statusMessagePanel.SetActive(false);
    }

    private void AutoPopulateAllDropdowns()
    {
        if (dropdownsPopulated) return;

        Debug.Log("Auto-populating dropdowns...");

        // 1. Populate Resolution Dropdown
        AutoPopulateResolutionDropdown();

        // 2. Populate Fullscreen Mode Dropdown
        AutoPopulateFullscreenDropdown();

        // 3. Populate Target FPS Options (if using dropdown instead of slider)
        AutoPopulateFPSOptions();

        // 4. Populate Quality Settings Dropdown (optional)
        AutoPopulateQualityDropdown();

        dropdownsPopulated = true;
    }

    private void AutoPopulateResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("Resolution dropdown not assigned in inspector");
            return;
        }

        resolutionDropdown.ClearOptions();
        availableResolutions = Screen.resolutions;

        if (availableResolutions.Length == 0)
        {
            Debug.LogWarning("No resolutions available");
            resolutionDropdown.AddOptions(new List<string> { "1920x1080 @ 60Hz" });
            return;
        }

        var options = new List<string>();
        int currentIndex = 0;
        Resolution currentRes = Screen.currentResolution;

        // Filter unique resolutions (same width/height, keep highest refresh rate)
        var uniqueResolutions = new Dictionary<string, Resolution>();
        foreach (var res in availableResolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRate > uniqueResolutions[key].refreshRate)
            {
                uniqueResolutions[key] = res;
            }
        }

        // Sort by resolution size (largest first)
        var sortedResolutions = uniqueResolutions.Values.OrderByDescending(r => r.width * r.height).ThenByDescending(r => r.refreshRate).ToList();

        foreach (var res in sortedResolutions)
        {
            string option = $"{res.width} x {res.height} @ {res.refreshRate}Hz";
            options.Add(option);

            if (res.width == currentRes.width && res.height == currentRes.height)
            {
                currentIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);

        if (options.Count > 0)
        {
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.RefreshShownValue();
            Debug.Log($"Populated resolution dropdown with {options.Count} options. Current: {options[currentIndex]}");
        }
    }

    private void AutoPopulateFullscreenDropdown()
    {
        if (fullscreenDropdown == null)
        {
            Debug.LogWarning("Fullscreen dropdown not assigned in inspector");
            return;
        }

        fullscreenDropdown.ClearOptions();

        var options = new List<string>();

        // Get all available fullscreen modes from System.Enum
        foreach (FullScreenMode mode in System.Enum.GetValues(typeof(FullScreenMode)))
        {
            string modeName = mode.ToString();

            // Format the display name
            string displayName = modeName switch
            {
                "FullScreenWindow" => "Borderless Fullscreen",
                "MaximizedWindow" => "Maximized Window",
                "Windowed" => "Windowed",
                _ => modeName.Replace("FullScreen", "Fullscreen")
            };

            options.Add(displayName);
        }

        fullscreenDropdown.AddOptions(options);

        // Set current value
        int currentIndex = (int)Screen.fullScreenMode;
        if (currentIndex >= 0 && currentIndex < options.Count)
        {
            fullscreenDropdown.value = currentIndex;
            fullscreenDropdown.RefreshShownValue();
            Debug.Log($"Populated fullscreen dropdown. Current: {options[currentIndex]}");
        }
    }

    private void AutoPopulateFPSOptions()
    {
        // If you're using a dropdown for FPS instead of a slider, populate it here
        // Common FPS options: 30, 60, 75, 90, 120, 144, 165, 240, Unlimited

        // Example if you had a TMP_Dropdown fpsDropdown:
        /*
        if (fpsDropdown != null)
        {
            fpsDropdown.ClearOptions();
            var fpsOptions = new List<string> { "30", "60", "75", "90", "120", "144", "165", "240", "Unlimited" };
            fpsDropdown.AddOptions(fpsOptions);
            
            int currentFPS = Application.targetFrameRate;
            if (currentFPS <= 0)
                fpsDropdown.value = fpsOptions.Count - 1; // Unlimited
            else
            {
                int closestIndex = 0;
                int minDiff = int.MaxValue;
                for (int i = 0; i < fpsOptions.Count - 1; i++)
                {
                    if (int.TryParse(fpsOptions[i], out int fpsValue))
                    {
                        int diff = Mathf.Abs(currentFPS - fpsValue);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            closestIndex = i;
                        }
                    }
                }
                fpsDropdown.value = closestIndex;
            }
            fpsDropdown.RefreshShownValue();
        }
        */
    }

    private void AutoPopulateQualityDropdown()
    {
        // Optional: If you have a quality settings dropdown
        /*
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
            Debug.Log($"Populated quality dropdown with {QualitySettings.names.Length} levels");
        }
        */
    }

    private void ShowTab(GameObject tabPanel, SettingsCategory category)
    {
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
        controlsPanel.SetActive(false);
        inputPanel.SetActive(false);

        tabPanel.SetActive(true);
        currentCategory = category;
        UpdateButtonLabels();
        UpdateApplyButtonState();

        if (category == SettingsCategory.Input)
        {
            RefreshInputBindingsUI();
        }
    }

    private void UpdateButtonLabels()
    {
        if (applyButtonText == null || resetButtonText == null) return;

        switch (currentCategory)
        {
            case SettingsCategory.Audio:
                applyButtonText.text = "SAVE AUDIO";
                resetButtonText.text = "RESET AUDIO";
                break;
            case SettingsCategory.Video:
                applyButtonText.text = "APPLY VIDEO";
                resetButtonText.text = "RESET VIDEO";
                break;
            case SettingsCategory.Controls:
                applyButtonText.text = "SAVE CONTROLS";
                resetButtonText.text = "RESET CONTROLS";
                break;
            case SettingsCategory.Input:
                applyButtonText.text = "SAVE INPUT";
                resetButtonText.text = "RESET INPUT";
                break;
        }
    }

    private void UpdateApplyButtonState()
    {
        if (applyButton == null) return;

        bool hasModifications = currentCategory switch
        {
            SettingsCategory.Audio => audioModified,
            SettingsCategory.Video => videoModified,
            SettingsCategory.Controls => controlsModified,
            SettingsCategory.Input => inputModified,
            _ => false
        };

        applyButton.interactable = hasModifications;

        ColorBlock colors = applyButton.colors;
        colors.normalColor = hasModifications ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.5f);
        applyButton.colors = colors;
    }

    private void OnApplyClicked()
    {
        if (settings == null) return;

        settings.ApplyCurrentCategory(currentCategory);
        ShowStatusMessage($"{applyButtonText.text} successful!");

        switch (currentCategory)
        {
            case SettingsCategory.Audio: audioModified = false; break;
            case SettingsCategory.Video: videoModified = false; break;
            case SettingsCategory.Controls: controlsModified = false; break;
            case SettingsCategory.Input: inputModified = false; break;
        }

        UpdateApplyButtonState();
        RefreshUI();
    }

    private void OnResetClicked()
    {
        if (settings == null) return;

        settings.ResetCurrentCategory(currentCategory);
        ShowStatusMessage($"{resetButtonText.text} successful!");

        switch (currentCategory)
        {
            case SettingsCategory.Audio: audioModified = false; break;
            case SettingsCategory.Video: videoModified = false; break;
            case SettingsCategory.Controls: controlsModified = false; break;
            case SettingsCategory.Input: inputModified = false; break;
        }

        UpdateApplyButtonState();
        RefreshUI();
    }

    private void ShowStatusMessage(string message)
    {
        if (statusMessagePanel == null || statusMessageText == null) return;

        statusMessageText.text = message;
        statusMessagePanel.SetActive(true);
        CancelInvoke(nameof(HideStatusMessage));
        Invoke(nameof(HideStatusMessage), statusMessageDuration);
    }

    private void HideStatusMessage()
    {
        if (statusMessagePanel != null)
            statusMessagePanel.SetActive(false);
    }

    private void OnInputDeviceChanged(bool usingController)
    {
        UpdateInputModeDisplay();

        if (currentCategory == SettingsCategory.Input)
        {
            RefreshInputBindingsUI();
        }
    }

    private void UpdateInputModeDisplay()
    {
        if (inputModeText == null) return;

        if (settings.UsingController)
        {
            inputModeText.text = $"CONTROLLER MODE ({settings.GetControllerType()})";
        }
        else
        {
            inputModeText.text = "KEYBOARD/MOUSE MODE";
        }
    }

    private void UpdateControllerStatus()
    {
        if (settings == null) return;

        bool connected = settings.IsControllerConnected();

        if (controllerConnectedIndicator != null)
            controllerConnectedIndicator.SetActive(connected);
        if (controllerDisconnectedIndicator != null)
            controllerDisconnectedIndicator.SetActive(!connected);

        if (controllerNameText != null)
        {
            if (InputDetector.Instance != null)
            {
                controllerNameText.text = connected ? InputDetector.Instance.GetControllerName() : "No Controller";
            }
            else
            {
                controllerNameText.text = connected ? settings.GetControllerType().ToString() : "No Controller";
            }
        }

        if (controllerTypeText != null)
        {
            controllerTypeText.text = connected ? settings.GetControllerType().ToString() : "Disconnected";
        }
    }

    private void LoadCurrentSettings()
    {
        if (settings == null) return;

        // Audio
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = settings.MasterVolume.Value;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = settings.MusicVolume.Value;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = settings.SFXVolume.Value;
        UpdateVolumeTexts();

        // Video - dropdowns are already populated, just set values
        if (vSyncToggle != null)
            vSyncToggle.isOn = settings.VSync.Value;
        if (targetFpsSlider != null)
            targetFpsSlider.value = settings.TargetFrameRate.Value;
        if (fpsText != null)
            fpsText.text = $"Target FPS: {settings.TargetFrameRate.Value}";

        // Update resolution dropdown to match current settings
        UpdateResolutionDropdownToCurrent();

        // Update fullscreen dropdown to match current settings
        UpdateFullscreenDropdownToCurrent();

        // Controls
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = settings.MouseSensitivity.Value;
        if (controllerSensitivitySlider != null)
            controllerSensitivitySlider.value = settings.ControllerSensitivity.Value;
        if (invertYToggle != null)
            invertYToggle.isOn = settings.InvertYAxis.Value;
        if (invertXToggle != null)
            invertXToggle.isOn = settings.InvertXAxis.Value;
        if (vibrationSlider != null)
            vibrationSlider.value = settings.VibrationIntensity.Value;
        UpdateControlTexts();

        UpdateButtonLabels();

        audioModified = false;
        videoModified = false;
        controlsModified = false;
        inputModified = false;
        UpdateApplyButtonState();

        UpdateInputModeDisplay();
        RefreshInputBindingsUI();
    }

    private void UpdateResolutionDropdownToCurrent()
    {
        if (resolutionDropdown == null || availableResolutions == null) return;

        int currentWidth = settings.ScreenWidth.Value;
        int currentHeight = settings.ScreenHeight.Value;

        // Find the matching resolution in dropdown
        for (int i = 0; i < resolutionDropdown.options.Count; i++)
        {
            string optionText = resolutionDropdown.options[i].text;
            // Parse resolution from text like "1920 x 1080 @ 60Hz"
            if (optionText.Contains(currentWidth.ToString()) && optionText.Contains(currentHeight.ToString()))
            {
                resolutionDropdown.value = i;
                resolutionDropdown.RefreshShownValue();
                break;
            }
        }
    }

    private void UpdateFullscreenDropdownToCurrent()
    {
        if (fullscreenDropdown == null) return;

        FullScreenMode currentMode = settings.FullscreenMode.Value;
        int modeIndex = (int)currentMode;

        if (modeIndex >= 0 && modeIndex < fullscreenDropdown.options.Count)
        {
            fullscreenDropdown.value = modeIndex;
            fullscreenDropdown.RefreshShownValue();
        }
    }

    private void RefreshUI()
    {
        LoadCurrentSettings();
    }

    private void RefreshInputBindingsUI()
    {
        if (settings == null) return;

        // Clear existing
        foreach (var ui in keyboardBindUIElements)
            if (ui != null) Destroy(ui.gameObject);
        foreach (var ui in controllerBindUIElements)
            if (ui != null) Destroy(ui.gameObject);

        keyboardBindUIElements.Clear();
        controllerBindUIElements.Clear();

        // Get binds based on current input mode
        bool usingController = settings.UsingController;

        if (!usingController)
        {
            // Show keyboard bindings
            var keyboardBinds = settings.InputBinds.GetInputBindsForDevice(false);

            foreach (var bind in keyboardBinds)
            {
                if (inputBindPrefab == null || keyboardBindingsContent == null) continue;

                GameObject go = Instantiate(inputBindPrefab, keyboardBindingsContent);
                InputBindUI ui = go.GetComponent<InputBindUI>();
                if (ui != null)
                {
                    string displayName = settings.GetKeyCodeDisplayName(bind.keyboardKey);
                    string spriteName = settings.GetBindingSpriteName(bind.keyboardKey.ToString());

                    ui.SetupForKeyboard(bind.action, displayName, spriteName,
                        (newKey) => {
                            OnKeyboardBindChanged(bind.action, newKey);
                            inputModified = true;
                            UpdateApplyButtonState();
                        },
                        allKeyCodes);
                    keyboardBindUIElements.Add(ui);
                }
            }
        }
        else
        {
            // Show controller bindings
            var controllerBinds = settings.InputBinds.GetInputBindsForDevice(true);

            foreach (var bind in controllerBinds)
            {
                if (inputBindPrefab == null || controllerBindingsContent == null) continue;

                GameObject go = Instantiate(inputBindPrefab, controllerBindingsContent);
                InputBindUI ui = go.GetComponent<InputBindUI>();
                if (ui != null)
                {
                    string displayName = settings.GetBindingDisplayName(bind.controllerBinding);
                    string spriteName = settings.GetBindingSpriteName(bind.controllerBinding);

                    ui.SetupForController(bind.action, displayName, spriteName,
                        (newBinding) => {
                            OnControllerBindChanged(bind.action, newBinding);
                            inputModified = true;
                            UpdateApplyButtonState();
                        },
                        allControllerBindings);
                    controllerBindUIElements.Add(ui);
                }
            }
        }

        // Show correct panel
        if (keyboardBindingsPanel != null)
            keyboardBindingsPanel.SetActive(!usingController);
        if (controllerBindingsPanel != null)
            controllerBindingsPanel.SetActive(usingController);
    }

    // Event Handlers
    private void OnMasterVolumeChanged(float value)
    {
        if (settings == null) return;
        settings.MasterVolume.Value = value;
        if (masterVolumeText != null)
            masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (settings == null) return;
        settings.MusicVolume.Value = value;
        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (settings == null) return;
        settings.SFXVolume.Value = value;
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnResolutionChanged(int index)
    {
        if (settings == null || availableResolutions == null) return;

        // We need to parse the selected resolution from the dropdown text
        if (index >= 0 && index < resolutionDropdown.options.Count)
        {
            string selectedText = resolutionDropdown.options[index].text;
            // Parse "1920 x 1080 @ 60Hz"
            string[] parts = selectedText.Split(new[] { " x ", " @ " }, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    settings.ScreenWidth.Value = width;
                    settings.ScreenHeight.Value = height;

                    // Try to get refresh rate if available
                    if (parts.Length >= 3 && parts[2].EndsWith("Hz"))
                    {
                        string refreshStr = parts[2].Replace("Hz", "");
                        if (int.TryParse(refreshStr, out int refreshRate))
                        {
                            // Store refresh rate if your SettingsManager supports it
                        }
                    }
                }
            }
        }
    }

    private void OnFullscreenChanged(int index)
    {
        if (settings == null) return;

        if (index >= 0 && index < System.Enum.GetValues(typeof(FullScreenMode)).Length)
        {
            settings.FullscreenMode.Value = (FullScreenMode)index;
        }
    }

    private void OnVSyncChanged(bool value)
    {
        if (settings == null) return;
        settings.VSync.Value = value;
    }

    private void OnTargetFPSChanged(float value)
    {
        if (settings == null) return;
        int intValue = Mathf.RoundToInt(value);
        settings.TargetFrameRate.Value = intValue;
        if (fpsText != null)
            fpsText.text = $"Target FPS: {intValue}";
    }

    private void OnMouseSensitivityChanged(float value)
    {
        if (settings == null) return;
        settings.MouseSensitivity.Value = value;
        if (mouseSensText != null)
            mouseSensText.text = $"{value:F1}";
    }

    private void OnControllerSensitivityChanged(float value)
    {
        if (settings == null) return;
        settings.ControllerSensitivity.Value = value;
        if (controllerSensText != null)
            controllerSensText.text = $"{value:F1}";
    }

    private void OnInvertYChanged(bool value)
    {
        if (settings == null) return;
        settings.InvertYAxis.Value = value;
    }

    private void OnInvertXChanged(bool value)
    {
        if (settings == null) return;
        settings.InvertXAxis.Value = value;
    }

    private void OnVibrationChanged(float value)
    {
        if (settings == null) return;
        settings.VibrationIntensity.Value = value;
        if (vibrationText != null)
            vibrationText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnKeyboardBindChanged(string action, string newKeyString)
    {
        if (settings == null) return;
        if (System.Enum.TryParse<KeyCode>(newKeyString, out KeyCode newKey))
        {
            settings.InputBinds.SetKeyboardBind(action, newKey);
        }
    }

    private void OnControllerBindChanged(string action, string newBinding)
    {
        if (settings == null) return;
        settings.InputBinds.SetControllerBind(action, newBinding);
        RefreshInputBindingsUI();
    }

    private void OnGraphicsApplied()
    {
        ShowStatusMessage("Graphics settings applied!");
    }

    private void UpdateVolumeTexts()
    {
        if (settings == null) return;

        if (masterVolumeText != null)
            masterVolumeText.text = $"{Mathf.RoundToInt(settings.MasterVolume.Value * 100)}%";
        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(settings.MusicVolume.Value * 100)}%";
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(settings.SFXVolume.Value * 100)}%";
    }

    private void UpdateControlTexts()
    {
        if (settings == null) return;

        if (mouseSensText != null)
            mouseSensText.text = $"{settings.MouseSensitivity.Value:F1}";
        if (controllerSensText != null)
            controllerSensText.text = $"{settings.ControllerSensitivity.Value:F1}";
        if (vibrationText != null)
            vibrationText.text = $"{Mathf.RoundToInt(settings.VibrationIntensity.Value * 100)}%";
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // Ensure dropdowns are populated
            if (!dropdownsPopulated)
                AutoPopulateAllDropdowns();

            LoadCurrentSettings();
            UpdateControllerStatus();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            bool isActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isActive);

            if (!isActive)
            {
                // Ensure dropdowns are populated
                if (!dropdownsPopulated)
                    AutoPopulateAllDropdowns();

                LoadCurrentSettings();
                UpdateControllerStatus();
            }
        }
    }

    // Public method to force refresh dropdowns (useful if screen resolution changes)
    public void RefreshDropdowns()
    {
        dropdownsPopulated = false;
        AutoPopulateAllDropdowns();
        UpdateResolutionDropdownToCurrent();
        UpdateFullscreenDropdownToCurrent();
    }
}