using UnityEngine;
using Debugging;
using Doody.Debugging;

public class CommandRegistryBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindObjectOfType<Doody.Debugging.CommandRegistry>() == null)
        {
            GameObject go = new GameObject("[CommandRegistry]");
            go.AddComponent<Doody.Debugging.CommandRegistry>();
            DontDestroyOnLoad(go);
        }
        else
        {
        }
    }
}