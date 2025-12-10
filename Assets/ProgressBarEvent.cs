using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.Framework.Progressbar
{


    /// <summary>
    /// Progress bar framework for things that has a duration to complete or cooldown to be able to use again
    /// Can be subscribed to
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


        public StartProgressBar( float duration, GameObject itemObject)
        {

            Duration = duration;
            ItemObject = itemObject;

        }
    }

    // Progress bar has completed

    public class ProgressbarCompleted : ProgressBarEvent
    {
        public ProgressbarCompleted()
        {
           
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