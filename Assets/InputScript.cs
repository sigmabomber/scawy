using UnityEngine;

public abstract class InputScript : MonoBehaviour
{
    // Static bool shared across ALL InputScript instances
    public static bool InputEnabled = true;

    protected virtual void Update()
    {
        if (!InputEnabled) return;  // Check the static flag
        HandleInput();
    }

    protected abstract void HandleInput();
}