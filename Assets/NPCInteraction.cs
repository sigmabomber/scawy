

using Doody.GameEvents;
using Doody.Framework.DialogueSystem;
using UnityEngine;

public class NPCInteraction : EventListener, IInteractable
{
    public DialogueTree[] npcDialogues;
    private bool isTalking = false;
    public bool lookAtPlayer = true;
    public LookAtPlayer lapScript;
    void Start()
    {
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);
    }

    public string GetInteractionPrompt()
    {
        return "Talk";
    }

    public bool CanInteract()
    {
        return !isTalking && npcDialogues.Length > 0;
    }

    public Sprite GetInteractionIcon()
    {
        return null;
    }

    public void Interact()
    {
        if (isTalking) return;
        if (npcDialogues == null || npcDialogues.Length == 0) return;

        DialogueTree validDialogue = FindFirstValidDialogue();

        if (validDialogue == null)
        {
            return;
        }

        isTalking = true;

        if (lookAtPlayer)
            lapScript.enabled = true;

        DialogueManager.Instance.StartDialogue(validDialogue);

        Events.Publish(new FocusOnObject(lapScript.transform != null ? lapScript.transform : transform));
    }

    private DialogueTree FindFirstValidDialogue()
    {
        foreach (DialogueTree dialogue in npcDialogues)
        {
            if (dialogue == null || dialogue.dialogue == null)
                continue;

            if (DialogueManager.Instance.MeetsRequirements(dialogue.dialogue))
            {
                return dialogue;
            }
        }

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
