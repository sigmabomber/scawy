using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable, IGunHit
{
    [SerializeField] private string doorID;

    [SerializeField] private InventorySystem inventorySystem;

    public bool canUnlockByGun = false;

    private bool isUnlocked = false;
    public Sprite interactionIcon;
    private int UnlockHash = Animator.StringToHash("Unlock");
    public AudioSource source;
    public Animator animator;
    private void Start()
    {
        animator = animator == null ?  GetComponentInParent<Animator>() : animator;

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
        source.Play();
        
    }

    public Sprite GetInteractionIcon()
    {


        return interactionIcon != null ? interactionIcon : null;
    }
    public void OnGunHit(GunData data)
    {
        if (isUnlocked || !canUnlockByGun) return;
        UnlockDoor();
    }
    
    public bool CanInteract()
    {
        return !isUnlocked;
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
