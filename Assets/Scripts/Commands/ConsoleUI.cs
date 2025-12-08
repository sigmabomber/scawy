using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using Doody.Debugging; 

public class ConsoleUI : MonoBehaviour
{
    public static ConsoleUI Instance { get; private set; }

    [Header("Main UI Components")]
    [SerializeField] public GameObject consolePanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ScrollRect outputScrollRect;
    [SerializeField] private RectTransform outputContent;
    [SerializeField] private Image backgroundImage;

    [Header("Suggestions UI")]
    [SerializeField] private GameObject suggestionsPanel;
    [SerializeField] private RectTransform suggestionsContent;
    [SerializeField] private TMP_Text suggestionPrefab;
    [SerializeField] private int maxSuggestions = 5;
    [SerializeField] private Color suggestionNormalColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color suggestionSelectedColor = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private Color suggestionDescriptionColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private int maxOutputLines = 100;
    [SerializeField] private bool showCommandDescriptions = true;
    [SerializeField] private float suggestionUpdateDelay = 0.1f;
    [SerializeField] private bool enableCommandSuggestions = true;

    [Header("Colors")]
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.4f);
    [SerializeField] private Color systemColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color inputColor = new Color(0.8f, 0.8f, 1f);

    [Header("Scroll Settings")]
    [SerializeField] private bool autoScrollToBottom = true;
    [SerializeField] private bool scrollWithMouseWheel = true;
    [SerializeField] private float mouseWheelSensitivity = 0.1f;

    // Registry cache
    private CommandRegistry cachedRegistry = null;
    private float lastRegistryCheckTime = 0f;
    private const float REGISTRY_CHECK_INTERVAL = 1f;

    // Private fields
    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private string currentInput = "";
    private string lastInputForSuggestions = "";
    private float lastSuggestionUpdateTime = 0f;

    // Suggestion system
    private List<TMP_Text> suggestionItems = new List<TMP_Text>();
    private List<CommandRegistry.CommandData> currentSuggestions = new List<CommandRegistry.CommandData>();
    private int selectedSuggestionIndex = -1;
    private bool isSuggestionPanelActive = false;

    // Scroll system
    private const float SCROLL_UPDATE_DELAY = 0.05f;
    private bool isDraggingScrollbar = false;
    private float lastScrollbarValue = 0f;
    private bool isMouseOverScrollbar = false;
    private bool userHasScrolled = false; 

    // Performance optimization
    private bool isInitialized = false;
    private bool isCommandRegistryAvailable = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeConsole();
    }

    void Start()
    {
        if (startHidden)
        {
            CloseConsole();
        }
        else
        {
            OpenConsole();
        }

        if (outputScrollRect != null && outputContent == null)
        {
            outputContent = outputScrollRect.content;
        }

        InitializeSuggestions();
        isInitialized = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }

        if (consolePanel != null && consolePanel.activeInHierarchy)
        {
            HandleConsoleInput();
            HandleMouseScrollbarInteraction();

            if (enableCommandSuggestions)
            {
                var registry = GetCommandRegistry();
                if (registry != null)
                {
                    UpdateSuggestions();
                }
            }
        }

        if (outputScrollRect != null)
        {
            UpdateScrollContent();
        }
    }

    private CommandRegistry GetCommandRegistry()
    {
        if (cachedRegistry != null)
            return cachedRegistry;

        if (Time.time - lastRegistryCheckTime < REGISTRY_CHECK_INTERVAL)
            return null;

        lastRegistryCheckTime = Time.time;
        cachedRegistry = FindObjectOfType<CommandRegistry>();

        if (cachedRegistry != null)
        {
            isCommandRegistryAvailable = true;
        }

        return cachedRegistry;
    }

    private void HandleConsoleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (isSuggestionPanelActive && selectedSuggestionIndex >= 0)
            {
                NavigateSuggestions(-1);
            }
            else
            {
                NavigateHistory(-1);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (isSuggestionPanelActive && currentSuggestions.Count > 0)
            {
                NavigateSuggestions(1);
            }
            else
            {
                NavigateHistory(1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isSuggestionPanelActive && selectedSuggestionIndex >= 0)
            {
                ApplySelectedSuggestion();
            }
            else if (isCommandRegistryAvailable)
            {
                ApplyTabCompletion();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isSuggestionPanelActive)
            {
                HideSuggestions();
            }
            else if (!string.IsNullOrEmpty(inputField.text))
            {
                inputField.text = "";
                HideSuggestions();
            }
            else
            {
                CloseConsole();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isSuggestionPanelActive && selectedSuggestionIndex >= 0)
            {
                ApplySelectedSuggestion();
                return;
            }
        }

        if (scrollWithMouseWheel && outputScrollRect != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                userHasScrolled = true;
                autoScrollToBottom = false;

                outputScrollRect.verticalNormalizedPosition += scroll * mouseWheelSensitivity;
                outputScrollRect.verticalNormalizedPosition = Mathf.Clamp01(outputScrollRect.verticalNormalizedPosition);
                lastScrollbarValue = outputScrollRect.verticalNormalizedPosition;
            }
        }

        if (!inputField.isFocused)
        {
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
    }

    private void HandleMouseScrollbarInteraction()
    {
        if (outputScrollRect == null || outputScrollRect.verticalScrollbar == null)
            return;

        RectTransform scrollbarRect = outputScrollRect.verticalScrollbar.GetComponent<RectTransform>();
        if (scrollbarRect == null) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 localMousePos = scrollbarRect.InverseTransformPoint(mousePos);

        isMouseOverScrollbar = scrollbarRect.rect.Contains(localMousePos);

        if (Input.GetMouseButtonDown(0) && isMouseOverScrollbar)
        {
            isDraggingScrollbar = true;
            userHasScrolled = true;
            autoScrollToBottom = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDraggingScrollbar = false;
        }

        if (isDraggingScrollbar)
        {
            lastScrollbarValue = outputScrollRect.verticalNormalizedPosition;
        }
    }

    private void InitializeConsole()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmitCommand);
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.onSelect.AddListener(OnInputFieldSelected);
            inputField.onDeselect.AddListener(OnInputFieldDeselected);
        }

        if (outputText != null)
        {
            outputText.text = "";
            outputText.overflowMode = TextOverflowModes.Overflow;
            outputText.enableWordWrapping = true;
            outputText.raycastTarget = false;
        }

        if (outputScrollRect != null)
        {
            if (outputContent == null)
            {
                outputContent = outputScrollRect.content;
            }

            outputScrollRect.vertical = true;
            outputScrollRect.horizontal = false;
            outputScrollRect.movementType = ScrollRect.MovementType.Clamped;
            outputScrollRect.scrollSensitivity = 30f;

            if (outputScrollRect.verticalScrollbar != null)
            {
                outputScrollRect.verticalScrollbar.gameObject.SetActive(true);
            }

            var layoutGroup = outputContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = outputContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 2;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;

            var sizeFitter = outputContent.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = outputContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            outputContent.anchorMin = new Vector2(0, 1);
            outputContent.anchorMax = new Vector2(1, 1);
            outputContent.pivot = new Vector2(0, 1);
        }

        AddOutputLine("=== Debug Console ===", systemColor);
        AddOutputLine("Type 'help' for available commands", systemColor);
        AddOutputLine("Press '`' to show/hide console", systemColor);
        AddOutputLine("Type part of a command and press Tab for suggestions", systemColor);
    }

    private void InitializeSuggestions()
    {
        if (suggestionPrefab == null || suggestionsContent == null)
            return;

        for (int i = 0; i < maxSuggestions; i++)
        {
            TMP_Text suggestion = Instantiate(suggestionPrefab, suggestionsContent);
            suggestion.gameObject.SetActive(false);
            suggestionItems.Add(suggestion);

            var button = suggestion.gameObject.AddComponent<Button>();
            int index = i;
            button.onClick.AddListener(() => OnSuggestionClicked(index));

            var trigger = suggestion.gameObject.AddComponent<EventTrigger>();
            var entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { OnSuggestionHoverEnter(index); });
            trigger.triggers.Add(entryEnter);
        }

        HideSuggestions();
    }

    private void UpdateSuggestions()
    {
        if (!enableCommandSuggestions || !consolePanel.activeInHierarchy || inputField == null || !inputField.isFocused)
        {
            if (isSuggestionPanelActive)
                HideSuggestions();
            return;
        }

        if (cachedRegistry == null)
        {
            HideSuggestions();
            return;
        }

        string currentText = inputField.text;
        if (currentText == lastInputForSuggestions && Time.time - lastSuggestionUpdateTime < suggestionUpdateDelay)
            return;

        lastInputForSuggestions = currentText;
        lastSuggestionUpdateTime = Time.time;

        if (string.IsNullOrWhiteSpace(currentText))
        {
            HideSuggestions();
            return;
        }

        try
        {
            var suggestions = cachedRegistry.GetCommandSuggestions(currentText)
                .Take(maxSuggestions)
                .ToList();

            if (suggestions.Count == 0)
            {
                HideSuggestions();
                return;
            }

            currentSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                var cmd = cachedRegistry.GetCommand(suggestion);
                if (cmd != null)
                    currentSuggestions.Add(cmd);
            }

            UpdateSuggestionDisplay();
            ShowSuggestions();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating suggestions: {e.Message}");
            HideSuggestions();
        }
    }

    private void UpdateSuggestionDisplay()
    {
        if (suggestionsPanel == null || suggestionItems.Count == 0)
            return;

        for (int i = 0; i < suggestionItems.Count; i++)
        {
            var suggestionItem = suggestionItems[i];

            if (i < currentSuggestions.Count)
            {
                var command = currentSuggestions[i];

                if (showCommandDescriptions && !string.IsNullOrEmpty(command.description))
                {
                    suggestionItem.text = $"{command.name} - <color=#{ColorUtility.ToHtmlStringRGB(suggestionDescriptionColor)}>{command.description}</color>";
                }
                else
                {
                    suggestionItem.text = command.name;
                }

                suggestionItem.color = (i == selectedSuggestionIndex) ? suggestionSelectedColor : suggestionNormalColor;
                suggestionItem.gameObject.SetActive(true);
            }
            else
            {
                suggestionItem.gameObject.SetActive(false);
            }
        }

        if (selectedSuggestionIndex >= currentSuggestions.Count)
        {
            selectedSuggestionIndex = currentSuggestions.Count - 1;
        }
    }

    private void ShowSuggestions()
    {
        if (suggestionsPanel != null && currentSuggestions.Count > 0)
        {
            suggestionsPanel.SetActive(true);
            isSuggestionPanelActive = true;

            if (inputField != null)
            {
                RectTransform inputRect = inputField.GetComponent<RectTransform>();
                RectTransform panelRect = suggestionsPanel.GetComponent<RectTransform>();

                if (inputRect != null && panelRect != null)
                {
                    Vector3 position = inputRect.position;
                    position.y -= inputRect.rect.height;
                    panelRect.position = position;
                    panelRect.SetParent(inputField.transform.parent, true);
                }
            }

            selectedSuggestionIndex = -1;
        }
    }

    private void HideSuggestions()
    {
        if (suggestionsPanel != null)
        {
            suggestionsPanel.SetActive(false);
            isSuggestionPanelActive = false;
            selectedSuggestionIndex = -1;
        }
    }

    private void NavigateSuggestions(int direction)
    {
        if (currentSuggestions.Count == 0)
            return;

        selectedSuggestionIndex += direction;

        if (selectedSuggestionIndex < 0)
            selectedSuggestionIndex = currentSuggestions.Count - 1;
        else if (selectedSuggestionIndex >= currentSuggestions.Count)
            selectedSuggestionIndex = 0;

        UpdateSuggestionDisplay();
    }

    private void ApplySelectedSuggestion()
    {
        if (selectedSuggestionIndex < 0 || selectedSuggestionIndex >= currentSuggestions.Count)
            return;

        var command = currentSuggestions[selectedSuggestionIndex];
        inputField.text = command.name + " ";
        inputField.caretPosition = inputField.text.Length;
        HideSuggestions();
    }

    private void ApplyTabCompletion()
    {
        if (cachedRegistry == null || string.IsNullOrEmpty(inputField.text))
            return;

        string completed = cachedRegistry.GetTabCompletion(inputField.text);
        if (!string.IsNullOrEmpty(completed) && completed != inputField.text)
        {
            inputField.text = completed + " ";
            inputField.caretPosition = inputField.text.Length;
        }
    }

    private void OnSuggestionClicked(int index)
    {
        selectedSuggestionIndex = index;
        ApplySelectedSuggestion();
    }

    private void OnSuggestionHoverEnter(int index)
    {
        selectedSuggestionIndex = index;
        UpdateSuggestionDisplay();
    }

    private void OnInputFieldSelected(string text = "")
    {
        lastInputForSuggestions = "";
        lastSuggestionUpdateTime = 0f;
    }

    private void OnInputFieldDeselected(string text = "")
    {
        Invoke("HideSuggestionsDelayed", 0.1f);
    }

    private void HideSuggestionsDelayed()
    {
        if (!inputField.isFocused)
        {
            HideSuggestions();
        }
    }

    public void ToggleConsole()
    {
        if (consolePanel.activeSelf)
        {
            CloseConsole();
        }
        else
        {
            OpenConsole();
        }
    }

    public void OpenConsole()
    {
        consolePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (cachedRegistry == null)
        {
            StartCoroutine(FindRegistryCoroutine());
        }

        Canvas.ForceUpdateCanvases();
        if (outputScrollRect != null)
        {
            outputScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private System.Collections.IEnumerator FindRegistryCoroutine()
    {
        cachedRegistry = FindObjectOfType<CommandRegistry>();

        if (cachedRegistry != null)
        {
            isCommandRegistryAvailable = true;
            yield break;
        }

        yield return null;

        cachedRegistry = FindObjectOfType<CommandRegistry>();

        if (cachedRegistry != null)
        {
            isCommandRegistryAvailable = true;
        }
    }

    public void CloseConsole()
    {
        consolePanel.SetActive(false);
        HideSuggestions();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private async void OnSubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        HideSuggestions();
        AddOutputLine($"> {command}", inputColor);
        AddToHistory(command);

        bool success = false;
        if (cachedRegistry != null)
        {
            success = await cachedRegistry.ExecuteCommandAsync(command);
        }
        else
        {
            AddOutputLine("Command system not initialized!", errorColor);
        }

        cachedRegistry?.ResetTabCompletion();

        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();

        historyIndex = -1;

        if (success)
        {
            userHasScrolled = false;
            autoScrollToBottom = true;
        }
    }

    private void OnInputChanged(string text)
    {
        currentInput = text;

        if (isSuggestionPanelActive)
        {
            UpdateSuggestions();
        }
    }

    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0)
            return;

        HideSuggestions();

        if (historyIndex == -1 && !string.IsNullOrEmpty(inputField.text))
        {
            currentInput = inputField.text;
        }

        historyIndex += direction;
        historyIndex = Mathf.Clamp(historyIndex, -1, commandHistory.Count - 1);

        if (historyIndex == -1)
        {
            inputField.text = currentInput;
        }
        else
        {
            inputField.text = commandHistory[commandHistory.Count - 1 - historyIndex];
        }

        inputField.caretPosition = inputField.text.Length;
    }

    private void AddToHistory(string command)
    {
        if (commandHistory.Count > 0 && commandHistory[commandHistory.Count - 1] == command)
            return;

        commandHistory.Add(command);

        if (commandHistory.Count > 50)
        {
            commandHistory.RemoveAt(0);
        }
    }

    #region Public Output Methods

    public void AddOutputLine(string text, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string coloredText = $"<color=#{colorHex}>{EscapeRichText(text)}</color>";

        outputLines.Add(coloredText);

        if (outputLines.Count > maxOutputLines)
        {
            outputLines.RemoveAt(0);
        }

        UpdateOutputDisplay();
    }

    public void AddOutputLine(string text)
    {
        AddOutputLine(text, defaultTextColor);
    }

    public void Log(string text)
    {
        AddOutputLine(text, defaultTextColor);
    }

    public void LogSuccess(string text)
    {
        AddOutputLine(text, successColor);
    }

    public void LogError(string text)
    {
        AddOutputLine(text, errorColor);
    }

    public void LogWarning(string text)
    {
        AddOutputLine(text, warningColor);
    }

    public void LogSystem(string text)
    {
        AddOutputLine(text, systemColor);
    }

    #endregion

    private void UpdateOutputDisplay()
    {
        if (outputText != null)
        {
            outputText.text = string.Join("\n", outputLines);
            outputText.ForceMeshUpdate();
        }
    }

    private void UpdateScrollContent()
    {
        if (outputScrollRect == null || outputContent == null || outputText == null)
            return;

        Canvas.ForceUpdateCanvases();

        float preferredHeight = outputText.preferredHeight;
        float padding = 20f;

        float viewportHeight = outputScrollRect.viewport.rect.height;
        float contentHeight = Mathf.Max(preferredHeight + padding, viewportHeight);

        outputContent.sizeDelta = new Vector2(outputContent.sizeDelta.x, contentHeight);

        RectTransform textRect = outputText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0, preferredHeight);
        }

        if (autoScrollToBottom && !isDraggingScrollbar && !isMouseOverScrollbar && !userHasScrolled)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.verticalNormalizedPosition = 0f;
            lastScrollbarValue = 0f;
        }
    }

    private string EscapeRichText(string text)
    {
        return text;
    }

    public void ClearOutput()
    {
        outputLines.Clear();
        UpdateOutputDisplay();
        LogSystem("Console cleared.");
        autoScrollToBottom = true;
        userHasScrolled = false;
    }

    public void ScrollToBottom()
    {
        if (outputScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.verticalNormalizedPosition = 0f;
            autoScrollToBottom = true;
            userHasScrolled = false;
        }
    }

    // Static helper methods
    public static void Print(string message)
    {
        print(message);
        if (Instance != null)
        {
            Instance.Log(message);
        }
    }

    public static void PrintSuccess(string message)
    {
        if (Instance != null)
        {
            Instance.LogSuccess(message);
        }
    }

    public static void PrintWarning(string message)
    {
        if (Instance != null)
        {
            Instance.LogWarning(message);
        }
    }

    public static void PrintError(string message)
    {
        if (Instance != null)
        {
            Instance.LogError(message);
        }
    }

    public static void PrintSystem(string message)
    {
        if (Instance != null)
        {
            Instance.LogSystem(message);
        }
    }
}