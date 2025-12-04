using UnityEngine;

public interface IInteractable
{
    void Interact();
    bool CanInteract();
    string GetInteractionPrompt();

    Sprite GetInteractionIcon();
}