using UnityEngine;
using Doody.GameEvents;

namespace Doody.AI.Events
{
    // Detection Events
    public struct TargetDetectedEvent
    {
        public GameObject AI;
        public GameObject Target;
        public Vector3 Position;
        public DetectionType Type;
    }

    public struct TargetLostEvent
    {
        public GameObject AI;
        public GameObject LastTarget;
        public Vector3 LastKnownPosition;
    }

    // Sound Events
    public struct SoundHeardEvent
    {
        public GameObject AI;
        public Vector3 SoundPosition;
        public float SoundIntensity;
    }

    // State Events
    public struct AIStateChangedEvent
    {
        public GameObject AI;
        public AIState PreviousState;
        public AIState NewState;
    }

    public struct InvestigationCompleteEvent
    {
        public GameObject AI;
        public Vector3 InvestigatedPosition;
        public bool FoundAnything;
    }

    // Alert Events
    public struct AlertRaisedEvent
    {
        public GameObject Source;
        public Vector3 Position;
        public AlertLevel Level;
    }

    public enum DetectionType
    {
        Visual,
        Sound,
        Alert
    }

    public enum AlertLevel
    {
        Low,
        Medium,
        High
    }

    public enum AIState
    {
        Patrolling,
        Investigating,
        Searching,
        Returning,
        Alerted,
        Chasing
    }
}