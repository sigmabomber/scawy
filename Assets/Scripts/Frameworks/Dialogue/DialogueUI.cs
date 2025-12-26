using Doody.Framework.DialogueSystem;
using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class DialogueUI : EventListener
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public Image speakerPortrait;
    public TextMeshProUGUI dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    [Header("Button Colors")]
    [Tooltip("Normal button color")]
    public Color normalButtonColor = Color.white;
    [Tooltip("Button color when hovered")]
    public Color hoverButtonColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [Tooltip("Button color when pressed")]
    public Color pressedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [Tooltip("Button color when disabled")]
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Typewriter Effect")]
    [Tooltip("Enable typewriter effect")]
    public bool useTypewriter = true;
    [Tooltip("Default characters per second (used when dialogue has 0)")]
    public float defaultTypewriterSpeed = 30f;
    [Tooltip("Key to skip typewriter effect")]
    public KeyCode skipKey = KeyCode.Space;
    private float currentTypeSpeed = 30f;

    private List<GameObject> activeButtons = new List<GameObject>();
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;
    private List<DialogueOption> cachedOptions = null; // Cache options for use after typewriter
    private string currentFullText = ""; // Store the full text for skipping

    void Update()
    {
        // Allow skipping typewriter effect
        if (isTyping && Input.GetKeyDown(skipKey))
        {
            SkipTypewriter();
        }
    }

    void Start()
    {
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);

        // Hide UI at start
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        else
            Debug.LogError("dialoguePanel is not assigned in the inspector!");
    }


    void OnDialogueStarted(DialogueStartedEvent evt)
    {
        Debug.Log($"DialogueUI: Received DialogueStartedEvent");
        Debug.Log($"  Tree: {evt.Tree != null}");
        Debug.Log($"  Node: {evt.Node != null}");
        Debug.Log($"  Speaker: {evt.Tree?.speakerName}");
        Debug.Log($"  Text: {evt.Node?.dialogueText}");
        Debug.Log($"  Options: {evt.Node?.options != null}");

        if (evt.Tree == null || evt.Node == null)
        {
            Debug.LogError("DialogueUI: Received null tree or node!");
            return;
        }

        DisplayDialogue(evt.Node, evt.Tree);
    }

    void OnDialogueEnded(DialogueEndedEvent evt)
    {
        HideDialogue();
    }

    void DisplayDialogue(DialogueNode node, DialogueTree tree)
    {
        Debug.Log($"DisplayDialogue called with:");
        Debug.Log($"  Node: {node}");
        Debug.Log($"  Tree: {tree}");
        Debug.Log($"  Node text: {node?.dialogueText}");
        Debug.Log($"  Node options: {node?.options}");

        // Stop any ongoing typewriter
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            isTyping = false;
        }

        // Clear options buttons immediately when starting new dialogue
        ClearOptionButtons();

        // Check if UI elements are assigned
        if (dialoguePanel == null)
        {
            Debug.LogError("dialoguePanel is not assigned!");
            return;
        }

        // Show panel
        dialoguePanel.SetActive(true);

        // Set speaker info
        if (speakerNameText != null)
        {
            if (!string.IsNullOrEmpty(tree.speakerName))
                speakerNameText.text = tree.speakerName;
            else
                speakerNameText.text = "Unknown Speaker";
        }
        else
        {
            Debug.LogError("speakerNameText is not assigned!");
        }

        // Store the full text for skipping
        currentFullText = node.dialogueText ?? "";

        // Clear previous options cache
        cachedOptions = null;

        // Start typewriter effect or display immediately
        if (useTypewriter)
        {
            // Cache options for later use - use empty list if null
            cachedOptions = node.options ?? new List<DialogueOption>();

            currentTypeSpeed = node.typewriterSpeed > 0 ? node.typewriterSpeed : defaultTypewriterSpeed;
            float speed = currentTypeSpeed;

            // Clear dialogue text
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }
            else
            {
                Debug.LogError("dialogueText is not assigned!");
                return;
            }

            typewriterCoroutine = StartCoroutine(TypewriterEffect(currentFullText, speed));
        }
        else
        {
            if (dialogueText != null)
            {
                dialogueText.text = currentFullText;
            }
            else
            {
                Debug.LogError("dialogueText is not assigned!");
                return;
            }
            CreateOptionButtons(node.options ?? new List<DialogueOption>());
        }
    }

    IEnumerator TypewriterEffect(string fullText, float speed)
    {
        isTyping = true;

        if (dialogueText == null)
        {
            Debug.LogError("dialogueText is null in TypewriterEffect!");
            yield break;
        }

        dialogueText.text = "";

        // Calculate delay per character
        float delayPerCharacter = 1f / speed;

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text += fullText[i];

            // Check if we should skip
            if (isTyping) // Check flag in case SkipTypewriter was called
            {
                yield return new WaitForSeconds(delayPerCharacter);
            }
        }

        isTyping = false;
        typewriterCoroutine = null;

        // Show options after text is complete
        if (cachedOptions != null)
        {
            CreateOptionButtons(cachedOptions);
        }
    }

    void SkipTypewriter()
    {
        if (typewriterCoroutine != null && isTyping)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            isTyping = false;

            // Show the entire text immediately
            if (dialogueText != null)
            {
                dialogueText.text = currentFullText;
            }

            // Show options immediately
            if (cachedOptions != null)
            {
                CreateOptionButtons(cachedOptions);
            }
        }
    }

    void CreateOptionButtons(List<DialogueOption> options)
    {
        Debug.Log($"CreateOptionButtons called with {options?.Count ?? 0} options");

        // Clear existing buttons first
        ClearOptionButtons();

        // Check if options exist
        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("No dialogue options provided");
            return;
        }

        // Check if optionButtonPrefab is assigned
        if (optionButtonPrefab == null)
        {
            Debug.LogError("Option button prefab is not assigned!");
            return;
        }

        // Check if optionsContainer is assigned
        if (optionsContainer == null)
        {
            Debug.LogError("Options container is not assigned!");
            return;
        }

        // Create option buttons
        foreach (DialogueOption option in options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            Image btnImage = btnObj.GetComponent<Image>();

            if (btn == null || btnText == null)
            {
                Debug.LogError("Option button prefab is missing Button or TextMeshProUGUI component!");
                Destroy(btnObj);
                continue;
            }

            btnText.text = option.optionText;

            // Set up button color transition
            ColorBlock colors = btn.colors;
            colors.normalColor = normalButtonColor;
            colors.highlightedColor = hoverButtonColor;
            colors.pressedColor = pressedButtonColor;
            colors.disabledColor = disabledButtonColor;
            colors.fadeDuration = 0.1f; // Smooth transition
            btn.colors = colors;

         
            
          
            

            // Capture option in closure
            DialogueOption capturedOption = option;
            btn.onClick.AddListener(() => OnOptionClicked(capturedOption));

            activeButtons.Add(btnObj);
        }
    }


    void ClearOptionButtons()
    {
        foreach (GameObject btn in activeButtons)
        {
            if (btn != null)
            {
                // Remove listeners before destroying
                Button button = btn.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
                Destroy(btn);
            }
        }
        activeButtons.Clear();
    }

    void OnOptionClicked(DialogueOption option)
    {
        ClearOptionButtons();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ChooseOption(option);
        }
        else
        {
            Debug.LogError("DialogueManager.Instance is null!");
        }
    }

    void HideDialogue()
    {
        // Stop typewriter if running
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            isTyping = false;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Clear buttons
        ClearOptionButtons();

        // Clear cache
        cachedOptions = null;
        currentFullText = "";
    }
}