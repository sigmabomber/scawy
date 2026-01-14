using Doody.GameEvents;
using Doody.InventoryFramework;
using UnityEngine;

public class ItemPickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    public ItemData itemData;
    public int quantity = 1;
    public SlotPriority slotPriority = SlotPriority.Normal;

    [Header("Interaction Settings")]
    public string customPrompt = "";
    public Sprite interactionIcon;
    public bool isBeingPickedUp = false;

    [Header("Visual Feedback")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;

    private ItemStateTracker stateTracker;
    private AudioSource audioSource;

    private void Start()
    {
        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
        {
            stateTracker = gameObject.AddComponent<ItemStateTracker>();
        }

      

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Interact()
    {
        if (isBeingPickedUp) return;

        isBeingPickedUp = true;

        bool success = false;

        if (InventoryFramework.Instance != null)
        {
            var playerSystem = InventoryFramework.Instance.GetInventorySystem("player_inventory");
            if (playerSystem != null)
            {
                // Do NOT drop when full for manual pickups

                success = playerSystem.AddItem(itemData, quantity, slotPriority, gameObject, false);
            }
        }
        else
        {
            if (InventorySystem.Instance != null)
            {
                // Do NOT drop when full for manual pickups
                success = InventorySystem.Instance.AddItem(itemData, quantity, slotPriority, gameObject, false);
            }
        }

        if (success)
        {
            PlayPickupEffects();
            gameObject.SetActive(false);
        }
        else
        {
            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowFeedback("Inventory full!", Color.red);
            }
            else
            {
                Debug.Log("Inventory full!");
            }

            Events.Publish(new InventoryFullEvent("player_inventory", itemData, quantity));
            isBeingPickedUp = false;
        }
    }
    public bool CanInteract()
    {
        return !isBeingPickedUp &&
               itemData != null &&
               stateTracker != null &&
               stateTracker.IsInWorld;
    }

    public string GetInteractionPrompt()
    {
        if (!string.IsNullOrEmpty(customPrompt))
            return customPrompt;

        return $"Pick up {itemData.itemName} \n[E]";
    }

    public Sprite GetInteractionIcon()
    {
        return interactionIcon;
    }

    private void PlayPickupEffects()
    {
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        StartCoroutine(PlayPickupAnimation());
    }

    private System.Collections.IEnumerator PlayPickupAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
    }

    public void OnDroppedFromInventory()
    {
        isBeingPickedUp = false;

        if (stateTracker != null)
        {
            stateTracker.SetState(ItemState.InWorld);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        gameObject.SetActive(true);

    }

    private void OnDestroy()
    {
    }
}