using UnityEngine;
using Doody.GameEvents;
using Doody.Framework.DialogueSystem;
using System.Collections.Generic;

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

    void Start()
    {
        // Check if DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is null! Make sure you have a DialogueManager in the scene.");
        }
    }

    void Update()
    {
        // Test dialogue trees from array
        if (Input.GetKeyDown(testKey))
        {
            Debug.Log("Test key pressed");

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("Cannot start dialogue: DialogueManager.Instance is null!");
                return;
            }

            if (testDialogues != null && testDialogues.Length > 0)
            {
                DialogueTree tree = testDialogues[currentTestIndex % testDialogues.Length];
                if (tree != null)
                {
                    Debug.Log($"Starting test dialogue: {tree.name}");
                    Debug.Log($"Dialogue text: {tree.dialogue?.dialogueText}");
                    Debug.Log($"Options count: {tree.dialogue?.options?.Count}");

                    DialogueManager.Instance.StartDialogue(tree);
                    currentTestIndex++;
                }
                else
                {
                    Debug.LogError("DialogueTree is null!");
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
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.SetFlag("TestFlag");
                Debug.Log("Set flag: TestFlag");
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ClearAllFlags();
                Debug.Log("Cleared all flags");
            }
        }
    }

    void CreateAndTestRuntimeDialogue()
    {
        Debug.Log("Creating runtime test dialogue chain...");

        // Create dialogue chain at runtime
        DialogueTree dialogue3 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue3.speakerName = "Excited Person";

        // Create node with initialized options list
        DialogueNode node3 = new DialogueNode
        {
            dialogueText = testText3,
            typewriterSpeed = testSpeed3,
            options = new List<DialogueOption>()
        };

        // Add option
        node3.options.Add(new DialogueOption
        {
            optionText = "Wow, that was fast!",
            nextDialogue = null
        });

        dialogue3.dialogue = node3;

        DialogueTree dialogue2 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue2.speakerName = "Mysterious Stranger";

        DialogueNode node2 = new DialogueNode
        {
            dialogueText = testText2,
            typewriterSpeed = testSpeed2,
            options = new List<DialogueOption>()
        };

        node2.options.Add(new DialogueOption
        {
            optionText = "Tell me more...",
            nextDialogue = dialogue3
        });

        node2.options.Add(new DialogueOption
        {
            optionText = "No thanks.",
            nextDialogue = null
        });

        dialogue2.dialogue = node2;

        DialogueTree dialogue1 = ScriptableObject.CreateInstance<DialogueTree>();
        dialogue1.speakerName = "Test NPC";

        DialogueNode node1 = new DialogueNode
        {
            dialogueText = testText1,
            typewriterSpeed = testSpeed1,
            options = new List<DialogueOption>()
        };

        node1.options.Add(new DialogueOption
        {
            optionText = "Yeah, it's pretty neat!",
            nextDialogue = dialogue2
        });

        node1.options.Add(new DialogueOption
        {
            optionText = "Meh, I've seen better.",
            nextDialogue = dialogue3
        });

        dialogue1.dialogue = node1;

        Debug.Log($"Starting dialogue: {dialogue1.speakerName}");
        Debug.Log($"Dialogue text: {dialogue1.dialogue.dialogueText}");
        Debug.Log($"Options count: {dialogue1.dialogue.options.Count}");

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

        // Show DialogueManager status
        GUILayout.Space(10);
        if (DialogueManager.Instance != null)
        {
            GUILayout.Label("DialogueManager: OK");
        }
        else
        {
            GUILayout.Label("DialogueManager: NULL!");
        }

        GUILayout.EndArea();
    }
}

