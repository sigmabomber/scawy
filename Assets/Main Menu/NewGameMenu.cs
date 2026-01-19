using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles creating new game saves with overwrite confirmation
/// </summary>
public class NewGameMenu : BaseSlotMenu
{
    [Header("New Game Specific")]
    public GameObject Confirmation;

    public static NewGameMenu Instance;

    private int pendingSlotNumber = -1;
    private bool waitingForConfirmation = false;

    protected override void Start()
    {
        base.Start();
        Instance = this;
    }

    protected override void CreateAllSlots()
    {
        for (int i = 0; i < SaveEventManager.Instance.maxSlots; i++)
        {
            bool hasSave = SaveEventManager.Instance.SaveExists(i);
            GameObject newSlot = CreateSlotObject(i, hasSave);

            // Add hover effects for existing saves
            if (hasSave)
            {
                Transform overrideButton = newSlot.transform.Find("HasSave/Override");
                if (overrideButton != null)
                {
                    var hoverEvent = newSlot.GetComponent<ButtonHoverEvent>();
                    if (hoverEvent != null)
                    {
                        hoverEvent.OnHoverEnter.AddListener(() => OnButtonHovered(overrideButton));
                        hoverEvent.OnHoverExit.AddListener(() => OnButtonHoveredExit(overrideButton));
                    }
                }
            }

            // Set button click listener
            int slotIndex = i;
            newSlot.GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(slotIndex, hasSave));
        }
    }

    #region Hover Effects

    private void OnButtonHovered(Transform button)
    {
        var canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            StartCoroutine(FadeIn(canvasGroup));
        }
    }

    private void OnButtonHoveredExit(Transform button)
    {
        var canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            StartCoroutine(FadeOut(canvasGroup));
        }
    }

    #endregion

    #region Button Actions

    private void OnButtonClicked(int slotNumber, bool hasSave)
    {
        if (hasSave)
        {
            // Show confirmation dialog for overwrite
            pendingSlotNumber = slotNumber;
            waitingForConfirmation = true;
            Confirmation.SetActive(true);
        }
        else
        {
            // No save exists, proceed directly
            StartNewGame(slotNumber);
        }
    }

    public void OnConfirmOverwrite()
    {
        if (waitingForConfirmation && pendingSlotNumber >= 0)
        {
            StartNewGame(pendingSlotNumber);
        }
        ResetConfirmationState();
    }

    public void OnCancelOverwrite()
    {
        ResetConfirmationState();
    }

    private void ResetConfirmationState()
    {
        waitingForConfirmation = false;
        pendingSlotNumber = -1;
        Confirmation.SetActive(false);
    }

    #endregion

    #region Game Start

    private void StartNewGame(int slotNumber)
    {
        // Delete existing save if present
        if (SaveEventManager.Instance.SaveExists(slotNumber))
        {
            SaveEventManager.Instance.DeleteSave(slotNumber);
        }

        // Save to slot and load next scene
        SaveEventManager.Instance.SaveToSlot(slotNumber);
        StartCoroutine(FadeAndLoadScene(slotNumber, 0f));
    }

    #endregion
}