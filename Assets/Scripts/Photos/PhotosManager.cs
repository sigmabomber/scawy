using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;
using System;
using Doody.Framework.NoteSystem;
using Unity.VisualScripting;
using Doody.Framwork.PhotoSystem;

public class PhotosManager : EventListener
{
    public static PhotosManager instance;
    public static PhotosManager Instance => instance;

    public Dictionary<string, Photo> photos = new();

    public event Action<Photo> OnPhotoAdded;



    private void Start()
    {
        Events.Subscribe<AddPhotoEvent>(AddPhotoEvent, this);

    }


    public void AddPhotoEvent(AddPhotoEvent e)
    {

    }
}


[Serializable]

public class AddPhotoEvent
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Date { get; private set; }

    
}
