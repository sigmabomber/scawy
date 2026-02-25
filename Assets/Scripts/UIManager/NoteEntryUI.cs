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

    // Static reference to track currently expanded note
    private static NoteEntryUI currentExpandedNote = null;

    private bool isExpanded = false;
    private LayoutElement layoutElement;
    private Note note;
    private JournalUI journal;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Transform contentOriginalParent;
    private int contentOriginalSiblingIndex;
    private Coroutine animationCoroutine;
    private Coroutine rotationCoroutine;

    public void Setup(Note noteData, JournalUI journalUI)
    {
        note = noteData;
        journal = journalUI;

        // Set header text
        headerText.text = string.IsNullOrEmpty(note.Date) ? note.Title : $"{note.Title} - {note.Date}";

        // Set content text
        contentText.text = note.Content;

        // Store original parent and sibling index
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Store original content parent and sibling index
        contentOriginalParent = contentContainer.transform.parent;
        contentOriginalSiblingIndex = contentContainer.transform.GetSiblingIndex();

        // Setup LayoutElement on the content container
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
        // If this note is already expanded, collapse it
        if (isExpanded)
        {
            CollapseNote();
            return;
        }

        // If another note is currently expanded, collapse it first
        if (currentExpandedNote != null && currentExpandedNote != this)
        {
            currentExpandedNote.CollapseNote();
        }

        // Expand this note
        ExpandNote();
    }

    private void ExpandNote()
    {
        isExpanded = true;
        currentExpandedNote = this;

        // Place content container directly under the prefab in the parent container
        ExtractContentUnderPrefab();

        // Stop any running coroutines
        StopAllCoroutines();

        // Animate content height
        animationCoroutine = StartCoroutine(AnimateContentHeight(true));

        // Rotate arrow
        rotationCoroutine = StartCoroutine(RotateArrow(true));
    }

    private void CollapseNote(bool immediate = false)
    {
        if (!isExpanded) return;

        isExpanded = false;

        // Only clear the static reference if this is the currently expanded note
        if (currentExpandedNote == this)
        {
            currentExpandedNote = null;
        }

        if (immediate)
        {
            // Immediate collapse without animation
            layoutElement.preferredHeight = 0;
            contentContainer.SetActive(false);
            arrowButton.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            ReturnContentToPrefab();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)originalParent);
        }
        else
        {
            // Stop any running coroutines
            StopAllCoroutines();

            // Animate content height
            animationCoroutine = StartCoroutine(AnimateContentHeight(false));

            // Rotate arrow
            rotationCoroutine = StartCoroutine(RotateArrow(false));
        }
    }

    private void ExtractContentUnderPrefab()
    {
        // Calculate the current position of this note in the parent
        // (it might have changed due to other notes being added/removed)
        int currentSiblingIndex = transform.GetSiblingIndex();

        // Place content right after this note
        int placementIndex = currentSiblingIndex + 1;

        // Move the content container to the parent (journal's container)
        contentContainer.transform.SetParent(originalParent);

        // Position it right after this note
        contentContainer.transform.SetSiblingIndex(placementIndex);

        // Reset transform for proper UI layout
        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.localScale = Vector3.one;
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 0);
        }
    }

    private void ReturnContentToPrefab()
    {
        // Move content container back to original position in prefab
        contentContainer.transform.SetParent(contentOriginalParent);
        contentContainer.transform.SetSiblingIndex(contentOriginalSiblingIndex);
        contentContainer.transform.localScale = Vector3.one;
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
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)originalParent);

            yield return null;
        }

        layoutElement.preferredHeight = targetHeight;

        if (!expand)
        {
            contentContainer.SetActive(false);
            ReturnContentToPrefab();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)originalParent);
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
            // Make sure content is returned to prefab before deletion
            if (isExpanded)
            {
                CollapseNote(true); // Use immediate collapse for deletion
            }

            journal.DeleteNote(note.Id);
        }
    }

    private void OnDestroy()
    {
        // Clean up by returning content to prefab if expanded
        if (isExpanded)
        {
            // Stop any running coroutines first
            StopAllCoroutines();

            // Immediate collapse without animation
            layoutElement.preferredHeight = 0;
            contentContainer.SetActive(false);
            arrowButton.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            ReturnContentToPrefab();

            // Clear static reference if this was the current expanded note
            if (currentExpandedNote == this)
            {
                currentExpandedNote = null;
            }
        }
    }

    // Static method to collapse any currently expanded note
    public static void CollapseCurrentExpandedNote()
    {
        if (currentExpandedNote != null)
        {
            currentExpandedNote.CollapseNote();
        }
    }
}