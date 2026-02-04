using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.Framwork.PhotoSystem
{

    public class Photo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Date { get; private set; }


        public Photo (string title, string description, string id = null, string date = null)
        {

            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            Title = string.IsNullOrEmpty(title) ? "Missing Title" : title;
            Description = string.IsNullOrEmpty(description) ? "Missing Description" : description;
            Date = string.IsNullOrEmpty(date) ? "" : date;

        }


    }
}
