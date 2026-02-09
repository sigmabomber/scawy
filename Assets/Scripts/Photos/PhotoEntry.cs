using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Doody.Framework.PhotoSystem;

public class PhotoEntryUI : MonoBehaviour
{
    [Header("UI References")]
    public Image photoImage;
    public Button photoButton;
    public GameObject contentContainer;
    public TMP_Text descriptionText;
    public TMP_Text dateText;

    [Header("Animation")]
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Static reference to track currently expanded photo
    private static PhotoEntryUI currentExpandedPhoto = null;

    private bool isExpanded = false;
    private LayoutElement layoutElement;
    private Photo photo;
    private JournalUI journal;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Transform contentOriginalParent;
    private int contentOriginalSiblingIndex;
    private Coroutine animationCoroutine;
    private Vector2 collapsedSize;
    private Vector2 expandedSize;


    public GameObject photoInspectionScreen;
    public Image photoInspectionImage;

    public void Setup(Photo photoData, JournalUI journalUI)
    {
        photo = photoData;
        journal = journalUI;

        // Set photo sprite
        if (photoImage != null)
            photoImage.sprite = photo.PhotoSprite;

        // Set description text
        if (descriptionText != null)
            descriptionText.text = photo.Description;

        // Set date text
        if (dateText != null)
            dateText.text = photo.Date;

        // Store original parent and sibling index
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Store original content parent and sibling index
        if (contentContainer != null)
        {
            contentOriginalParent = contentContainer.transform.parent;
            contentOriginalSiblingIndex = contentContainer.transform.GetSiblingIndex();

            // Setup LayoutElement on the content container
            layoutElement = contentContainer.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = contentContainer.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 0;

            // Start collapsed
            contentContainer.SetActive(false);
        }

        // Store sizes for animation
        RectTransform photoRect = photoImage.GetComponent<RectTransform>();
        if (photoRect != null)
        {
            collapsedSize = photoRect.sizeDelta;
            expandedSize = new Vector2(collapsedSize.x * 1.5f, collapsedSize.y * 1.5f); // 1.5x larger when expanded
        }

        // Button listener
        if (photoButton != null)
        {
            photoButton.onClick.RemoveAllListeners();
            photoButton.onClick.AddListener(ToggleContent);
        }
    }

    private void ToggleContent()
    {
        // If this photo is already expanded, collapse it
        if (isExpanded)
        {
            CollapsePhoto();
            return;
        }

        // If another photo is currently expanded, collapse it first
        if (currentExpandedPhoto != null && currentExpandedPhoto != this)
        {
            currentExpandedPhoto.CollapsePhoto();
        }

        // Expand this photo
        ExpandPhoto();
    }

    private void ExpandPhoto()
    {
        isExpanded = true;
        currentExpandedPhoto = this;




    }

    private void CollapsePhoto(bool immediate = false)
    {
        if (!isExpanded) return;

        isExpanded = false;

        // Only clear the static reference if this is the currently expanded photo
        if (currentExpandedPhoto == this)
        {
            currentExpandedPhoto = null;
        }

        if (immediate)
        {
            // Immediate collapse without animation
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = 0;
            }

            if (contentContainer != null)
            {
                contentContainer.SetActive(false);
                ReturnContentToPrefab();
            }

            RectTransform photoRect = photoImage.GetComponent<RectTransform>();
            if (photoRect != null)
            {
                photoRect.sizeDelta = collapsedSize;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)originalParent);
        }
        else
        {
            // Stop any running coroutines
            StopAllCoroutines();

            // Animate content height
            if (contentContainer != null)
            {
                animationCoroutine = StartCoroutine(AnimateContentHeight(false));
            }

            // Scale down photo
            StartCoroutine(AnimatePhotoScale(false));
        }
    }

    private void ExtractContentUnderPrefab()
    {
        // Calculate the current position of this photo in the parent
        int currentSiblingIndex = transform.GetSiblingIndex();

        // Place content right after this photo
        int placementIndex = currentSiblingIndex + 1;

        // Move the content container to the parent (journal's container)
        contentContainer.transform.SetParent(originalParent);

        // Position it right after this photo
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
        float targetHeight = expand ? GetContentHeight() : 0f;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = scaleCurve.Evaluate(elapsed / animationDuration);
            layoutElement.preferredHeight = Mathf.Lerp(startHeight, targetHeight, t);

            // Rebuild parent layout so photos below move
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

    private IEnumerator AnimatePhotoScale(bool expand)
    {
        RectTransform photoRect = photoImage.GetComponent<RectTransform>();
        if (photoRect == null) yield break;

        Vector2 startSize = photoRect.sizeDelta;
        Vector2 targetSize = expand ? expandedSize : collapsedSize;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = scaleCurve.Evaluate(elapsed / animationDuration);
            photoRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            yield return null;
        }

        photoRect.sizeDelta = targetSize;
    }

    private float GetContentHeight()
    {
        float height = 0f;

        if (descriptionText != null && descriptionText.gameObject.activeSelf)
        {
            height += descriptionText.preferredHeight;
        }

        if (dateText != null && dateText.gameObject.activeSelf)
        {
            height += dateText.preferredHeight;
        }

        // Add some padding
        height += 20f;

        return height;
    }

    private void OnDestroy()
    {
        // Clean up by returning content to prefab if expanded
        if (isExpanded)
        {
            // Stop any running coroutines first
            StopAllCoroutines();

            // Immediate collapse without animation
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = 0;
            }

            if (contentContainer != null)
            {
                contentContainer.SetActive(false);
                ReturnContentToPrefab();
            }

            RectTransform photoRect = photoImage.GetComponent<RectTransform>();
            if (photoRect != null)
            {
                photoRect.sizeDelta = collapsedSize;
            }

            // Clear static reference if this was the current expanded photo
            if (currentExpandedPhoto == this)
            {
                currentExpandedPhoto = null;
            }
        }
    }

    // Static method to collapse any currently expanded photo
    public static void CollapseCurrentExpandedPhoto()
    {
        if (currentExpandedPhoto != null)
        {
            currentExpandedPhoto.CollapsePhoto();
        }
    }
}