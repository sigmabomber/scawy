using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.GameEvents.Health
{

   
    public  class  AddHealthEvent
    {
        public int Amount { get; private set; }

        public AddHealthEvent (int amount)
        {
            Amount = amount;
        }
    }

    public class RemoveHealthEvent
    {
        public int Amount { get; private set; }

        public RemoveHealthEvent(int amount) 
        {
            Amount = amount;
        }

    }


    public class DeadEvent
    {
        public DeadEvent()
        {

        }

        
    }

   

}