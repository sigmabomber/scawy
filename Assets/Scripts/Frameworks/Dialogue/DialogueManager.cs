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
        if (tree == null || tree.dialogue == null)
        {
            Debug.LogError("Cannot start dialogue - tree is null or empty!");
            return;
        }

        // Check if player meets requirements
        if (!MeetsRequirements(tree.dialogue))
        {
            Debug.Log("Player doesn't meet requirements for this dialogue");
            return;
        }

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