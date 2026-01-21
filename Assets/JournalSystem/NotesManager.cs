using Doody.Framework.NoteSystem;
using Doody.GameEvents;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NoteManager : EventListener
{
    private static NoteManager instance;
    public static NoteManager Instance => instance;

    private Dictionary<string, Note> notes = new Dictionary<string, Note>();

    public event Action<Note> OnNoteAdded;
    public event Action<Note> OnNoteUpdated;
    public event Action<string> OnNoteDeleted;

    [Header("Settings")]
    [SerializeField] private bool debugLogs = true;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Listen for note-related events
        Events.Subscribe<AddNoteEvent>(OnAddNoteEvent);
        Events.Subscribe<UpdateNoteEvent>(OnUpdateNoteEvent);
        Events.Subscribe<DeleteNoteEvent>(OnDeleteNoteEvent);

        if (debugLogs)
            Debug.Log("NoteManager initialized and listening for note events");
    }

    public void AddNote(string title, string content, string date = "")
    {
        string id = Guid.NewGuid().ToString();
        AddNote(id, title, content, date);
    }

    public void AddNote(string id, string title, string content, string date = "")
    {
        if (notes.ContainsKey(id))
        {
            Debug.LogWarning($"Note with ID '{id}' already exists!");
            return;
        }

        Note note = new Note(title, content, date, id);
        notes[id] = note;

        // Subscribe to note updates
        note.OnNoteUpdated += HandleNoteUpdated;

        OnNoteAdded?.Invoke(note);

        if (debugLogs)
            Debug.Log($"Note added: {title} (ID: {id})");
    }

    public void UpdateNote(string noteId, string newTitle, string newContent, string newDate = null)
    {
        if (notes.TryGetValue(noteId, out Note note))
        {
            note.Update(newTitle, newContent, newDate);
        }
        else
        {
            Debug.LogWarning($"Note with ID '{noteId}' not found for update");
        }
    }

    public bool DeleteNote(string noteId)
    {
        if (notes.TryGetValue(noteId, out Note note))
        {
            notes.Remove(noteId);
            note.OnNoteUpdated -= HandleNoteUpdated;
            OnNoteDeleted?.Invoke(noteId);

            if (debugLogs)
                Debug.Log($"Note deleted: {note.Title} (ID: {noteId})");

            return true;
        }

        if (debugLogs)
            Debug.LogWarning($"Note with ID '{noteId}' not found for deletion");

        return false;
    }

    public Note GetNote(string noteId)
    {
        notes.TryGetValue(noteId, out Note note);
        return note;
    }

    public List<Note> GetAllNotes()
    {
        return new List<Note>(notes.Values);
    }

    public List<Note> GetNotesSortedByDate(bool descending = true)
    {
        List<Note> noteList = GetAllNotes();

        // Simple alphabetical sort for date strings
        if (descending)
        {
            noteList.Sort((a, b) => string.Compare(b.Date, a.Date, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            noteList.Sort((a, b) => string.Compare(a.Date, b.Date, StringComparison.OrdinalIgnoreCase));
        }

        return noteList;
    }

    public List<Note> SearchNotes(string searchText)
    {
        List<Note> results = new List<Note>();

        if (string.IsNullOrWhiteSpace(searchText))
            return results;

        string lowerSearch = searchText.ToLower();

        foreach (var note in notes.Values)
        {
            if (note.Title.ToLower().Contains(lowerSearch) ||
                note.Content.ToLower().Contains(lowerSearch))
            {
                results.Add(note);
            }
        }

        return results;
    }

    public void ClearAllNotes()
    {
        foreach (var note in notes.Values)
        {
            note.OnNoteUpdated -= HandleNoteUpdated;
        }
        notes.Clear();

        if (debugLogs)
            Debug.Log("All notes cleared");
    }

    public int GetNoteCount()
    {
        return notes.Count;
    }

    public bool HasNote(string noteId)
    {
        return notes.ContainsKey(noteId);
    }

    private void HandleNoteUpdated(Note note)
    {
        OnNoteUpdated?.Invoke(note);

        if (debugLogs)
            Debug.Log($"Note updated: {note.Title} (ID: {note.Id})");
    }

    #region Event Handlers

    private void OnAddNoteEvent(AddNoteEvent e)
    {
        if (string.IsNullOrEmpty(e.Id))
        {
            AddNote(e.Title, e.Content, e.Date);
        }
        else
        {
            AddNote(e.Id, e.Title, e.Content, e.Date);
        }
    }

    private void OnUpdateNoteEvent(UpdateNoteEvent e)
    {
        UpdateNote(e.NoteId, e.NewTitle, e.NewContent, e.NewDate);
    }

    private void OnDeleteNoteEvent(DeleteNoteEvent e)
    {
        DeleteNote(e.NoteId);
    }

    

    #endregion

    // Save/Load functionality
    public void SaveNotes()
    {
        // Implement your save system here
        // Example: Save to PlayerPrefs, JSON file, etc.
        Debug.Log($"Saving {notes.Count} notes");
    }

    public void LoadNotes()
    {
       
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up event subscriptions from notes
        foreach (var note in notes.Values)
        {
            note.OnNoteUpdated -= HandleNoteUpdated;
        }

        if (instance == this)
            instance = null;
    }
}

// Event Definitions for the note system
[System.Serializable]
public class AddNoteEvent
{
    public string Id = ""; // Optional - will generate if empty
    public string Title;
    public string Content;
    public string Date = ""; // Optional lore date

    public AddNoteEvent(string title, string content, string date = "", string id = "")
    {
        Title = title;
        Content = content;
        Date = date;
        Id = id;
    }
}

[System.Serializable]
public class UpdateNoteEvent
{
    public string NoteId;
    public string NewTitle;
    public string NewContent;
    public string NewDate = null; // null means keep existing

    public UpdateNoteEvent(string noteId, string newTitle, string newContent, string newDate = null)
    {
        NoteId = noteId;
        NewTitle = newTitle;
        NewContent = newContent;
        NewDate = newDate;
    }
}

[System.Serializable]
public class DeleteNoteEvent
{
    public string NoteId;

    public DeleteNoteEvent(string noteId)
    {
        NoteId = noteId;
    }
}

