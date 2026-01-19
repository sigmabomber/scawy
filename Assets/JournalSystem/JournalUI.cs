using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Doody.Framework.ObjectiveSystem;
using Doody.Framework.NoteSystem;
using Doody.GameEvents;
using Doody.Framework.UI;
using TMPro;

public class JournalUI : MonoBehaviour
{
    [Header("Section Buttons")]
    public Button objectivesButton;
    public Button notesButton;
    public Button photosButton;

    [Header("Content Area")]
    public Transform contentContainer;

    [Header("Page Navigation")]
    public Button previousPageButton;
    public Button nextPageButton;
    public TMP_Text pageNumberText;
    [SerializeField] private int entriesPerPage = 5;

    [Header("Prefabs")]
    public GameObject objectiveEntryPrefab;
    public GameObject noteEntryPrefab;
    public GameObject photoEntryPrefab;

    [Header("Settings")]
    public GameObject journal;
    [SerializeField] private KeyCode journalKeycode = KeyCode.J;
    [SerializeField] private string contentTextName = "ContentText";

    private GameObject objectiveOutline;
    private GameObject noteOutline;
    private GameObject photoOutline;

    private JournalSection currentSection = JournalSection.Objectives;
    private int currentPage = 0;
    private int totalPages = 0;

    private List<PhotoEntry> photos = new List<PhotoEntry>();

    private void Start()
    {
        if (objectivesButton != null)
        {
            objectiveOutline = objectivesButton.transform.Find("Outline").gameObject;
            objectivesButton.onClick.AddListener(() =>
            {
                if (photoOutline != null) photoOutline.SetActive(false);
                if (noteOutline != null) noteOutline.SetActive(false);
                if (objectiveOutline != null) objectiveOutline.SetActive(true);
                ShowSection(JournalSection.Objectives);
            });
        }

        if (notesButton != null)
        {
            noteOutline = notesButton.transform.Find("Outline").gameObject;
            notesButton.onClick.AddListener(() =>
            {
                if (photoOutline != null) photoOutline.SetActive(false);
                if (noteOutline != null) noteOutline.SetActive(true);
                if (objectiveOutline != null) objectiveOutline.SetActive(false);
                ShowSection(JournalSection.Notes);
            });
        }

        if (photosButton != null)
        {
            photoOutline = photosButton.transform.Find("Outline").gameObject;
            photosButton.onClick.AddListener(() =>
            {
                if (photoOutline != null) photoOutline.SetActive(true);
                if (noteOutline != null) noteOutline.SetActive(false);
                if (objectiveOutline != null) objectiveOutline.SetActive(false);
                ShowSection(JournalSection.Photos);
            });
        }

        // Setup page navigation buttons
        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(() => ChangePage(-1));
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(() => ChangePage(1));
        }

