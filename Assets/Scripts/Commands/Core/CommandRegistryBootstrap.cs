using UnityEngine;
using Debugging; // Make sure this is here

public class CommandRegistryBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // IMPORTANT: Use Debugging.CommandRegistry, not just CommandRegistry
        if (FindObjectOfType<Debugging.CommandRegistry>() == null)
        {
            GameObject go = new GameObject("[CommandRegistry]");
            go.AddComponent<Debugging.CommandRegistry>();
            DontDestroyOnLoad(go);
            Debug.Log("[CommandRegistryBootstrap] CommandRegistry (Debugging) created.");
        }
        else
        {
            Debug.Log("[CommandRegistryBootstrap] CommandRegistry (Debugging) already exists.");
        }
    }
}