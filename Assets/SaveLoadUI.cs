using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Simple Save/Load Status UI - One text + one icon
/// </summary>
public class SaveLoadUI : MonoBehaviour
{
    public static SaveLoadUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image statusIcon;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Icons")]
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite failedIcon;
    [SerializeField] private Sprite loadingIcon;

    [Header("Settings")]
    [SerializeField] private float showDuration = 2f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color failedColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color loadingColor = new Color(0.2f, 0.5f, 0.8f);

    private Coroutine currentShowRoutine;
    private Coroutine currentHideRoutine;
    private bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        InitializeUI();

        // Subscribe to save events
        Doody.GameEvents.Events.Subscribe<SaveOperationCompleteEvent>(OnSaveComplete, this);
        Doody.GameEvents.Events.Subscribe<LoadOperationCompleteEvent>(OnLoadComplete, this);
    }

    void Start()
    {
        HideImmediate();
    }

    void OnDestroy()
    {
        Doody.GameEvents.Events.Unsubscribe<SaveOperationCompleteEvent>(OnSaveComplete);
        Doody.GameEvents.Events.Unsubscribe<LoadOperationCompleteEvent>(OnLoadComplete);
    }

    void InitializeUI()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    // ========== PUBLIC API ==========

    public void ShowSaving()
    {
        ShowStatus("Saving...", loadingIcon, loadingColor);
    }

    public void ShowLoading()
    {
        ShowStatus("Loading...", loadingIcon, loadingColor);
    }

    public void ShowSaveSuccess(string details = "")
    {
        string text = "Save Successful";
        if (!string.IsNullOrEmpty(details))
        {
            text += $"\n{details}";
        }
        ShowStatus(text, successIcon, successColor);
    }

    public void ShowSaveFailed(string errorDetails = "")
    {
        string text = "Save Failed";
        if (!string.IsNullOrEmpty(errorDetails))
        {
            // Show only first line of error for simplicity
            string[] lines = errorDetails.Split('\n');
            if (lines.Length > 0)
            {
                text += $"\n{lines[0]}";
            }
        }
        ShowStatus(text, failedIcon, failedColor);
    }

    public void ShowLoadSuccess(string details = "")
    {
        string text = "Load Successful";
        if (!string.IsNullOrEmpty(details))
        {
            text += $"\n{details}";
        }
        ShowStatus(text, successIcon, successColor);
    }

    public void ShowLoadFailed(string errorDetails = "")
    {
        string text = "Load Failed";
        if (!string.IsNullOrEmpty(errorDetails))
        {
            // Show only first line of error for simplicity
            string[] lines = errorDetails.Split('\n');
            if (lines.Length > 0)
            {
                text += $"\n{lines[0]}";
            }
        }
        ShowStatus(text, failedIcon, failedColor);
    }

    public void ShowStatus(string text, Sprite icon, Color color)
    {
        if (currentShowRoutine != null)
        {
            StopCoroutine(currentShowRoutine);
        }

        if (currentHideRoutine != null)
        {
            StopCoroutine(currentHideRoutine);
            currentHideRoutine = null;
        }

        currentShowRoutine = StartCoroutine(ShowStatusCoroutine(text, icon, color));
    }

    public void Hide()
    {
        if (!isVisible) return;

        if (currentShowRoutine != null)
        {
            StopCoroutine(currentShowRoutine);
            currentShowRoutine = null;
        }

        if (currentHideRoutine != null)
        {
            StopCoroutine(currentHideRoutine);
        }

        currentHideRoutine = StartCoroutine(HideCoroutine());
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }

        isVisible = false;

        if (currentShowRoutine != null)
        {
            StopCoroutine(currentShowRoutine);
            currentShowRoutine = null;
        }

        if (currentHideRoutine != null)
        {
            StopCoroutine(currentHideRoutine);
            currentHideRoutine = null;
        }
    }

    // ========== EVENT HANDLERS ==========

    private void OnSaveComplete(SaveOperationCompleteEvent evt)
    {
        if (evt.success)
        {
            string details = $"Slot {evt.saveSlot}";
            if (evt.systemsSaved > 0)
            {
                details += $" - {evt.systemsSaved} systems";
            }
            ShowSaveSuccess(details);
        }
        else
        {
            ShowSaveFailed(evt.errorMessage);
        }
    }

    private void OnLoadComplete(LoadOperationCompleteEvent evt)
    {
        if (evt.success)
        {
            string details = $"Slot {evt.saveSlot}";
            if (evt.systemsLoaded > 0)
            {
                details += $" - {evt.systemsLoaded} systems";
            }
            ShowLoadSuccess(details);
        }
        else
        {
            ShowLoadFailed(evt.errorMessage);
        }
    }

    // ========== COROUTINES ==========

    private IEnumerator ShowStatusCoroutine(string text, Sprite icon, Color color)
    {
        // Set text and icon
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }

        if (statusIcon != null && icon != null)
        {
            statusIcon.sprite = icon;
            statusIcon.color = color;
        }

        // Activate panel
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
        }

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false; // Don't block raycasts for simple status
            canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        isVisible = true;
        currentShowRoutine = null;

        // Auto-hide after duration
        yield return new WaitForSeconds(showDuration);
        Hide();
    }

    private IEnumerator HideCoroutine()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }

        isVisible = false;
        currentHideRoutine = null;
    }

    // ========== TEST METHODS ==========

    [ContextMenu("Test Save Success")]
    public void TestSaveSuccess()
    {
        ShowSaveSuccess("Slot 1 - 5 systems");
    }

    [ContextMenu("Test Save Failed")]
    public void TestSaveFailed()
    {
        ShowSaveFailed("Disk full\nCannot write to file");
    }

    [ContextMenu("Test Saving")]
    public void TestSaving()
    {
        ShowSaving();
    }
}