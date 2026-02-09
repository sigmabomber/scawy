using Doody.Framework.NoteSystem;
using Doody.Framework.PhotoSystem;
using Doody.GameEvents;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PhotosManager : EventListener
{
    private static PhotosManager instance;
    public static PhotosManager Instance => instance;

    private Dictionary<string, Photo> photos = new Dictionary<string, Photo>();

    public event Action<Photo> OnPhotoAdded;

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

        // Listen for photo-related events
        Events.Subscribe<AddPhotoEvent>(OnAddPhotoEvent);

        if (debugLogs)
            Debug.Log("PhotosManager initialized and listening for photo events");
    }

    public void AddPhoto(Sprite photoSprite, string description, string date = "")
    {
        string id = Guid.NewGuid().ToString();
        AddPhoto(id, photoSprite, description, date);
    }

    public void AddPhoto(string id, Sprite photoSprite, string description, string date = "")
    {
        if (photos.ContainsKey(id))
        {
            Debug.LogWarning($"Photo with ID '{id}' already exists!");
            return;
        }

        Photo photo = new Photo(photoSprite, description, date, id);
        photos[id] = photo;

        OnPhotoAdded?.Invoke(photo);

        if (debugLogs)
            Debug.Log($"Photo added: {description} (ID: {id})");
    }

    public Photo GetPhoto(string photoId)
    {
        photos.TryGetValue(photoId, out Photo photo);
        return photo;
    }

    public List<Photo> GetAllPhotos()
    {
        return new List<Photo>(photos.Values);
    }

    public List<Photo> GetPhotosSortedByDate(bool descending = true)
    {
        List<Photo> photoList = GetAllPhotos();

        if (descending)
        {
            photoList.Sort((a, b) => string.Compare(b.Date, a.Date, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            photoList.Sort((a, b) => string.Compare(a.Date, b.Date, StringComparison.OrdinalIgnoreCase));
        }

        return photoList;
    }

    public int GetPhotoCount()
    {
        return photos.Count;
    }

    public bool HasPhoto(string photoId)
    {
        return photos.ContainsKey(photoId);
    }

    #region Event Handlers

    private void OnAddPhotoEvent(AddPhotoEvent e)
    {
        if (string.IsNullOrEmpty(e.Id))
        {
            AddPhoto(e.PhotoSprite, e.Description, e.Date);
        }
        else
        {
            AddPhoto(e.Id, e.PhotoSprite, e.Description, e.Date);
        }
    }

    #endregion

    // Save/Load functionality
    public void SavePhotos()
    {
        // Implement your save system here
        Debug.Log($"Saving {photos.Count} photos");
    }

    public void LoadPhotos()
    {
        // Implement your load system here
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (instance == this)
            instance = null;
    }
}

// Event Definition for the photo system
[System.Serializable]
public class AddPhotoEvent
{
    public string Id { get; set; } = ""; 
    public Sprite PhotoSprite { get; set; }
    public string Description { get; set; }
    public string Date { get; set; } = ""; 

    public AddPhotoEvent(Sprite photoSprite, string description, string date = "", string id = "")
    {
        PhotoSprite = photoSprite;
        Description = description;
        Date = date;
        Id = id;
    }
}