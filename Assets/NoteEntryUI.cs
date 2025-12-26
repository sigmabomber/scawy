using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Doody.Framework.NoteSystem;

public class NoteEntryUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text headerText;
    public Button arrowButton;
    public GameObject contentContainer;
    public TMP_Text contentText;

    [Header("Animation")]
    public float animationDuration = 0.3f;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional")]
    public Button deleteButton;

    private bool isExpanded = false;
    private LayoutElement layoutElement;
    private Note note;
    private JournalUI journal;

    public void Setup(Note noteData, JournalUI journalUI)
    {
        note = noteData;
        journal = journalUI;

        // Set header text
        headerText.text = string.IsNullOrEmpty(note.Date) ? note.Title : $"{note.Title} - {note.Date}";

        // Set content text
        contentText.text = note.Content;

        // Setup LayoutElement
        layoutElement = contentContainer.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = contentContainer.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 0;

        // Start collapsed
        contentContainer.SetActive(false);

        // Arrow rotation
        arrowButton.transform.localRotation = Quaternion.Euler(0, 0, 90f);

        // Button listeners
        arrowButton.onClick.RemoveAllListeners();
        arrowButton.onClick.AddListener(ToggleContent);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDelete);
        }
    }

    private void ToggleContent()
    {
        isExpanded = !isExpanded;

        // Animate content height
        StartCoroutine(AnimateContentHeight(isExpanded));

        // Rotate arrow
        StartCoroutine(RotateArrow(isExpanded));
    }

    private IEnumerator AnimateContentHeight(bool expand)
    {
        contentContainer.SetActive(true);

        float startHeight = layoutElement.preferredHeight;
        float targetHeight = expand ? contentText.preferredHeight : 0f;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = rotationCurve.Evaluate(elapsed / animationDuration);
            layoutElement.preferredHeight = Mathf.Lerp(startHeight, targetHeight, t);

            // Rebuild parent layout so notes below move
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
            yield return null;
        }

        layoutElement.preferredHeight = targetHeight;

        if (!expand)
            contentContainer.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
    }

    private IEnumerator RotateArrow(bool expand)
    {
        float startRot = arrowButton.transform.localEulerAngles.z;
        float endRot = expand ? 0f : 90f;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = rotationCurve.Evaluate(elapsed / animationDuration);
            float z = Mathf.LerpAngle(startRot, endRot, t);
            arrowButton.transform.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }

        arrowButton.transform.localRotation = Quaternion.Euler(0, 0, endRot);
    }

    private void OnDelete()
    {
        if (journal != null && note != null)
        {
            journal.DeleteNote(note.Id);
        }
    }
}
