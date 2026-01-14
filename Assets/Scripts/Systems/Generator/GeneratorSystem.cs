using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents.Generator;
public class GeneratorSystem : EventListener, IInteractable
{
    private void Start()
    {
       
    }

    public void Interact()
    {

    }
    public bool CanInteract()
    {
        return true;
    }
    public string GetInteractionPrompt()
    {
        return null;
    }
    public Sprite GetInteractionIcon()
    {
        return null;
    }

}
