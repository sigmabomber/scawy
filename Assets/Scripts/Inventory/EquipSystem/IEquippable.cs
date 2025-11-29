using UnityEngine;

public interface IEquippable
{
    void OnEquip(Transform equipPoint);
    void OnUnequip();
    GameObject GetGameObject();
}
