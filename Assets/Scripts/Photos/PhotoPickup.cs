using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoPickup : EventListener, IInteractable
{
    public Sprite image;
    public string desc;



    public void Interact()
    {

        Events.Publish(new AddPhotoEvent(image, desc));

        Destroy(gameObject);
    }
    public bool CanInteract()
    {
        return true;
    }

    public string GetInteractionPrompt()
    {
        return "Pick up Photo";
    }
    public Sprite GetInteractionIcon()
    {
        return null;
    }


}
