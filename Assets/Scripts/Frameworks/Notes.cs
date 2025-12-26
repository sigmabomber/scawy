// Core note system
using System;
using System.Collections.Generic;

namespace Doody.Framework.NoteSystem
{
    public class Note
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public string Date { get; private set; } // Just a string for lore/display

        public event Action<Note> OnNoteUpdated;

        public Note(string title, string content, string date = "", string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            Title = title;
            Content = content;
            Date = date; // Just store as provided string
        }

        public void Update(string newTitle, string newContent, string newDate = null)
        {
            if (!string.IsNullOrEmpty(newTitle))
            {
                Title = newTitle;
            }

            if (!string.IsNullOrEmpty(newContent))
            {
                Content = newContent;
            }

            if (!string.IsNullOrEmpty(newDate))
            {
                Date = newDate;
            }

            OnNoteUpdated?.Invoke(this);
        }
    }

    
    // For simpler usage, optional
    public class LoreNote : Note
    {
        public LoreNote(string title, string content, string date, string id = null)
            : base(title, content, date, id)
        {
        }
    }
}