using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageInteractable : MonoBehaviour, IInteractable
{


    public Sprite interactionIcon;
    public bool CanInteract()
    {
        return true;
    }
    public string GetInteractionPrompt()
    {
        return "Open";
    }
    public void Interact()
    {


    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
}
