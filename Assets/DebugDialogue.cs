using UnityEngine;
using Doody.GameEvents;
using Doody.Framework.DialogueSystem;

// DIALOGUE DEBUGGER
public class DialogueDebugger : MonoBehaviour
{
    [Header("Quick Test Setup")]
    [Tooltip("Press this key to start test dialogue")]
    public KeyCode testKey = KeyCode.T;

    [Header("Test Dialogue Trees")]
    [Tooltip("Drag dialogue trees here to test them quickly")]
    public DialogueTree[] testDialogues;

    [Header("Create Test Dialogue At Runtime")]
    public bool createTestDialogue = true;
    [TextArea(2, 4)]
    public string testText1 = "Hello! This is a test dialogue. Pretty cool, right?";
    public float testSpeed1 = 30f;

    [TextArea(2, 4)]
    public string testText2 = "This one types slower... for dramatic effect.";
    public float testSpeed2 = 15f;

    [TextArea(2, 4)]
    public string testText3 = "And this one is SUPER FAST because I'm excited!!!";
    public float testSpeed3 = 60f;

    private int currentTestIndex = 0;

    void Update()
    {
        // Test dialogue trees from array
        if (Input.GetKeyDown(testKey))
        {
            if (testDialogues != null && testDialogues.Length > 0)
            {
                DialogueTree tree = testDialogues[currentTestIndex % testDialogues.Length];
                if (tree != null)
                {
                    Debug.Log($"Starting test dialogue: {tree.name}");
                    DialogueManager.Instance.StartDialogue(tree);
                    currentTestIndex++;
                }
            }
            else if (createTestDialogue)
            {
                CreateAndTestRuntimeDialogue();
            }
            else
            {
                Debug.LogWarning("No test dialogues assigned! Drag DialogueTree assets into the Test Dialogues array.");
            }
        }

        // Quick flag testing
        if (Input.GetKeyDown(KeyCode.F))
        {
            DialogueManager.Instance.SetFlag("TestFlag");
            Debug.Log("Set flag: TestFlag");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            DialogueManager.Instance.ClearAllFlags();
            Debug.Log("Cleared all flags");
        }
    }

    void CreateAndTestRuntimeDialogue()
    {
        // Create dialogue chain at runtime
        DialogueTree dialogue3 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue3.speakerName = "Excited Person";
        dialogue3.dialogue = new DialogueNode
        {
            dialogueText = testText3,
            typewriterSpeed = testSpeed3,
            options = new System.Collections.Generic.List<DialogueOption>
            {
                new DialogueOption { optionText = "Wow, that was fast!", nextDialogue = null }
            }
        };

        DialogueTree dialogue2 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue2.speakerName = "Mysterious Stranger";
        dialogue2.dialogue = new DialogueNode
        {
            dialogueText = testText2,
            typewriterSpeed = testSpeed2,
            options = new System.Collections.Generic.List<DialogueOption>
            {
                new DialogueOption { optionText = "Tell me more...", nextDialogue = dialogue3 },
                new DialogueOption { optionText = "No thanks.", nextDialogue = null }
            }
        };

        DialogueTree dialogue1 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue1.speakerName = "Test NPC";
        dialogue1.dialogue = new DialogueNode
        {
            dialogueText = testText1,
            typewriterSpeed = testSpeed1,
            options = new System.Collections.Generic.List<DialogueOption>
            {
                new DialogueOption { optionText = "Yeah, it's pretty neat!", nextDialogue = dialogue2 },
                new DialogueOption { optionText = "Meh, I've seen better.", nextDialogue = dialogue3 }
            }
        };

        Debug.Log("Starting runtime test dialogue chain");
        DialogueManager.Instance.StartDialogue(dialogue1);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== DIALOGUE DEBUGGER ===");
        GUILayout.Label($"Press [{testKey}] to test dialogue");
        GUILayout.Label("Press [F] to set TestFlag");
        GUILayout.Label("Press [C] to clear all flags");
        GUILayout.Space(10);

        if (testDialogues != null && testDialogues.Length > 0)
        {
            GUILayout.Label($"Testing {testDialogues.Length} dialogue(s)");
            GUILayout.Label($"Next: {currentTestIndex % testDialogues.Length}");
        }
        else if (createTestDialogue)
        {
            GUILayout.Label("Using runtime test dialogue");
        }
        else
        {
            GUILayout.Label("No dialogues assigned!");
        }

        GUILayout.EndArea();
    }
}

// ============================================
// DIALOGUE STATS MONITOR - Shows live info
// ============================================
public class DialogueStatsMonitor : EventListener
{
    [Header("Display Settings")]
    public bool showStats = true;
    public bool logEvents = true;

    private string currentDialogue = "None";
    private string lastChoice = "None";
    private int flagCount = 0;

    void Start()
    {
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);
        Listen<DialogueOptionChosenEvent>(OnOptionChosen);
        Listen<DialogueFlagSetEvent>(OnFlagSet);
    }

    void OnDialogueStarted(DialogueStartedEvent evt)
    {
        currentDialogue = evt.Tree.speakerName + ": " + evt.Node.dialogueText.Substring(0, Mathf.Min(30, evt.Node.dialogueText.Length)) + "...";

        if (logEvents)
            Debug.Log($"[DIALOGUE] Started: {evt.Tree.speakerName} | Speed: {evt.Node.typewriterSpeed} | Options: {evt.Node.options.Count}");
    }

    void OnDialogueEnded(DialogueEndedEvent evt)
    {
        currentDialogue = "None";

        if (logEvents)
            Debug.Log("[DIALOGUE] Ended");
    }

    void OnOptionChosen(DialogueOptionChosenEvent evt)
    {
        lastChoice = evt.OptionText;

        if (logEvents)
            Debug.Log($"[DIALOGUE] Choice: {evt.OptionText}");
    }

    void OnFlagSet(DialogueFlagSetEvent evt)
    {
        flagCount++;

        if (logEvents)
            Debug.Log($"[DIALOGUE] Flag Set: {evt.Flag}");
    }

    void OnGUI()
    {
        if (!showStats) return;

        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 150));
        GUILayout.Label("=== DIALOGUE STATS ===");
        GUILayout.Label($"Current: {currentDialogue}");
        GUILayout.Label($"Last Choice: {lastChoice}");
        GUILayout.Label($"Flags Set: {flagCount}");
        GUILayout.EndArea();
    }
}