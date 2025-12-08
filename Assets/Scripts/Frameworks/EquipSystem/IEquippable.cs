using UnityEngine;


namespace Doody.InventoryFramework
{
    public interface IEquippable
    {
        void OnEquip(Transform equipPoint);
        void OnUnequip();
        GameObject GetGameObject();
    }
}