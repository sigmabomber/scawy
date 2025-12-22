using Doody.Framework.Player.Effects;
using Doody.Framework.Progressbar;
using Doody.GameEvents;
using Doody.GameEvents.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HealingBehavior : MonoBehaviour, IItemUsable
{
    private bool isUsing = false;

    private HealingItemData data;

    private ItemStateTracker stateTracker;
    private InventorySlotsUI slot;

    private void Start()
    {

        stateTracker = GetComponent<ItemStateTracker>();
        if (stateTracker == null)
        {
            stateTracker = gameObject.AddComponent<ItemStateTracker>();
        }

        Events.Subscribe<ProgressbarCompleted>(DrinkCompleted, this);
    }

    private void Update()
    {
        if (stateTracker != null && stateTracker.IsEquipped)
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                Use();
            }
        }
    }
    public void OnEquip(InventorySlotsUI slotUI)
    {

        slot = slotUI;
        if (slot.itemData is HealingItemData soda)
        {

            data = soda;
        }
    }
    public void OnUnequip(InventorySlotsUI slot)
    {
        if (isUsing)
            Events.Publish(new ProgressbarInteruppted());

        isUsing = false;
    }
    public void OnUse(InventorySlotsUI slot)
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        Use();

    }


    private void Use()
    {
        if (isUsing)
        {
            Events.Publish(new ProgressbarInteruppted());
            isUsing = false;
            return;
        }
        Events.Publish(new StartProgressBar(data.duration, gameObject, "Applying Bandage"));

        isUsing = true;
    }

    private void DrinkCompleted(ProgressbarCompleted completed)
    {
        if (completed.ItemObject == gameObject)
        {

            Events.Publish(new AddHealthEvent((int)data.healAmount));
            slot.ClearSlot();
            Events.Unsubscribe<ProgressbarCompleted>(DrinkCompleted);
            Destroy(gameObject);


        }
    }

    public void OnItemStateChanged(ItemState oldState, ItemState newState)
    {

    }

    public void OnDroppedInWorld()
    {

    }//
}
