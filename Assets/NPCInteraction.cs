

using Doody.GameEvents;
using Doody.Framework.DialogueSystem;
using UnityEngine;

public class NPCInteraction : EventListener, IInteractable
{
    public DialogueTree[] npcDialogues;
    public bool isTalking = false;
    public bool lookAtPlayer = true;
    public LookAtPlayer lapScript;
    public Transform head;
    void Start()
    {
        // Listen for dialogue events
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);
    }

    public string GetInteractionPrompt()
    {
        return "Talk";
    }

    public bool CanInteract()
    {
        return !isTalking;
    }

    public Sprite GetInteractionIcon()
    {
        return null;
    }

    public void Interact()
    {
        if (isTalking) return;
        if (npcDialogues == null || npcDialogues.Length == 0) return;

        // Find the first dialogue that meets requirements
        DialogueTree validDialogue = FindFirstValidDialogue();

        if (validDialogue == null)
        {
            return;
        }

        // Only set isTalking if we found a valid dialogue
        isTalking = true;

        if (lookAtPlayer)
            lapScript.enabled = true;

        // Start the dialogue
        DialogueManager.Instance.StartDialogue(validDialogue);

        Events.Publish(new FocusOnObject(head != null ? head : transform));
    }

    // Find the first dialogue that meets all requirements
    private DialogueTree FindFirstValidDialogue()
    {
        foreach (DialogueTree dialogue in npcDialogues)
        {
            // Skip null or invalid dialogues
            if (dialogue == null || dialogue.dialogue == null)
                continue;

            // Check if player meets requirements
            if (DialogueManager.Instance.MeetsRequirements(dialogue.dialogue))
            {
                return dialogue;
            }
        }

        // No valid dialogue found
        return null;
    }

    void OnDialogueStarted(DialogueStartedEvent evt)
    {
    }

    void OnDialogueEnded(DialogueEndedEvent evt)
    {
        isTalking = false;
        if (lookAtPlayer)
            lapScript.enabled = false;


        Events.Publish(new UnFocusObject());
    
}
}
