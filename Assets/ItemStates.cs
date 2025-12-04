using UnityEngine;

public enum ItemState
{
    InWorld,
    InInventory,
    Equipped
}

public class ItemStateTracker : MonoBehaviour
{
    [SerializeField] private ItemState currentState = ItemState.InWorld;

    public ItemState CurrentState => currentState;

    public void SetState(ItemState newState, bool forceNotify = false)
    {
        if (currentState == newState && !forceNotify) return;

        ItemState previousState = currentState;
        currentState = newState;


        NotifyItemUsableComponents(previousState, newState);
    }

    private void NotifyItemUsableComponents(ItemState previousState, ItemState newState)
    {
        var usableComponents = GetComponents<IItemUsable>();
        foreach (var usable in usableComponents)
        {
            usable.OnItemStateChanged(previousState, newState);
        }
    }

    public bool IsInWorld => currentState == ItemState.InWorld;
    public bool IsInInventory => currentState == ItemState.InInventory;
    public bool IsEquipped => currentState == ItemState.Equipped;

  
}