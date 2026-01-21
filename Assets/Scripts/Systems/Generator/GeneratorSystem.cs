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
    private AudioSource source;
    private Animator animator;

    public GameObject one;
    public GameObject two;


    private void Start()
    {
        animator = GetComponent<Animator>();
        source = GetComponent<AudioSource>();
        StartFillingUpHash = Animator.StringToHash("StartFillingUp");

        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }

    public void Interact()
    {
        if (inventorySystem == null) return;
        if (inventorySystem.currentlyEquippedSlot == null) return;

        if(inventorySystem.currentlyEquippedSlot.itemData is GasCanItemData)
        {
            inventorySystem.currentlyEquippedSlot.ClearSlot();
            InputScript.InputEnabled = false;
            animator.SetTrigger(StartFillingUpHash);
            interact = false;
            source?.Play();

        }
        else
        {
            InteractionSystem.Instance.ShowFeedback("Need Gas can!", Color.red);
        }
    }

    public void InteractionComplete()
    {
        InputScript.InputEnabled = true;
        source?.Stop();

        one?.SetActive(false);
        two?.SetActive(true);
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
