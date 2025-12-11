using Doody.Framework.Progressbar;
using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SodaBehavior : EventListener, IItemUsable
{


    private bool isUsing = false;

    private StaminaItemData data;

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

                Drink();
            }
        }
    }
    public void OnEquip(InventorySlotsUI slotUI)
    {

        slot = slotUI;
        if (slot.itemData is StaminaItemData soda)
        {

            data = soda;
        }
    }
    public void OnUnequip(InventorySlotsUI slot)
    {
        if(isUsing)
            Events.Publish(new ProgressbarInteruppted());

        isUsing = false;
    }
    public void OnUse(InventorySlotsUI slot)
    {
        if (stateTracker != null && !stateTracker.IsEquipped) return;
        Drink();

    }


    private void Drink()
    {
        if (isUsing)
        {
            Events.Publish(new ProgressbarInteruppted());
            isUsing = false;
            return;
        }
        Events.Publish(new StartProgressBar(data.duration, gameObject, "Drinking"));

        isUsing = true;
    }

    private void DrinkCompleted(ProgressbarCompleted completed)
    {
        if(completed.ItemObject == gameObject)
        {
            Destroy(gameObject);
            slot.ClearSlot();
        }
    }

    public void OnItemStateChanged(ItemState oldState, ItemState newState)
    {

    }

    public void OnDroppedInWorld()
    {

    }//
}
