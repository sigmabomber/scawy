using Doody.Framework.DialogueSystem;
using Doody.GameEvents;
using System.Collections.Generic;
using UnityEngine;
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Audio")]
    public AudioSource audioSource;

    // Track player's choices
    private HashSet<string> activeFlags = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start a dialogue
    public void StartDialogue(DialogueTree tree)
    {
        Debug.Log($"DialogueManager.StartDialogue called with tree: {tree != null}");

        if (tree == null)
        {
            Debug.LogError("Tree is null!");
            return;
        }

        Debug.Log($"Tree speaker: {tree.speakerName}");
        Debug.Log($"Tree dialogue: {tree.dialogue != null}");

        if (tree.dialogue == null)
        {
            Debug.LogError("Tree.dialogue is null!");
            return;
        }

        Debug.Log($"Dialogue text: {tree.dialogue.dialogueText}");
        Debug.Log($"Dialogue options: {tree.dialogue.options != null}");
        Debug.Log($"Options count: {tree.dialogue.options?.Count ?? 0}");

        // Check if player meets requirements
        if (!MeetsRequirements(tree.dialogue))
        {
            Debug.Log("Player doesn't meet requirements for this dialogue");
            return;
        }
        PlayerController.Instance.DisablePlayerInput();
        // Play voice line if available
        if (tree.dialogue.voiceLine != null && audioSource != null)
        {
            audioSource.PlayOneShot(tree.dialogue.voiceLine);
        }

        // Publish event for UI to display
        Events.Publish(new DialogueStartedEvent
        {
            Node = tree.dialogue,
            Tree = tree
        });
    }
    // Player selects an option
    public void ChooseOption(DialogueOption option)
    {
        if (option == null) return;

        // Publish option chosen event
        Events.Publish(new DialogueOptionChosenEvent
        {
            Option = option,
            OptionText = option.optionText
        });

        // Apply any flags from this choice
        foreach (string flag in option.setFlags)
        {
            activeFlags.Add(flag);
            Events.Publish(new DialogueFlagSetEvent { Flag = flag });
        }

        // Continue to next dialogue or end
        if (option.nextDialogue != null)
        {

            StartDialogue(option.nextDialogue);
        }
        else
        {
            EndDialogue();
        }
    }

    // End the conversation
    public void EndDialogue()
    {
        PlayerController.Instance.EnablePlayerInput();
        Events.Publish(new DialogueEndedEvent());
    }

    // Check if player meets requirements
    private bool MeetsRequirements(DialogueNode node)
    {
        foreach (string flag in node.requiredFlags)
        {
            if (!activeFlags.Contains(flag))
                return false;
        }
        return true;
    }

    // Utility methods
    public void SetFlag(string flag)
    {
        activeFlags.Add(flag);
        Events.Publish(new DialogueFlagSetEvent { Flag = flag });
    }

    public void ClearFlag(string flag) => activeFlags.Remove(flag);
    public bool HasFlag(string flag) => activeFlags.Contains(flag);
    public void ClearAllFlags() => activeFlags.Clear();

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}