using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Doody.Framework.ObjectiveSystem;
using Doody.GameEvents;
using Doody.Framework.UI;

public class JournalUI : MonoBehaviour
{
    [Header("Section Buttons")]
    public Button objectivesButton;
    public Button notesButton;
    public Button photosButton;

    [Header("Content Area")]
    public Transform contentContainer; 

    [Header("Prefabs")]
    public GameObject objectiveEntryPrefab;
    public GameObject noteEntryPrefab;
    public GameObject photoEntryPrefab;

    private GameObject objectiveOutline;
    private GameObject noteOutline;
    private GameObject photoOutline;

    private JournalSection currentSection = JournalSection.Objectives;

    private List<NoteEntry> notes = new List<NoteEntry>();
    private List<PhotoEntry> photos = new List<PhotoEntry>();

    public GameObject journal;
    [SerializeField] private KeyCode journalKeycode = KeyCode.J;

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

        // Subscribe to objective events
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveAdded += OnObjectiveAdded;
            ObjectiveManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
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

    public void ShowSection(JournalSection section)
    {
        currentSection = section;

        // Clear the content container
        ClearContent();

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

    #region Objectives Section
    private void LoadObjectivesContent()
    {
        ClearContent();

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning("ObjectiveManager instance not found!");
            return;
        }

        // Load active objectives first
        var activeObjectives = ObjectiveManager.Instance.GetActiveObjectives();
        foreach (var objective in activeObjectives)
        {
            CreateObjectiveEntry(objective);
        }

        // Load completed objectives
        var completedObjectives = ObjectiveManager.Instance.GetCompletedObjectives();
        foreach (var objective in completedObjectives)
        {
            CreateObjectiveEntry(objective);
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
        }
    }
    #endregion

    #region Notes Section
    public void AddNote(string title, string content)
    {
        NoteEntry note = new NoteEntry
        {
            id = System.Guid.NewGuid().ToString(),
            title = title,
            content = content,
            dateCreated = System.DateTime.Now
        };

        notes.Add(note);

        // Refresh if currently viewing notes
        if (currentSection == JournalSection.Notes)
        {
            LoadNotesContent();
        }

        SaveJournalData();
    }

    public void DeleteNote(string id)
    {
        notes.RemoveAll(n => n.id == id);
        LoadNotesContent();
        SaveJournalData();
    }

    private void LoadNotesContent()
    {
        ClearContent();

        foreach (var note in notes)
        {
            if (noteEntryPrefab != null)
            {
                GameObject entry = Instantiate(noteEntryPrefab, contentContainer);
                NoteEntryUI entryUI = entry.GetComponent<NoteEntryUI>();
                if (entryUI != null)
                {
                    entryUI.Setup(note, this);
                }
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

        foreach (var photo in photos)
        {
            if (photoEntryPrefab != null)
            {
                GameObject entry = Instantiate(photoEntryPrefab, contentContainer);
                PhotoEntryUI entryUI = entry.GetComponent<PhotoEntryUI>();
                if (entryUI != null)
                {
                    entryUI.Setup(photo, this);
                }
            }
        }
    }
    #endregion

    #region Save/Load
    private void SaveJournalData()
    {
        // Implement your save system here (PlayerPrefs, JSON, etc.)
        Debug.Log("Journal data saved");
    }

    private void LoadJournalData()
    {
        // Implement your load system here
        Debug.Log("Journal data loaded");
    }
    #endregion
}

public enum JournalSection
{
    Objectives,
    Notes,
    Photos
}

[System.Serializable]
public class NoteEntry
{
    public string id;
    public string title;
    public string content;
    public System.DateTime dateCreated;
}

[System.Serializable]
public class PhotoEntry
{
    public string id;
    public Sprite sprite;
    public string caption;
    public System.DateTime dateCreated;
}


// Helper UI component for note entries
public class NoteEntryUI : MonoBehaviour
{
    public Text titleText;
    public Text contentText;
    public Text dateText;
    public Button deleteButton;
    private NoteEntry data;
    private JournalUI journal;

    public void Setup(NoteEntry note, JournalUI journalUI)
    {
        data = note;
        journal = journalUI;

        if (titleText) titleText.text = note.title;
        if (contentText) contentText.text = note.content;
        if (dateText) dateText.text = note.dateCreated.ToString("MMM dd, yyyy");
        if (deleteButton) deleteButton.onClick.AddListener(OnDelete);
    }

    private void OnDelete()
    {
        journal.DeleteNote(data.id);
    }
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