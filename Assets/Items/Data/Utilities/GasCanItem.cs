using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasCanItem : MonoBehaviour, IItemUsable
{
   public void OnEquip(InventorySlotsUI slot) { }
   public void OnUnequip(InventorySlotsUI slot) { }
    public void OnUse(InventorySlotsUI slot) { }
    public void OnItemStateChanged(ItemState oldState, ItemState newState) { }
    public void OnDroppedInWorld() { }
}
