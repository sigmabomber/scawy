using System.Collections;
using UnityEngine;

public class ItemPickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    public ItemData itemData;
    public int quantity = 1;
    public SlotPriority slotPriority = SlotPriority.Normal;
    public Sprite interactionIcon;
    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;

    private bool isBeingPickedUp = false;

    private void Start()
    {
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }

    public void Interact()
    {
        if (isBeingPickedUp) return;

        SlotPriority priority = SlotPriority.Normal;

        var itemDataType = itemData.GetType();
        var priorityField = itemDataType.GetField("preferredSlotType");
        if (priorityField != null)
        {
            priority = (SlotPriority)priorityField.GetValue(itemData);
        }
        else
        {
            priority = slotPriority;
        }

        if (inventorySystem != null && inventorySystem.AddItem(itemData, quantity, priority))
        {
            isBeingPickedUp = true;
            StartCoroutine(MoveToPlayerThenDestroy());
        }
        else
        {
            InteractionSystem.Instance.ShowFeedback("Inventory Full!", Color.red);
        }
    }

    public bool CanInteract()
    {
        return itemData != null && !isBeingPickedUp;
    }
    public Sprite GetInteractionIcon()
    {

        return interactionIcon != null ? interactionIcon : null;
    }
    public string GetInteractionPrompt()
    {
        return itemData != null ? itemData.itemName : "Item";
    }


    private IEnumerator MoveToPlayerThenDestroy()
    {
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        // Offset the target slightly downward
        Vector3 playerPosition = Camera.main.transform.position - new Vector3(0, 0.5f, 0); // adjust 0.5f to taste

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, playerPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

}
