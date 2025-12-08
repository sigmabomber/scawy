using System.Linq;
using UnityEngine;

public class ItemStateTracker : MonoBehaviour
{
    [SerializeField] private ItemState currentState = ItemState.InWorld;

    public ItemState CurrentState => currentState;

    public void SetState(ItemState newState, bool forceNotify = false)
    {
        if (currentState == newState && !forceNotify) return;

        ItemState previousState = currentState;
        currentState = newState;

        // Update physics and layers based on state
        UpdatePhysicsForState(newState);

        // Notify components
        NotifyItemUsableComponents(previousState, newState);
    }

    private void UpdatePhysicsForState(ItemState state)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();

        switch (state)
        {
            case ItemState.InWorld:
                // Enable physics for world items
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                if (col != null) col.enabled = true;
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("Interactable"));
                break;

            case ItemState.InInventory:
                // Disable physics for inventory items
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                if (col != null) col.enabled = false;
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("PickedUpItem"));
                break;

            case ItemState.Equipped:
                // Disable physics for equipped items
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                if (col != null) col.enabled = false;
                SetLayerRecursively(gameObject, LayerMask.NameToLayer("PickedUpItem"));
                break;
        }
    }

    private void NotifyItemUsableComponents(ItemState previousState, ItemState newState)
    {
        // Notify IItemUsable components
        var usableComponents = GetComponents<IItemUsable>();
        foreach (var usable in usableComponents)
        {
            usable.OnItemStateChanged(previousState, newState);
        }

        // Also notify child components
        var childUsableComponents = GetComponentsInChildren<IItemUsable>(true);
        var mainComponentsList = usableComponents.ToList();

        foreach (var usable in childUsableComponents)
        {
            if (!mainComponentsList.Contains(usable))
            {
                usable.OnItemStateChanged(previousState, newState);
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public bool IsInWorld => currentState == ItemState.InWorld;
    public bool IsInInventory => currentState == ItemState.InInventory;
    public bool IsEquipped => currentState == ItemState.Equipped;
}