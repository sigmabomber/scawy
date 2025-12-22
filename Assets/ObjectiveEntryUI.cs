using Doody.Framework.ObjectiveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveEntryUI : MonoBehaviour
{
    public TMP_Text text;
    public Image completionIcon;

    [Header("Visual States")]

    private Objective data;
    private JournalUI journal;

    public void Setup(Objective objective, JournalUI journalUI)
    {
        data = objective;
        journal = journalUI;
        text = GetComponent<TMP_Text>();
        UpdateProgress(objective.CurrentValue);
        UpdateVisualState();
    }

    public void UpdateProgress(int currentValue)
    {
        if (data == null)
        {
            print(":(");
            return;
        }

        // Update text with description - progress format
        if (text)
        {
            text.text = $"{data.Description} - {currentValue}/{data.MaxValue}";
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (data == null)
        {
            print("::_F");
            return;
        }

        bool isComplete = data.IsComplete;

        if (text)
        {
            if (isComplete)
            {
                text.fontStyle = FontStyles.Strikethrough;
            }
           
        }

        // Show/hide completion icon
        if (completionIcon)
        {
            completionIcon.gameObject.SetActive(isComplete);
        }
    }
}