
using Doody.AI.Events;
using Doody.GameEvents;
using System.Collections.Generic;
using UnityEngine;

public class AICoordinator : EventListener
{
    private static AICoordinator instance;
    public static AICoordinator Instance => instance;

    [Header("Settings")]
    [SerializeField] private bool enableCoordination = true;
    [SerializeField] private float coordinationRadius = 50f;
    [SerializeField] private bool showDebugStats = true;

    private Dictionary<GameObject, AIState> aiStates = new Dictionary<GameObject, AIState>();
    private Dictionary<GameObject, Vector3> lastKnownPositions = new Dictionary<GameObject, Vector3>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Listen<AIStateChangedEvent>(OnAIStateChanged);
        Listen<AlertRaisedEvent>(OnAlertRaised);
        Listen<TargetDetectedEvent>(OnTargetDetected);
        Listen<TargetLostEvent>(OnTargetLost);
    }

    void OnAIStateChanged(AIStateChangedEvent evt)
    {
        aiStates[evt.AI] = evt.NewState;
    }

    void OnAlertRaised(AlertRaisedEvent evt)
    {
        if (!enableCoordination) return;

        lastKnownPositions[evt.Source] = evt.Position;
        Debug.Log($"<color=orange>Coordinator: Managing {evt.Level} alert response</color>");
    }

    void OnTargetDetected(TargetDetectedEvent evt)
    {
        if (!enableCoordination) return;

        lastKnownPositions[evt.AI] = evt.Position;

        // Count how many nearby AI could respond
        int alertedCount = 0;
        foreach (var kvp in aiStates)
        {
            if (kvp.Key == evt.AI || kvp.Key == null) continue;

            float distance = Vector3.Distance(evt.AI.transform.position, kvp.Key.transform.position);
            if (distance <= coordinationRadius && kvp.Value == AIState.Patrolling)
            {
                alertedCount++;
            }
        }

        if (alertedCount > 0)
        {
            Debug.Log($"<color=yellow>Coordinator: {alertedCount} nearby AI can respond</color>");
        }
    }

    void OnTargetLost(TargetLostEvent evt)
    {
        lastKnownPositions[evt.AI] = evt.LastKnownPosition;
    }

    public int GetActiveAICount()
    {
        int count = 0;
        foreach (var ai in aiStates.Keys)
        {
            if (ai != null) count++;
        }
        return count;
    }

    public int GetAlertedAICount()
    {
        int count = 0;
        foreach (var kvp in aiStates)
        {
            if (kvp.Key != null && kvp.Value != AIState.Patrolling && kvp.Value != AIState.Returning)
            {
                count++;
            }
        }
        return count;
    }

    public int GetChasingAICount()
    {
        int count = 0;
        foreach (var kvp in aiStates)
        {
            if (kvp.Key != null && kvp.Value == AIState.Chasing)
            {
                count++;
            }
        }
        return count;
    }

    void OnGUI()
    {
        if (!showDebugStats) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        int totalAI = GetActiveAICount();
        int alertedAI = GetAlertedAICount();
        int chasingAI = GetChasingAICount();

        GUI.Box(new Rect(Screen.width - 220, 10, 200, 100), "", style);
        GUI.Label(new Rect(Screen.width - 210, 20, 180, 25), $"Total AI: {totalAI}", style);
        GUI.Label(new Rect(Screen.width - 210, 45, 180, 25), $"Alerted: {alertedAI}", style);
        GUI.Label(new Rect(Screen.width - 210, 70, 180, 25), $"Chasing: {chasingAI}", style);
    }
}