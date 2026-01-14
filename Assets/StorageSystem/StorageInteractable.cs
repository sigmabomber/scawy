using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageInteractable : MonoBehaviour, IInteractable
{

    private StorageContainer container;
    public Sprite interactionIcon;

    private void Start()
    {
        container = GetComponent<StorageContainer>();
    }
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

        StorageUIManager.Instance.OpenStorage(container);
    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
}
