using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Doody.Framework.Player.Effects
{

    /// <summary>
    /// Player effect framework for status effects such as a speed up, slow, etc
    /// Uses Events framework
    /// Documentation on Events Framework <https://github.com/sigmabomber/Unity-Event-Framework/tree/main>
    /// </summary>
    public abstract class EffectEvent
    {
        protected float Duration { get; set; }

        protected EffectType Type { get; set; }

        public enum EffectType { Slow, Speed, Stamina, Health }

        protected float Strength { get; set; }
    }

    // Add a specific effect to the player
    public class AddEffect : EffectEvent
    {
        protected string ID { get; set; }


        public AddEffect(EffectType type, float duration, float strength, string id = null)
        {
            Type = type;
            Duration = duration;
            Strength = strength;
            ID = id ?? System.Guid.NewGuid().ToString();
        }
    }
    // Remove a specific effect from a player
    public class RemoveEffect 
    {
        protected string ID { get; set; }
        public RemoveEffect(string iD = null)
        {
            ID = iD;
        }


    }
    // Remove all effects from a player
    public class RemoveAllEffects 
    {
        public RemoveAllEffects() { }
        
    }
    // Give all effects to a player (Debug)
    public class GiveAllEffects 
    {
        public GiveAllEffects() { }
    }
}