using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ConsoleUI : MonoBehaviour
{
    public static ConsoleUI Instance { get; private set; }

    [Header("Main UI Components")]
    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ScrollRect outputScrollRect;
    [SerializeField] private RectTransform outputContent;
    [SerializeField] private Image backgroundImage;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` key
    [SerializeField] private bool startHidden = true;
    [SerializeField] private int maxOutputLines = 100;

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
    [SerializeField] private float lineHeight = 24f; // Approximate height of one line

    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private string currentInput = "";
    private bool needsScrollUpdate = false;

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

        // Ensure content is properly set up
        if (outputScrollRect != null && outputContent == null)
        {
            outputContent = outputScrollRect.content;
        }
    }

    void Update()
    {
        // Toggle console
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }

        // Handle console-specific input
        if (consolePanel.activeInHierarchy)
        {
            // Command history navigation
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateHistory(1);
            }

            // Clear input with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    inputField.text = "";
                }
                else
                {
                    CloseConsole();
                }
            }

            // Mouse wheel scrolling
            if (scrollWithMouseWheel && outputScrollRect != null)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    outputScrollRect.verticalNormalizedPosition += scroll * mouseWheelSensitivity;
                    outputScrollRect.verticalNormalizedPosition = Mathf.Clamp01(outputScrollRect.verticalNormalizedPosition);
                }
            }

            // Auto-focus input field
            if (!inputField.isFocused)
            {
                if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
                {
                    inputField.Select();
                    inputField.ActivateInputField();
                }
            }
        }

        // Update scroll if needed
        if (needsScrollUpdate && outputScrollRect != null)
        {
            UpdateScrollContent();
            needsScrollUpdate = false;
        }
    }

    void LateUpdate()
    {
        // Ensure scrolling works properly
        if (autoScrollToBottom && outputScrollRect != null && outputContent != null)
        {
            // Check if we're near the bottom (within 0.01 of bottom)
            if (outputScrollRect.verticalNormalizedPosition < 0.01f)
            {
                outputScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    private void InitializeConsole()
    {
        // Set up input field callback
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmitCommand);
            inputField.onValueChanged.AddListener(OnInputChanged);
        }

        // Set up output text
        if (outputText != null)
        {
            outputText.text = "";
            outputText.overflowMode = TextOverflowModes.Overflow;
            outputText.enableWordWrapping = true;
        }

        // Ensure content is properly set up
        if (outputScrollRect != null && outputContent == null)
        {
            outputContent = outputScrollRect.content;
        }

        // Add welcome message
        AddOutputLine("=== Debug Console ===", systemColor);
        AddOutputLine("Type 'help' for available commands", systemColor);
        AddOutputLine("Press '`' to show/hide console", systemColor);
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

        // Set cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Focus input field
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        // Force update of scroll content
        needsScrollUpdate = true;
    }

    public void CloseConsole()
    {
        consolePanel.SetActive(false);

        // Restore cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnSubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Add to output with input styling
        AddOutputLine($"> {command}", inputColor);

        // Add to command history
        AddToHistory(command);

        // Execute the command
        bool success = ExecuteCommand(command);

        // Clear input field
        inputField.text = "";

        // Refocus input field
        inputField.Select();
        inputField.ActivateInputField();

        // Reset history navigation
        historyIndex = -1;
    }

    private void OnInputChanged(string text)
    {
        currentInput = text;
    }

    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0)
            return;

        // Save current input if we're starting navigation
        if (historyIndex == -1 && !string.IsNullOrEmpty(inputField.text))
        {
            currentInput = inputField.text;
        }

        historyIndex += direction;
        historyIndex = Mathf.Clamp(historyIndex, -1, commandHistory.Count - 1);

        if (historyIndex == -1)
        {
            // Back to current input
            inputField.text = currentInput;
        }
        else
        {
            // Set to history item
            inputField.text = commandHistory[commandHistory.Count - 1 - historyIndex];
        }

        // Move cursor to end
        inputField.caretPosition = inputField.text.Length;
    }

    private void AddToHistory(string command)
    {
        commandHistory.Add(command);

        // Limit history size
        if (commandHistory.Count > 50)
        {
            commandHistory.RemoveAt(0);
        }
    }

    private bool ExecuteCommand(string command)
    {
        if (CommandRegistry.Instance != null)
        {
            return CommandRegistry.Instance.ExecuteCommand(command);
        }

        AddOutputLine("Command system not initialized!", errorColor);
        return false;
    }

    #region Public Output Methods

    public void AddOutputLine(string text, Color color)
    {
        // Convert color to hex for rich text
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string coloredText = $"<color=#{colorHex}>{EscapeRichText(text)}</color>";

        outputLines.Add(coloredText);

        // Trim if too many lines
        if (outputLines.Count > maxOutputLines)
        {
            outputLines.RemoveAt(0);
        }

        UpdateOutputDisplay();
        needsScrollUpdate = true;
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

            // Force text mesh update
            outputText.ForceMeshUpdate();
        }
    }

    private void UpdateScrollContent()
    {
        if (outputScrollRect == null || outputContent == null || outputText == null)
            return;

        // Calculate required height based on line count
        float requiredHeight = outputLines.Count * lineHeight + 10f;

        // Update content size
        outputContent.sizeDelta = new Vector2(outputContent.sizeDelta.x, Mathf.Max(requiredHeight, outputScrollRect.viewport.rect.height));

        // Auto-scroll to bottom if enabled
        if (autoScrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.verticalNormalizedPosition = 0f;
        }

        // Force update of layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(outputContent);
        Canvas.ForceUpdateCanvases();
    }

    private string EscapeRichText(string text)
    {
        // Escape any rich text tags that might be in the input
        return text
            .Replace("<", "<")
            .Replace(">", ">");
    }

    public void ClearOutput()
    {
        outputLines.Clear();
        UpdateOutputDisplay();
        needsScrollUpdate = true;
        LogSystem("Console cleared.");
    }

    // Static helper methods for easy access from anywhere
    public static void Print(string message)
    {
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

    // Helper method to force scroll update
    public void ForceScrollUpdate()
    {
        needsScrollUpdate = true;
    }
}