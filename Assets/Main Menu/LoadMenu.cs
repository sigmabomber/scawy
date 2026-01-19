using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles loading existing game saves
/// </summary>
public class LoadMenu : BaseSlotMenu
{
    public static LoadMenu Instance;

    protected override void Start()
    {
        base.Start();
        Instance = this;
    }

    protected override void CreateAllSlots()
    {
        for (int i = 0; i < SaveEventManager.Instance.maxSlots; i++)
        {
            // Only show slots that have saves
            if (!SaveEventManager.Instance.SaveExists(i))
            {
                continue; // Changed from return to continue to check all slots
            }

            GameObject newSlot = CreateSlotObject(i, true);

            // Set button click listener
            int slotIndex = i;
            newSlot.GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(slotIndex));
        }
    }

    private void OnButtonClicked(int slotNumber)
    {
        StartCoroutine(FadeAndLoadScene(slotNumber, 0.9f));
    }

    public void GoBack()
    {
        // Implement back functionality if needed
    }
}