using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Doody.InventoryFramework;
using Doody.Framework.Progressbar;

public class SodaBehavior : EventListener, IItemUsable
{


    private bool isUsing = false;

    private StaminaItemData data;
    

    public void OnEquip(InventorySlotsUI slot)
    {

    }
    public void OnUnequip(InventorySlotsUI slot)
    {
        if(isUsing)
            Events.Publish(new ProgressbarInteruppted());
    }
    public void OnUse(InventorySlotsUI slot)
    {
        if (isUsing)
        {
            Events.Publish(new ProgressbarInteruppted());
            return;
        }
        Events.Publish(new StartProgressBar(data.duration, gameObject));

        isUsing = true;

    }

    public void OnItemStateChanged(ItemState oldState, ItemState newState)
    {

    }

    public void OnDroppedInWorld()
    {

    }
}
