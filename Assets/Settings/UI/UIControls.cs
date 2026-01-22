// UIControls.cs - For menu navigation with controller
using UnityEngine;
using UnityEngine.EventSystems;

public class UIControls : MonoBehaviour
{
    public GameObject firstSelectedButton;

    private void OnEnable()
    {
        SetControllerNavigation();
    }

    private void Update()
    {
        // Auto-select first button when controller is connected
        if (Input.GetJoystickNames().Length > 0 &&
            !string.IsNullOrEmpty(Input.GetJoystickNames()[0]) &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        // Allow mouse to override
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0 || Input.GetMouseButtonDown(0))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void SetControllerNavigation()
    {
        if (Input.GetJoystickNames().Length > 0 &&
            !string.IsNullOrEmpty(Input.GetJoystickNames()[0]) &&
            firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }
}