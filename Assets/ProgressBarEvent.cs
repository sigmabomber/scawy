using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.Framework.Progressbar
{


    /// <summary>
    /// Progress bar framework for things that has a duration to complete or cooldown to be able to use again
    /// Can be subscribed to Events
    /// Documentation on Events Framework <https://github.com/sigmabomber/Unity-Event-Framework/tree/main>
    /// </summary>


    // Base class
    public abstract class ProgressBarEvent
    {

        public float Duration { get; set; }

        
    }

    // Start progress bar 
    public class StartProgressBar : ProgressBarEvent
    {
        public GameObject ItemObject { get; set; }

        public string ProgressName { get; set; }
        public StartProgressBar( float duration, GameObject itemObject, string progressName)
        {

            Duration = duration;
            ItemObject = itemObject;
            ProgressName = progressName;
        }
    }

    // Progress bar has completed

    public class ProgressbarCompleted : ProgressBarEvent
    {
        public GameObject ItemObject { get; set; }
        public ProgressbarCompleted(GameObject itemObject)
        {
            ItemObject = itemObject;
        }
    }

    // Progress bar has been interuppted
    public class ProgressbarInteruppted : ProgressBarEvent
    {
        public ProgressbarInteruppted()
        {

        }
    }
}