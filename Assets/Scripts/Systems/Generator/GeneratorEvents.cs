using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Doody.GameEvents.Generator
{
 
    
        public abstract class Generator
        {
            public float AmountToFillUp { get; private set; }
            public bool IsOn { get; private set; }
        }

        public class ActivateGenerator
        {
            public ActivateGenerator() { }
        }

        public class DeactivateGenerator
        {
            public DeactivateGenerator() { }
        }



    
}
    