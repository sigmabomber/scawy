using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents.Generator;
public class GeneratorSystem : EventListener, IInteractable
{

    [SerializeField] private InventorySystem inventorySystem;
    protected int StartFillingUpHash;

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
            animator.SetTrigger(StartFillingUpHash);
        }
    }
    public bool CanInteract()
    {
        return true;
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
