using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class StorageNavigation : MonoBehaviour
{
    public static StorageNavigation Instance { get; private set; }

    [Header("Navigation Settings")]
    public float navigationDelay = 0.2f;
    public float stickDeadzone = 0.1f;

    [Header("Focus Settings")]
    public Color storageFocusColor = Color.cyan;
    public Color inventoryFocusColor = Color.yellow;
    public float focusScale = 1.05f;

    private float lastNavigationTime;
    private bool isStorageOpen = false;
    private StorageSlotUI currentFocusedSlot;
    private List<StorageSlotUI> allSlots = new List<StorageSlotUI>();
    private Vector2 lastProcessedInput = Vector2.zero;

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
        if (!isStorageOpen) return;

        HandleNavigation();
        HandleButtons();
    }

    public void SetStorageOpen(bool open)
    {
        isStorageOpen = open;

        if (open)
        {
            if (StorageUIManager.Instance != null)
            {
                allSlots = StorageUIManager.Instance.GetAllStorageSlotsIncludingInventory();

                if (allSlots.Count > 0)
                {
                    FocusSlot(allSlots[0]);
                }
            }
        }
        else
        {
            ClearFocus();
            allSlots.Clear();
        }
    }

    private void HandleNavigation()
    {
        if (allSlots.Count == 0) return;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 input = gamepad.leftStick.ReadValue();
        bool hasValidInput = input.magnitude > stickDeadzone;

        if (hasValidInput)
        {
            Vector2 direction = input.normalized;

            if (
                lastProcessedInput == Vector2.zero ||
                Vector2.Angle(lastProcessedInput, direction) > 30f ||
                Time.time - lastNavigationTime >= navigationDelay
            )
            {
                NavigateSlots(direction);
                lastNavigationTime = Time.time;
                lastProcessedInput = direction;
            }
        }
        else
        {
            lastProcessedInput = Vector2.zero;
        }
    }

    private void NavigateSlots(Vector2 direction)
    {
        if (currentFocusedSlot == null || allSlots.Count <= 1)
            return;

        RectTransform currentRect = currentFocusedSlot.GetComponent<RectTransform>();
        if (currentRect == null) return;

        Vector2 currentPos = currentRect.position;

        StorageSlotUI bestSlot = null;
        float bestScore = float.MaxValue;

        foreach (var slot in allSlots)
        {
            if (slot == null || slot == currentFocusedSlot || !slot.gameObject.activeInHierarchy)
                continue;

            RectTransform rect = slot.GetComponent<RectTransform>();
            if (rect == null) continue;

            Vector2 slotPos = rect.position;
            Vector2 delta = slotPos - currentPos;

            if (Vector2.Dot(delta.normalized, direction) < 0.6f)
                continue;

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

            float distance = delta.magnitude;

            if (distance < bestScore)
            {
                bestScore = distance;
                bestSlot = slot;
            }
        }

        if (bestSlot != null)
        {
            FocusSlot(bestSlot);
        }
    }

    private void FocusSlot(StorageSlotUI slot)
    {
        if (slot == null) return;

        if (currentFocusedSlot != null)
        {
            currentFocusedSlot.SetControllerFocus(false);
        }

        currentFocusedSlot = slot;

        Color focusColor = slot.slotType == StorageSlotUI.SlotType.Storage ?
            storageFocusColor : inventoryFocusColor;

        currentFocusedSlot.SetControllerFocus(true, focusColor, focusScale);
    }

    private void ClearFocus()
    {
        if (currentFocusedSlot != null)
        {
            currentFocusedSlot.SetControllerFocus(false);
            currentFocusedSlot = null;
        }
    }

    private void HandleButtons()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null || currentFocusedSlot == null) return;

        if (gamepad.yButton.wasPressedThisFrame)          
        {
            currentFocusedSlot.OnControllerSelect();
        }
        else if (gamepad.xButton.wasPressedThisFrame)     
        {
            currentFocusedSlot.OnControllerTransfer();
        }
        else if (gamepad.bButton.wasPressedThisFrame)     
        {
            currentFocusedSlot.OnControllerDrop();
        }
       

        if (gamepad.startButton.wasPressedThisFrame)
        {
            if (StorageUIManager.Instance != null)
            {
                StorageUIManager.Instance.CloseStorage();
            }
        }
    }

    public void RefreshSlots()
    {
        if (!isStorageOpen) return;

        if (StorageUIManager.Instance != null)
        {
            allSlots = StorageUIManager.Instance.GetAllStorageSlotsIncludingInventory();
        }

        if (currentFocusedSlot != null && !allSlots.Contains(currentFocusedSlot) && allSlots.Count > 0)
        {
            FocusSlot(allSlots[0]);
        }
    }

    public StorageSlotUI GetFocusedSlot()
    {
        return currentFocusedSlot;
    }
}