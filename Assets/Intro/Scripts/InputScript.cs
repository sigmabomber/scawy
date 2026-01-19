using UnityEngine;
using UnityEngine.InputSystem;

public abstract class InputScript : MonoBehaviour
{
    // Static bool shared across ALL InputScript instances
    public static bool InputEnabled = true;

    // REMOVE IsUsingController static property - we don't need it anymore

    protected virtual void Update()
    {
        if (!InputEnabled) return;
        HandleInput();
    }

    protected abstract void HandleInput();
}