using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotePickup : EventListener, IInteractable
{
    public string title;
    public string content;
    public string date;

    public void Interact()
    {
        Events.Publish(new AddNoteEvent(title, content, date));

        Destroy(gameObject);
    }
    public bool CanInteract()
    {
        return true;
    }

    public string GetInteractionPrompt()
    {
        return "Pick up Note";
    }
    public Sprite GetInteractionIcon()
    {
        return null;
    }

   
}
