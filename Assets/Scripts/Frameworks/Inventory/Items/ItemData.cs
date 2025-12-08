using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemData : ScriptableObject
{

    [Header("ItemData")]

    [Space]
    public Sprite icon;
    public string itemName;
    public int maxStack;

    public GameObject prefab;


    public SlotPriority priority;

}
