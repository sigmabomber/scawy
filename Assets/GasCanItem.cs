using Doody.InventoryFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasCanItem : MonoBehaviour, IEquippable
{
   public void OnEquip(Transform transform) { }
   public void OnUnequip() { }
    public GameObject GetGameObject()
    {
        return null;
    }
}
