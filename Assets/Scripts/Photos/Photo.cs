using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Doody.Framework.PhotoSystem
{
    [System.Serializable]
    public class Photo
    {
        public string Id { get; private set; }
        public Sprite PhotoSprite { get; private set; }
        public string Description { get; private set; }
        public string Date { get; private set; }
        public DateTime DateCreated { get; private set; }

        public Photo(Sprite photoSprite, string description, string date = "", string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            PhotoSprite = photoSprite;
            Description = string.IsNullOrEmpty(description) ? "No Description" : description;
            Date = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("MMM dd, yyyy") : date;
            DateCreated = DateTime.Now;
        }

        public void UpdateSprite(Sprite newSprite)
        {
            if (newSprite != null)
            {
                PhotoSprite = newSprite;
            }
        }

        public void UpdateDescription(string newDescription)
        {
            if (!string.IsNullOrEmpty(newDescription))
            {
                Description = newDescription;
            }
        }
    }
}