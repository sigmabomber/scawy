using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;
using UnityEngine.Experimental.GlobalIllumination;
using Unity.VisualScripting;

public class LightSystem : EventListener
{
    [Tooltip("Share the same id as the lever")]
    public string lightId;

    public GameObject lightObj;


    public void Start()
    {
        Events.Subscribe<TurnOnLights>(TurnOnLights, this);
        Events.Subscribe<TurnOffLights>(TurnOffLights, this);
    }

   


    private void TurnOnLights(TurnOnLights data)
    {
        if (data.Id != lightId)
        {
            print("E");

            return;
        }

        lightObj.SetActive(true);
    }
    private void TurnOffLights(TurnOffLights data)
    {
        if (data.Id != lightId)
        {
            print("E");

            return;
        }
        lightObj.SetActive(false);
    }
}
