// DialogueEventDispatcher.cs - Put this in Assets folder
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.Framework.DialogueSystem
{
    public class DialogueEventDispatcher : MonoBehaviour
    {
        private static DialogueEventDispatcher _instance;
        public static DialogueEventDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DialogueEventDispatcher");
                    _instance = go.AddComponent<DialogueEventDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<string, Action> eventActions = new Dictionary<string, Action>();

        public void RegisterEvent(string eventID, Action action)
        {
            if (eventActions.ContainsKey(eventID))
            {
                eventActions[eventID] += action;
            }
            else
            {
                eventActions[eventID] = action;
            }
        }

        public void UnregisterEvent(string eventID, Action action)
        {
            if (eventActions.ContainsKey(eventID))
            {
                eventActions[eventID] -= action;
            }
        }

        public void TriggerEvent(string eventID)
        {
            if (eventActions.ContainsKey(eventID))
            {
                eventActions[eventID]?.Invoke();
            }
            else
            {
                Debug.LogWarning($"No event registered with ID: {eventID}");
            }
        }
    }
}