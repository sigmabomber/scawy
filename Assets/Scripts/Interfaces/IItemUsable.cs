using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemUsable
{
    void OnUse(InventorySlotsUI slot);
    void OnEquip(InventorySlotsUI slot);
    void OnUnequip(InventorySlotsUI slot);
}