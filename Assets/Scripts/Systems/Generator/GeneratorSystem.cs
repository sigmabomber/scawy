using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents.Generator;
using Unity.VisualScripting;

public class GeneratorSystem : EventListener, IInteractable
{

    [SerializeField] private InventorySystem inventorySystem;
    protected int StartFillingUpHash;
    private bool interact = true;

    private Animator animator;


    private void Start()
    {
        animator = GetComponent<Animator>();

        StartFillingUpHash = Animator.StringToHash("StartFillingUp");
    }

    public void Interact()
    {
        if (inventorySystem == null) return;
        if (inventorySystem.currentlyEquippedSlot == null) return;
        print(inventorySystem.currentlyEquippedSlot.itemData.itemName);

        if(inventorySystem.currentlyEquippedSlot.itemData is GasCanItemData)
        {
            inventorySystem.currentlyEquippedSlot.ClearSlot();
            InputScript.InputEnabled = false;
            animator.SetTrigger(StartFillingUpHash);
            interact = false;
        }
        else
        {
            InteractionSystem.Instance.ShowFeedback("Need Gas can!", Color.red);
        }
    }

    public void InteractionComplete()
    {
        InputScript.InputEnabled = true;
    }
    public bool CanInteract()
    {
        return interact;
    }
    public string GetInteractionPrompt()
    {
        return "Fill up";
    }
    public Sprite GetInteractionIcon()
    {
        return null;
    }

}
