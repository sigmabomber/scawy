using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControllerManager : MonoBehaviour
{
    public static ControllerManager Instance { get; private set; }

    [Header("Navigation Settings")]
    public float navigationDelay = 0.2f;
    public float stickDeadzone = 0.1f;
    public float scrollSpeed = 100f;
    public float scrollDelay = 0.1f;

    private float lastNavigationTime;
    private float lastScrollTime;
    private bool isInventoryOpen = false;
    private ScrollRect currentScrollRect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Only handle navigation when inventory is open
        if (!isInventoryOpen) return;

        HandleNavigation();
        HandleScrolling();
        EnsureSelection();
    }

    private void HandleNavigation()
    {
        if (Time.time - lastNavigationTime < navigationDelay) return;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 input = GetNavigationInput(gamepad);

        if (input.magnitude > stickDeadzone)
        {
            NavigateUI(input);
            lastNavigationTime = Time.time;
        }
    }

    private Vector2 GetNavigationInput(Gamepad gamepad)
    {
        Vector2 input = gamepad.leftStick.ReadValue();
        if (input.magnitude < stickDeadzone)
        {
            input = gamepad.dpad.ReadValue();
        }
        return input;
    }

    private void NavigateUI(Vector2 direction)
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected == null) return;

        Selectable selectable = currentSelected.GetComponent<Selectable>();
        if (selectable == null) return;

        Selectable nextSelectable = null;

        if (direction.y > 0.5f)
        {
            nextSelectable = selectable.FindSelectableOnUp();
        }
        else if (direction.y < -0.5f)
        {
            nextSelectable = selectable.FindSelectableOnDown();
        }
        else if (direction.x > 0.5f)
        {
            nextSelectable = selectable.FindSelectableOnRight();
        }
        else if (direction.x < -0.5f)
        {
            nextSelectable = selectable.FindSelectableOnLeft();
        }

        if (nextSelectable != null)
        {
            nextSelectable.Select();

            // Scroll to make the selected item visible
            ScrollToSelection(nextSelectable.gameObject);
        }
    }

    private void HandleScrolling()
    {
        if (Time.time - lastScrollTime < scrollDelay) return;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // Check for right stick scrolling
        Vector2 scrollInput = gamepad.rightStick.ReadValue();

        if (scrollInput.magnitude > stickDeadzone)
        {
            if (currentScrollRect != null)
            {
                // Scroll vertically with right stick
                if (Mathf.Abs(scrollInput.y) > 0.5f)
                {
                    float scrollAmount = scrollInput.y * scrollSpeed * Time.deltaTime;
                    currentScrollRect.verticalNormalizedPosition += scrollAmount / currentScrollRect.content.rect.height;
                    currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition);
                }

                // Scroll horizontally with right stick (optional)
                if (Mathf.Abs(scrollInput.x) > 0.5f && currentScrollRect.horizontal)
                {
                    float scrollAmount = scrollInput.x * scrollSpeed * Time.deltaTime;
                    currentScrollRect.horizontalNormalizedPosition += scrollAmount / currentScrollRect.content.rect.width;
                    currentScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(currentScrollRect.horizontalNormalizedPosition);
                }

                lastScrollTime = Time.time;
            }
        }

        // Also check for shoulder button scrolling (L1/R1 or LB/RB)
        if (gamepad.leftShoulder.wasPressedThisFrame)
        {
            ScrollPage(false); // Scroll up/left
            lastScrollTime = Time.time;
        }
        else if (gamepad.rightShoulder.wasPressedThisFrame)
        {
            ScrollPage(true); // Scroll down/right
            lastScrollTime = Time.time;
        }

        // Check for trigger scrolling (L2/R2 or LT/RT)
        if (gamepad.leftTrigger.isPressed)
        {
            if (currentScrollRect != null)
            {
                currentScrollRect.verticalNormalizedPosition += 0.5f * Time.deltaTime;
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition);
            }
        }
        else if (gamepad.rightTrigger.isPressed)
        {
            if (currentScrollRect != null)
            {
                currentScrollRect.verticalNormalizedPosition -= 0.5f * Time.deltaTime;
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition);
            }
        }
    }

    private void ScrollPage(bool forward)
    {
        if (currentScrollRect == null) return;

        if (currentScrollRect.vertical)
        {
            // Scroll one "page" (about 1/4 of the content)
            float pageSize = 0.25f;
            if (forward)
            {
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition - pageSize);
            }
            else
            {
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition + pageSize);
            }
        }
    }

    private void ScrollToSelection(GameObject selectedObject)
    {
        if (currentScrollRect == null || selectedObject == null) return;

        // Get the selected object's RectTransform
        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
        if (selectedRect == null) return;

        // Calculate the normalized position of the selected item
        Vector3 viewportLocalPos = currentScrollRect.viewport.InverseTransformPoint(currentScrollRect.content.position);
        Vector3 selectedLocalPos = currentScrollRect.viewport.InverseTransformPoint(selectedRect.position);

        // Check if the selected item is outside the viewport
        Rect viewportRect = currentScrollRect.viewport.rect;

        if (currentScrollRect.vertical)
        {
            float itemTop = selectedLocalPos.y + selectedRect.rect.height / 2;
            float itemBottom = selectedLocalPos.y - selectedRect.rect.height / 2;
            float viewportTop = viewportRect.yMax;
            float viewportBottom = viewportRect.yMin;

            // If item is below viewport, scroll down
            if (itemTop > viewportTop)
            {
                float excess = itemTop - viewportTop;
                float normalizedExcess = excess / currentScrollRect.content.rect.height;
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition - normalizedExcess);
            }
            // If item is above viewport, scroll up
            else if (itemBottom < viewportBottom)
            {
                float excess = viewportBottom - itemBottom;
                float normalizedExcess = excess / currentScrollRect.content.rect.height;
                currentScrollRect.verticalNormalizedPosition = Mathf.Clamp01(currentScrollRect.verticalNormalizedPosition + normalizedExcess);
            }
        }

        if (currentScrollRect.horizontal)
        {
            float itemRight = selectedLocalPos.x + selectedRect.rect.width / 2;
            float itemLeft = selectedLocalPos.x - selectedRect.rect.width / 2;
            float viewportRight = viewportRect.xMax;
            float viewportLeft = viewportRect.xMin;

            // If item is to the right of viewport, scroll right
            if (itemRight > viewportRight)
            {
                float excess = itemRight - viewportRight;
                float normalizedExcess = excess / currentScrollRect.content.rect.width;
                currentScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(currentScrollRect.horizontalNormalizedPosition + normalizedExcess);
            }
            // If item is to the left of viewport, scroll left
            else if (itemLeft < viewportLeft)
            {
                float excess = viewportLeft - itemLeft;
                float normalizedExcess = excess / currentScrollRect.content.rect.width;
                currentScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(currentScrollRect.horizontalNormalizedPosition - normalizedExcess);
            }
        }
    }

    private void EnsureSelection()
    {
        // Ensure something is selected when inventory is open and controller is being used
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            // Try to find any selectable in the inventory
            Selectable[] allSelectables = FindObjectsOfType<Selectable>();
            foreach (var selectable in allSelectables)
            {
                if (selectable.gameObject.activeInHierarchy && selectable.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                    break;
                }
            }
        }
    }

    public void SetInventoryOpen(bool open, ScrollRect scrollRect = null)
    {
        isInventoryOpen = open;
        currentScrollRect = scrollRect;

        if (open)
        {
            // When inventory opens, ensure something is selected
            EnsureSelection();
        }
        else
        {
            // When inventory closes, clear selection
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            currentScrollRect = null;
        }
    }

    public void SelectFirstSlot(GameObject slot)
    {
        if (EventSystem.current != null && slot != null)
        {
            EventSystem.current.SetSelectedGameObject(slot);

            // Scroll to make the selected slot visible
            ScrollToSelection(slot);
        }
    }
}