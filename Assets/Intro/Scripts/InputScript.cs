using UnityEngine;
using UnityEngine.InputSystem;

public abstract class InputScript : MonoBehaviour
{
    public static bool InputEnabled = true;


    protected virtual void Update()
    {
        if (!InputEnabled) return;
        HandleInput();
    }

    protected abstract void HandleInput();
}