using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable, IRevolverHit
{
    [SerializeField] private string doorID;

    [SerializeField] private InventorySystem inventorySystem;

    private bool isUnlocked = false;
    public Sprite interactionIcon;
    public int UnlockHash = Animator.StringToHash("Unlock");

    public Animator animator;
    private void Start()
    {
        animator = GetComponentInParent<Animator>();

        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }



    public void Interact()
    {
        if (isUnlocked) return;

        if (inventorySystem == null) return;

        foreach (InventorySlotsUI slot in inventorySystem.normalInventorySlots)
        {
            if (slot.itemData == null) continue;

            if (slot.itemData is KeyItemData key)
            {
                if (key.keyID == doorID)
                {
                    UnlockDoor();
                    slot.ClearSlot();
                    return;
                }

            }
        }

        InteractionSystem.Instance.ShowFeedback("No Key!", Color.red);
        
    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
    public void OnRevolverHit()
    {
        if (isUnlocked) return;
        UnlockDoor();
    }
    
    public bool CanInteract()
    {
        return true;
    }
    public string GetInteractionPrompt()
    {
        return "Unlock";
    }

    private void UnlockDoor()
    {
        if(isUnlocked) return;
       isUnlocked = true;


        animator.SetTrigger(UnlockHash);
    }
}
