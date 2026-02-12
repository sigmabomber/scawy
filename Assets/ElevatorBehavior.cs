using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorBehavior : MonoBehaviour, IInteractable
{

    public bool InsideButton = false;
    public Sprite interactionIcon;

    public int targetScene;

    private bool isUsing;
    public bool CanInteract()
    {
        return !isUsing;
    }
    public string GetInteractionPrompt()
    {
        return !InsideButton ? "Call" : "Interact";
    }
    public void Interact()
    {
        if (isUsing) return;

        isUsing = true;

        SceneManager.LoadScene(targetScene);

    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
}
