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

    public bool isBeingPickedUp = false;
    private Coroutine pickupCoroutine;
    private ItemStateTracker stateTracker; 

    private void Start()
    {
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }

        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
        {
            stateTracker = gameObject.AddComponent<ItemStateTracker>();
        }
    }

    public void Interact()
    {
        if (isBeingPickedUp || !gameObject.activeSelf) return;

        if (stateTracker != null && !stateTracker.IsInWorld)
        {
            Debug.LogWarning("Trying to pick up item that's not in world state!");
            return;
        }

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

        if (inventorySystem != null)
        {
            Vector3 originalPosition = transform.position;
            bool wasAdded = inventorySystem.AddItem(itemData, quantity, priority, gameObject);

            if (wasAdded)
            {
                isBeingPickedUp = true;

                if (stateTracker != null)
                {
                    stateTracker.SetState(ItemState.InInventory);
                }

                var usableComponents = GetComponents<IItemUsable>();
                foreach (var usable in usableComponents)
                {
                    var method = usable.GetType().GetMethod("OnItemStateChanged");
                    if (method != null)
                    {
                        method.Invoke(usable, new object[] { ItemState.InWorld, ItemState.InInventory });
                    }

                    var pickupMethod = usable.GetType().GetMethod("OnPickedUp");
                    if (pickupMethod != null)
                    {
                        pickupMethod.Invoke(usable, null);
                    }
                }

                if (pickupCoroutine != null)
                    StopCoroutine(pickupCoroutine);

                pickupCoroutine = StartCoroutine(MoveToPlayerThenDisable());
            }
            else
            {
                InteractionSystem.Instance?.ShowFeedback("Inventory Full!", Color.red);

                if (stateTracker != null)
                {
                    stateTracker.SetState(ItemState.InWorld);
                }
            }
        }
    }

    public bool CanInteract()
    {
        bool isInWorld = stateTracker != null && stateTracker.IsInWorld;
        return itemData != null && !isBeingPickedUp && gameObject.activeSelf && isInWorld;
    }

    public Sprite GetInteractionIcon()
    {
        return interactionIcon != null ? interactionIcon : null;
    }

    public string GetInteractionPrompt()
    {
        return itemData != null ? $"{itemData.itemName}" : "Item";
    }

    private IEnumerator MoveToPlayerThenDisable()
    {
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("GameObject is already inactive, cannot start pickup animation.");
            isBeingPickedUp = false;
            yield break;
        }

        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 playerPosition = Camera.main.transform.position - new Vector3(0, 0.5f, 0);

        while (elapsedTime < duration)
        {
            if (!gameObject.activeSelf)
            {
                Debug.LogWarning("GameObject became inactive during pickup animation.");
                isBeingPickedUp = false;
                yield break;
            }

            transform.position = Vector3.Lerp(startPosition, playerPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        isBeingPickedUp = false;

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    private void OnEnable()
    {
        isBeingPickedUp = false;

        if (stateTracker != null)
        {
            if (stateTracker.CurrentState != ItemState.InWorld)
            {
                stateTracker.SetState(ItemState.InWorld);
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }
    }
}