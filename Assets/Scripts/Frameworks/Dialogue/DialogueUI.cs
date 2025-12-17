
using Doody.Framework.DialogueSystem;
using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI CONTROLLER - Uses EventListener

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
    private float currentTypeSpeed = 30f;

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

     

      
        // Start typewriter effect or display immediately
        if (useTypewriter)
        {
            currentTypeSpeed = node.typewriterSpeed;
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

        float delay = 1f / currentTypeSpeed;

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

        // This is a bit hacky but works we'll set it in the next frame
        isTyping = false;
    }

    void CreateOptionButtons(List<DialogueOption> options)
    {
        // Create option buttons
        foreach (DialogueOption option in options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = option.optionText;

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

// DIALOGUE STARTER EXAMPLE

/*public class DialogueStarter : MonoBehaviour
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
}*/

