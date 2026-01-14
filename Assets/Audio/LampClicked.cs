using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;
public class LampClicked : MonoBehaviour
{
#pragma warning disable CS0108
    public GameObject light;
#pragma warning restore CS0108
    void Start()
    {
        Events.Subscribe<ItemClicked>(ToggleLight, this);
    }

    // Update is called once per frame
   public void ToggleLight(ItemClicked data)
    {
        if (light == null || gameObject != data.GameObject) return;


        light.SetActive(!light.activeSelf);
    }
}


public class ItemClicked
{
    public GameObject GameObject;
    public ItemClicked(GameObject obj) {
        GameObject = obj;
    }
}