        // Subscribe to objective events
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveAdded += OnObjectiveAdded;
            ObjectiveManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
        }

        // Subscribe to note events
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNoteAdded += OnNoteAdded;
            NoteManager.Instance.OnNoteUpdated += OnNoteUpdated;
            NoteManager.Instance.OnNoteDeleted += OnNoteDeleted;
        }

        // Show objectives section by default
        ShowSection(JournalSection.Objectives);

        // Load saved data
        LoadJournalData();
    }

    private void OnDestroy()
    {
        // Unsubscribe from objective events
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveAdded -= OnObjectiveAdded;
            ObjectiveManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
        }

        // Unsubscribe from note events
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNoteAdded -= OnNoteAdded;
            NoteManager.Instance.OnNoteUpdated -= OnNoteUpdated;
            NoteManager.Instance.OnNoteDeleted -= OnNoteDeleted;
        }
    }

    private void OnObjectiveAdded(Objective objective)
    {
        // Refresh objectives view if currently viewing
        if (currentSection == JournalSection.Objectives)
        {
            LoadObjectivesContent();
        }
    }

    private void Update()
    {
        if (Input.GetKeyUp(journalKeycode))
        {
            bool isOpen = !journal.activeSelf;
            Events.Publish(new UIRequestToggleEvent(journal));

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Keyboard shortcuts for page navigation
        if (journal != null && journal.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ChangePage(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangePage(1);
            }
        }
    }

    private void OnObjectiveCompleted(Objective objective)
    {
        // Refresh objectives view if currently viewing
        if (currentSection == JournalSection.Objectives)
        {
            LoadObjectivesContent();
        }

        Debug.Log($"Objective completed: {objective.Name}");
    }

    // Add note event handlers
    private void OnNoteAdded(Note note)
    {
        if (currentSection == JournalSection.Notes)
        {
            LoadNotesContent();
        }
    }

    private void OnNoteUpdated(Note note)
    {
        if (currentSection == JournalSection.Notes)
        {
            LoadNotesContent();
        }
    }

    private void OnNoteDeleted(string noteId)
    {
        if (currentSection == JournalSection.Notes)
        {
            LoadNotesContent();
        }
    }

    public void ShowSection(JournalSection section)
    {
        currentSection = section;
        currentPage = 0; // Reset to first page when changing sections

        // Load the appropriate content
        switch (section)
        {
            case JournalSection.Objectives:
                LoadObjectivesContent();
                break;
            case JournalSection.Notes:
                LoadNotesContent();
                break;
            case JournalSection.Photos:
                LoadPhotosContent();
                break;
        }

        UpdateButtonStates();
    }

    private void ChangePage(int direction)
    {
        int newPage = currentPage + direction;

        // Clamp to valid page range
        if (newPage < 0 || newPage >= totalPages)
            return;

        currentPage = newPage;

        // Reload current section with new page
        switch (currentSection)
        {
            case JournalSection.Objectives:
                LoadObjectivesContent();
                break;
            case JournalSection.Notes:
                LoadNotesContent();
                break;
            case JournalSection.Photos:
                LoadPhotosContent();
                break;
        }
    }

    private void UpdatePageNavigation(int totalEntries)
    {
        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalEntries / entriesPerPage));

        // Clamp current page
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        bool showNavigation = totalPages > 1;

        // Page number
        if (pageNumberText != null)
        {
            pageNumberText.gameObject.SetActive(showNavigation);
            if (showNavigation)
                pageNumberText.text = $"{currentPage + 1}/{totalPages}";
        }

        // Previous button
        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(showNavigation);
            previousPageButton.interactable = showNavigation && currentPage > 0;
        }

        // Next button
        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(showNavigation);
            nextPageButton.interactable = showNavigation && currentPage < totalPages - 1;
        }
    }

    private void ClearContent()
    {
        // Remove all children from content container
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateButtonStates()
    {
        // Visual feedback for active button
        if (objectivesButton != null)
            objectivesButton.interactable = currentSection != JournalSection.Objectives;
        if (notesButton != null)
            notesButton.interactable = currentSection != JournalSection.Notes;
        if (photosButton != null)
            photosButton.interactable = currentSection != JournalSection.Photos;
    }

    // Helper method to extract content text from prefab and move it under the prefab
    private void ExtractAndMoveContentText(GameObject entryPrefab)
    {
        // Find the content text object in the prefab by name
        Transform contentTextTransform = entryPrefab.transform.Find(contentTextName);

        if (contentTextTransform == null)
        {
            // Try to find it recursively if not a direct child
            contentTextTransform = FindChildRecursive(entryPrefab.transform, contentTextName);
        }

        if (contentTextTransform != null)
        {
            // Get the text content before moving
            string textContent = "";
            TMP_Text tmpText = contentTextTransform.GetComponent<TMP_Text>();
            Text regularText = contentTextTransform.GetComponent<Text>();

            if (tmpText != null)
            {
                textContent = tmpText.text;
            }
            else if (regularText != null)
            {
                textContent = regularText.text;
            }

            // Store original properties
            RectTransform originalRect = contentTextTransform.GetComponent<RectTransform>();

            // Unparent from prefab and reparent to content container
            contentTextTransform.SetParent(contentContainer);

            // Set sibling index to be right after the prefab
            int prefabIndex = entryPrefab.transform.GetSiblingIndex();
            contentTextTransform.SetSiblingIndex(prefabIndex + 1);

            // Reset local position/scale if needed
            contentTextTransform.localScale = Vector3.one;
        }
    }

    // Recursive helper to find child by name
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    #region Objectives Section
    private void LoadObjectivesContent()
    {
        ClearContent();

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning("ObjectiveManager instance not found!");

            UpdatePageNavigation(0);
            return;
        }

        // Combine active and completed objectives
        var allObjectives = new List<Objective>();
        allObjectives.AddRange(ObjectiveManager.Instance.GetActiveObjectives());
        allObjectives.AddRange(ObjectiveManager.Instance.GetCompletedObjectives());

        // Calculate pagination
        int totalEntries = allObjectives.Count;
        UpdatePageNavigation(totalEntries);

        // Get objectives for current page
        int startIndex = currentPage * entriesPerPage;
        int endIndex = Mathf.Min(startIndex + entriesPerPage, totalEntries);

        for (int i = startIndex; i < endIndex; i++)
        {
            CreateObjectiveEntry(allObjectives[i]);
        }
    }

    private void CreateObjectiveEntry(Objective objective)
    {
        if (objectiveEntryPrefab != null)
        {
            GameObject entry = Instantiate(objectiveEntryPrefab, contentContainer);
            ObjectiveEntryUI entryUI = entry.GetComponent<ObjectiveEntryUI>();

            if (entryUI != null)
            {
                entryUI.Setup(objective, this);

                // Subscribe to progress changes
                objective.OnProgressChanged += (obj, progress) =>
                {
                    if (currentSection == JournalSection.Objectives)
                    {
                        entryUI.UpdateProgress(progress);
                    }
                };
            }

            // Extract and move content text from prefab to content container
            ExtractAndMoveContentText(entry);
        }
    }
    #endregion

    #region Notes Section
    public void AddNote(string title, string content, string date = "")
    {
        // Use the NoteManager instead of local list
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.AddNote(title, content, date);
        }
        else
        {
            Debug.LogError("NoteManager.Instance is null!");
        }

        SaveJournalData();
    }

    public void DeleteNote(string id)
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.DeleteNote(id);
        }

        SaveJournalData();
    }

    private void LoadNotesContent()
    {
        ClearContent();

        if (NoteManager.Instance == null)
        {
            Debug.LogWarning("NoteManager instance not found!");
            UpdatePageNavigation(0);
            return;
        }

        List<Note> allNotes = NoteManager.Instance.GetAllNotes();
        int totalEntries = allNotes.Count;
        UpdatePageNavigation(totalEntries);

        // Get notes for current page
        int startIndex = currentPage * entriesPerPage;
        int endIndex = Mathf.Min(startIndex + entriesPerPage, totalEntries);

        for (int i = startIndex; i < endIndex; i++)
        {
            if (noteEntryPrefab != null)
            {
                GameObject entry = Instantiate(noteEntryPrefab, contentContainer);
                NoteEntryUI entryUI = entry.GetComponent<NoteEntryUI>();
                if (entryUI != null)
                {
                    // Pass the Note object instead of NoteEntry
                    entryUI.Setup(allNotes[i], this);
                }

                // Extract and move content text from prefab to content container
                ExtractAndMoveContentText(entry);
            }
        }
    }
    #endregion

    #region Photos Section
    public void AddPhoto(Sprite photoSprite, string caption)
    {
        PhotoEntry photo = new PhotoEntry
        {
            id = System.Guid.NewGuid().ToString(),
            sprite = photoSprite,
            caption = caption,
            dateCreated = System.DateTime.Now
        };

        photos.Add(photo);

        // Refresh if currently viewing photos
        if (currentSection == JournalSection.Photos)
        {
            LoadPhotosContent();
        }

        SaveJournalData();
    }

    public void DeletePhoto(string id)
    {
        photos.RemoveAll(p => p.id == id);
        LoadPhotosContent();
        SaveJournalData();
    }

    private void LoadPhotosContent()
    {
        ClearContent();

        int totalEntries = photos.Count;
        UpdatePageNavigation(totalEntries);

        // Get photos for current page
        int startIndex = currentPage * entriesPerPage;
        int endIndex = Mathf.Min(startIndex + entriesPerPage, totalEntries);

        for (int i = startIndex; i < endIndex; i++)
        {
            if (photoEntryPrefab != null)
            {
                GameObject entry = Instantiate(photoEntryPrefab, contentContainer);
                PhotoEntryUI entryUI = entry.GetComponent<PhotoEntryUI>();
                if (entryUI != null)
                {
                    entryUI.Setup(photos[i], this);
                }

                // Extract and move content text from prefab to content container
                ExtractAndMoveContentText(entry);
            }
        }
    }
    #endregion

    #region Save/Load
    private void SaveJournalData()
    {
        // Implement your save system here (PlayerPrefs, JSON, etc.)
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.SaveNotes();
        }
        Debug.Log("Journal data saved");
    }

    private void LoadJournalData()
    {
        // Implement your load system here
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.LoadNotes();
        }
    }
    #endregion
}

// Keep these enums/classes, but NoteEntryUI needs to be updated
public enum JournalSection
{
    Objectives,
    Notes,
    Photos
}

[System.Serializable]
public class PhotoEntry
{
    public string id;
    public Sprite sprite;
    public string caption;
    public System.DateTime dateCreated;
}

// Helper UI component for photo entries
public class PhotoEntryUI : MonoBehaviour
{
    public Image photoImage;
    public Text captionText;
    public Text dateText;
    public Button deleteButton;
    private PhotoEntry data;
    private JournalUI journal;

    public void Setup(PhotoEntry photo, JournalUI journalUI)
    {
        data = photo;
        journal = journalUI;

        if (photoImage) photoImage.sprite = photo.sprite;
        if (captionText) captionText.text = photo.caption;
        if (dateText) dateText.text = photo.dateCreated.ToString("MMM dd, yyyy");
        if (deleteButton) deleteButton.onClick.AddListener(OnDelete);
    }

    private void OnDelete()
    {
        journal.DeletePhoto(data.id);
    }
}