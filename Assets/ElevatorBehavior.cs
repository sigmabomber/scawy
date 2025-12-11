using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorBehavior : MonoBehaviour, IInteractable
{


    public Sprite interactionIcon;
    public bool CanInteract()
    {
        return true;
    }
    public string GetInteractionPrompt()
    {
        return "Call";
    }
    public void Interact()
    {
        

    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
}
