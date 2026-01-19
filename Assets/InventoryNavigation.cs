using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class InventoryNavigation : MonoBehaviour
{
    public static InventoryNavigation Instance { get; private set; }

    [Header("Navigation Settings")]
    public float navigationDelay = 0.2f;
    public float stickDeadzone = 0.1f;

    [Header("Focus Settings")]
    public Color focusColor = Color.yellow;
    public float focusScale = 1.05f;

    [Header("Debug")]
    public bool showDebug = true;
    public float debugDisplayTime = 2f;

    private float lastNavigationTime;
    private bool isInventoryOpen = false;
    private InventorySlotsUI currentFocusedSlot;
    private List<InventorySlotsUI> allSlots = new List<InventorySlotsUI>();
    private List<InventorySlotsUI> filteredSlots = new List<InventorySlotsUI>();
    private GameObject currentInventoryPanel;
    private string lastDebugMessage = "";
    private float lastDebugTime;
    private Vector2 lastProcessedInput = Vector2.zero;

    public GameObject xboxInfo;
    public GameObject psInfo;


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
        if (!isInventoryOpen) return;

        HandleNavigation();



        if (InputDetector.Instance != null && InputDetector.Instance.IsUsingController())
        {
            string controller = InputDetector.Instance.GetControllerName();

            if (controller != null)
            {
                if (controller == "PlayStation")
                {
                    if (psInfo != null && xboxInfo != null)
                    {
                        psInfo.SetActive(true);
                        xboxInfo.SetActive(false);
                    }
                }
                else 
                {
                    if (psInfo != null && xboxInfo != null)
                    {
                        psInfo.SetActive(false);
                        xboxInfo.SetActive(true);
                    }
                }
            }
            else
            {
                if (psInfo != null && xboxInfo != null)
                {
                    psInfo.SetActive(false);
                    xboxInfo.SetActive(false);
                }
            }
        }
        else 
        {
            if (psInfo != null && xboxInfo != null)
            {
                psInfo.SetActive(false);
                xboxInfo.SetActive(false);
            }
        }
    }

    public void SetInventoryOpen(bool open, GameObject inventoryPanel = null)
    {
        LogDebug($"SetInventoryOpen: {open}, Panel: {inventoryPanel}");
        isInventoryOpen = open;
        currentInventoryPanel = inventoryPanel;

        if (open && inventoryPanel != null)
        {
            UpdateAllSlots();
            FilterSlotsForNavigation();

            if (filteredSlots.Count > 0)
            {
                LogDebug($"Found {filteredSlots.Count} navigable slots.");
                FocusSlot(filteredSlots[0]);
            }
            else if (allSlots.Count > 0)
            {
                LogDebug($"No navigable slots found, but have {allSlots.Count} total slots.");
                ClearFocus();
            }
            else
            {
                LogDebug("No slots found.");
                ClearFocus();
            }

            // Reset navigation state
            lastNavigationTime = Time.time - navigationDelay;
            lastProcessedInput = Vector2.zero;
        }
        else
        {
            // Clear focus when inventory closes
            LogDebug("Clearing inventory focus");
            ClearFocus();
            allSlots.Clear();
            filteredSlots.Clear();
            currentInventoryPanel = null;

            // Also clean up any controller drag
            if (InventorySlotsUI.controllerDragSource != null)
            {
                InventorySlotsUI.controllerDragSource.EndControllerDrag();
            }
        }
    }

    public void RefreshSlots()
    {
        LogDebug("Refreshing all slots");
        if (currentInventoryPanel != null && isInventoryOpen)
        {
            UpdateAllSlots();
            FilterSlotsForNavigation();

            if (currentFocusedSlot != null && !filteredSlots.Contains(currentFocusedSlot))
            {
                LogDebug($"Current focused slot {currentFocusedSlot.name} no longer navigable");
                if (filteredSlots.Count > 0)
                {
                    LogDebug($"Focusing new slot: {filteredSlots[0].name}");
                    FocusSlot(filteredSlots[0]);
                }
                else
                {
                    LogDebug("No navigable slots available");
                    ClearFocus();
                }
            }
        }
    }

    private void UpdateAllSlots()
    {
        allSlots.Clear();

        if (currentInventoryPanel == null)
        {
            LogDebug("No inventory panel set");
            return;
        }

        var foundSlots = currentInventoryPanel.GetComponentsInChildren<InventorySlotsUI>(true);
        LogDebug($"Found {foundSlots.Length} total slots");

        foreach (var slot in foundSlots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;

            allSlots.Add(slot);
            LogDebug($"Slot {slot.name} - Has item: {slot.itemData != null}, Priority: {slot.slotPriority}");
        }
    }

    private void FilterSlotsForNavigation()
    {
        filteredSlots.Clear();
        bool isDragging = InventorySlotsUI.controllerDragSource != null;

        foreach (var slot in allSlots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;

            bool isNavigable = true;

            // Rule 1: If not dragging, skip empty slots
            if (!isDragging && slot.itemData == null)
            {
                isNavigable = false;
            }

            // Rule 2: When dragging, only show slots with same priority as drag source
            if (isDragging && isNavigable)
            {
                var dragSource = InventorySlotsUI.controllerDragSource;
                if (dragSource != null && dragSource.slotPriority != slot.slotPriority)
                {
                    isNavigable = false;
                }
            }

            if (isNavigable)
            {
                filteredSlots.Add(slot);
            }
        }

        LogDebug($"Filtered {allSlots.Count} slots down to {filteredSlots.Count} navigable slots (isDragging: {isDragging})");
    }

    private void HandleNavigation()
    {
        if (filteredSlots.Count == 0)
        {
            LogDebug("No navigable slots to navigate");
            return;
        }

        var gamepad = Gamepad.current;
        if (gamepad == null)
        {
            LogDebug("No gamepad connected");
            return;
        }

        Vector2 input = gamepad.leftStick.ReadValue();
       

        // Check if we have valid input above deadzone
        bool hasValidInput = input.magnitude > stickDeadzone;

        if (hasValidInput)
        {
            Vector2 direction = input.normalized;

            if (lastProcessedInput == Vector2.zero ||
                Vector2.Angle(lastProcessedInput, direction) > 30f ||
                Time.time - lastNavigationTime >= navigationDelay)
            {
                LogDebug($"Navigation input: {input}, magnitude: {input.magnitude:F2}, direction: {direction}");
                NavigateSlots(direction);
                lastNavigationTime = Time.time;
                lastProcessedInput = direction;
            }
            else
            {
                LogDebug($"Waiting for delay or direction change. Time diff: {Time.time - lastNavigationTime:F2}");
            }
        }
        else
        {
            // Reset processed input when stick is released
            lastProcessedInput = Vector2.zero;
        }

        // Handle button inputs
        if (currentFocusedSlot != null)
        {
            if (gamepad.aButton.wasPressedThisFrame)
            {
                LogDebug("A Button pressed - Select");
                currentFocusedSlot.OnControllerSelect();
                RefreshSlots();
            }
            else if (gamepad.xButton.wasPressedThisFrame)
            {
                LogDebug("X Button pressed - Transfer/Drag");
                currentFocusedSlot.OnControllerTransfer();
                RefreshSlots();
            }
            else if (gamepad.bButton.wasPressedThisFrame)
            {
                LogDebug("Y Button pressed - Drop");
                currentFocusedSlot.OnControllerDrop();
                RefreshSlots();
            }
        }
    }

    private void NavigateSlots(Vector2 inputDirection)
    {
        if (currentFocusedSlot == null || filteredSlots.Count == 0)
            return;

        Vector2 direction = GetCardinalDirection(inputDirection);
        Vector2 currentPos = GetSlotPosition(currentFocusedSlot);

        InventorySlotsUI bestSlot = null;
        float bestDistance = float.MaxValue;

        bool isDragging = InventorySlotsUI.controllerDragSource != null;

        foreach (var slot in filteredSlots)
        {
            if (slot == null || slot == currentFocusedSlot || !slot.gameObject.activeInHierarchy)
                continue;

            Vector2 slotPos = GetSlotPosition(slot);
            Vector2 delta = slotPos - currentPos;

            if (Vector2.Dot(delta.normalized, direction) <= 0.01f)
                continue;

            if (!isDragging)
            {
                if (direction == Vector2.up || direction == Vector2.down)
                {
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        continue;
                }
                else
                {
                    if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                        continue;
                }
            }

            float distance = delta.magnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = slot;
            }
        }

        if (bestSlot == null && filteredSlots.Count > 1)
            bestSlot = FindWrappedSlot(direction);

        if (bestSlot != null)
            FocusSlot(bestSlot);
    }

    private Vector2 GetCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0 ? Vector2.right : Vector2.left;
        else
            return input.y > 0 ? Vector2.up : Vector2.down;
    }

    private InventorySlotsUI FindWrappedSlot(Vector2 direction)
    {
        if (filteredSlots.Count <= 1) return null;

        InventorySlotsUI farthestSlot = null;
        float maxDistance = float.MinValue;
        Vector2 currentPos = GetSlotPosition(currentFocusedSlot);

        foreach (var slot in filteredSlots)
        {
            if (slot == null || slot == currentFocusedSlot || !slot.gameObject.activeInHierarchy) continue;

            Vector2 slotPos = GetSlotPosition(slot);
            Vector2 diff = slotPos - currentPos;
            float distance = diff.magnitude;

            bool isOppositeDirection = false;

            if (direction.y > 0)
            {
                isOppositeDirection = diff.y < 0;
            }
            else if (direction.y < 0)
            {
                isOppositeDirection = diff.y > 0;
            }
            else if (direction.x > 0)
            {
                isOppositeDirection = diff.x < 0;
            }
            else if (direction.x < 0)
            {
                isOppositeDirection = diff.x > 0;
            }

            if (isOppositeDirection && distance > maxDistance)
            {
                maxDistance = distance;
                farthestSlot = slot;
            }
        }

        return farthestSlot;
    }

    private Vector2 GetSlotPosition(InventorySlotsUI slot)
    {
        if (slot == null) return Vector2.zero;

        RectTransform rect = slot.GetComponent<RectTransform>();
        if (rect != null)
        {
            Canvas canvas = slot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return rect.position;
            }
            return rect.anchoredPosition;
        }

        return slot.transform.position;
    }

    private void FocusSlot(InventorySlotsUI slot)
    {
        LogDebug($"FocusSlot: {slot?.name}");

        // Clear previous focus
        ClearFocus();

        // Set new focus
        currentFocusedSlot = slot;
        if (currentFocusedSlot != null)
        {
            currentFocusedSlot.SetControllerFocus(true, focusColor, focusScale);

            // Auto-scroll if needed
            ScrollToSlot(currentFocusedSlot);
        }
    }

    private void ClearFocus()
    {
        if (currentFocusedSlot != null)
        {
            LogDebug($"Clearing focus from {currentFocusedSlot.name}");
            currentFocusedSlot.SetControllerFocus(false);
            currentFocusedSlot = null;
        }
    }

    private void ScrollToSlot(InventorySlotsUI slot)
    {
        ScrollRect scrollRect = slot.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            LogDebug("No ScrollRect found for scrolling");
            return;
        }

        RectTransform slotRect = slot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        Canvas.ForceUpdateCanvases();

        RectTransform contentPanel = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        Vector3 slotLocalPosition = contentPanel.InverseTransformPoint(slotRect.position);
        Vector3 viewportLocalPosition = contentPanel.InverseTransformPoint(viewport.position);

        float verticalOffset = slotLocalPosition.y - viewportLocalPosition.y;
        float contentHeight = contentPanel.rect.height;
        float viewportHeight = viewport.rect.height;

        if (scrollRect.vertical)
        {
            float normalizedPosition = scrollRect.verticalNormalizedPosition;
            float scrollAmount = verticalOffset / (contentHeight - viewportHeight);

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition + scrollAmount);
            LogDebug($"Scrolling to slot. Vertical offset: {verticalOffset:F1}, New pos: {scrollRect.verticalNormalizedPosition:F2}");
        }
    }

    private void LogDebug(string message)
    {
        if (!showDebug) return;

        lastDebugMessage = $"[{Time.time:F2}] {message}";
        lastDebugTime = Time.time;
        Debug.Log($"InventoryNavigation: {message}");
    }
}