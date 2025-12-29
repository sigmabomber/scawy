using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Doody.GameEvents;
using Doody.Framework.DialogueSystem;

// ============================================
// SIMPLE UI CONTROLLER - Uses EventListener
// ============================================
public class DialogueUI : EventListener
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public Image speakerPortrait;
    public TextMeshProUGUI dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    [Header("Typewriter Effect")]
    [Tooltip("Enable typewriter effect")]
    public bool useTypewriter = true;
    [Tooltip("Default characters per second (used when dialogue has 0)")]
    public float defaultTypewriterSpeed = 30f;
    [Tooltip("Key to skip typewriter effect")]
    public KeyCode skipKey = KeyCode.Space;


    [Header("Button Colors")]
    [Tooltip("Normal button color")]
    public Color normalButtonColor = Color.white;
    [Tooltip("Button color when hovered")]
    public Color hoverButtonColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [Tooltip("Button color when pressed")]
    public Color pressedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [Tooltip("Button color when disabled")]
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private List<GameObject> activeButtons = new List<GameObject>();
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;

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
        // Subscribe to dialogue events using your framework
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);

        // Hide UI at start
        dialoguePanel.SetActive(false);
    }

    void OnDialogueStarted(DialogueStartedEvent evt)
    {
        DisplayDialogue(evt.Node, evt.Tree);
    }

    void OnDialogueEnded(DialogueEndedEvent evt)
    {
        HideDialogue();
    }

    void DisplayDialogue(DialogueNode node, DialogueTree tree)
    {
        // Stop any ongoing typewriter
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            isTyping = false;
        }

        // Show panel
        dialoguePanel.SetActive(true);

        // Set speaker info
        if (!string.IsNullOrEmpty(tree.speakerName))
            speakerNameText.text = tree.speakerName;

        if (tree.speakerPortrait != null)
        {
           // speakerPortrait.sprite = tree.speakerPortrait;
          //  speakerPortrait.gameObject.SetActive(true);
        }
        else
        {
           // speakerPortrait.gameObject.SetActive(false);
        }

        // Clear old buttons (hide during typing)
        foreach (GameObject btn in activeButtons)
            Destroy(btn);
        activeButtons.Clear();

        // Start typewriter effect or display immediately
        if (useTypewriter)
        {
            float speed = node.typewriterSpeed > 0 ? node.typewriterSpeed : defaultTypewriterSpeed;
            typewriterCoroutine = StartCoroutine(TypewriterEffect(node.dialogueText, node.options, speed));
        }
        else
        {
            dialogueText.text = node.dialogueText;
            CreateOptionButtons(node.options);
        }
    }

    IEnumerator TypewriterEffect(string fullText, List<DialogueOption> options, float speed)
    {
        isTyping = true;
        dialogueText.text = "";

        float delay = 1f / speed;

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typewriterCoroutine = null;

        // Show options after text is complete
        CreateOptionButtons(options);
    }

    void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // This is a bit hacky but works - we'll set it in the next frame
        isTyping = false;
    }

    void CreateOptionButtons(List<DialogueOption> options)
    {
       

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("No dialogue options provided");
            return;
        }

        if (optionButtonPrefab == null)
        {
            Debug.LogError("Option button prefab is not assigned!");
            return;
        }

        if (optionsContainer == null)
        {
            Debug.LogError("Options container is not assigned!");
            return;
        }

        // Create option buttons (only for options that meet requirements)
        foreach (DialogueOption option in options)
        {
            // Check if player meets requirements for this option
            if (!DialogueManager.Instance.MeetsOptionRequirements(option))
            {
                Debug.Log($"[DialogueUI] Hiding option '{option.optionText}' - requirements not met");
                continue; // Skip this option
            }

            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();

            if (btn == null || btnText == null)
            {
                Debug.LogError("Option button prefab is missing Button or TextMeshProUGUI component!");
                Destroy(btnObj);
                continue;
            }

            btnText.text = option.optionText;

            // Set up button colors...
            ColorBlock colors = btn.colors;
            colors.normalColor = normalButtonColor;
            colors.highlightedColor = hoverButtonColor;
            colors.pressedColor = pressedButtonColor;
            colors.disabledColor = disabledButtonColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Capture option in closure
            DialogueOption capturedOption = option;
            btn.onClick.AddListener(() => OnOptionClicked(capturedOption));

            activeButtons.Add(btnObj);
        }
    }

    void OnOptionClicked(DialogueOption option)
    {
        DialogueManager.Instance.ChooseOption(option);
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

        dialoguePanel.SetActive(false);

        // Clear buttons
        foreach (GameObject btn in activeButtons)
            Destroy(btn);
        activeButtons.Clear();
    }
}

// ============================================
// DIALOGUE STARTER - Put this on NPCs/triggers
// ============================================
public class DialogueStarter : MonoBehaviour
{
    [Header("Dialogue Setup")]
    [Tooltip("Drag your dialogue tree here")]
    public DialogueTree dialogueToStart;

    [Header("Trigger Settings")]
    public KeyCode interactKey = KeyCode.E;
    public bool requirePlayerNearby = true;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (dialogueToStart != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToStart);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (requirePlayerNearby && other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (requirePlayerNearby && other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}

// ============================================
// OPTIONAL: Listen to dialogue events elsewhere
// ============================================
// Example of how your teammates can listen to dialogue events
public class ExampleDialogueListener : EventListener
{
    void Start()
    {
        // Listen to when dialogue starts
        Listen<DialogueStartedEvent>(OnDialogueStart);

        // Listen to when player chooses an option
        Listen<DialogueOptionChosenEvent>(OnOptionChosen);

        // Listen to when flags are set
        Listen<DialogueFlagSetEvent>(OnFlagSet);

        // Listen to when dialogue ends
        Listen<DialogueEndedEvent>(OnDialogueEnd);
    }

    void OnDialogueStart(DialogueStartedEvent evt)
    {
        Debug.Log($"Dialogue started: {evt.Tree.speakerName}");
        // Maybe pause player movement, disable other UI, etc.
    }

    void OnOptionChosen(DialogueOptionChosenEvent evt)
    {
        Debug.Log($"Player chose: {evt.OptionText}");
        // Track analytics, save choices, etc.
    }

    void OnFlagSet(DialogueFlagSetEvent evt)
    {
        Debug.Log($"Flag set: {evt.Flag}");
        // React to story flags (unlock areas, trigger quests, etc.)
    }

    void OnDialogueEnd(DialogueEndedEvent evt)
    {
        Debug.Log("Dialogue ended");
        // Resume player movement, re-enable UI, etc.
    }
}