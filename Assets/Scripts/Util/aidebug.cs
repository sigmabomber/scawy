
using Doody.AI.Events;
using Doody.GameEvents;
using System.Collections.Generic;
using UnityEngine;

public class AIDebugUI : EventListener
{
    [SerializeField] private bool showEvents = true;
    [SerializeField] private int maxLogEntries = 10;
    [SerializeField] private bool showAIStats = true;

    private Queue<string> eventLog = new Queue<string>();
    private Dictionary<GameObject, AIState> aiStates = new Dictionary<GameObject, AIState>();

    void Start()
    {
        Listen<TargetDetectedEvent>(OnTargetDetected);
        Listen<TargetLostEvent>(OnTargetLost);
        Listen<AIStateChangedEvent>(OnStateChanged);
        Listen<AlertRaisedEvent>(OnAlertRaised);
        Listen<InvestigationCompleteEvent>(OnInvestigationComplete);
    }

    void OnTargetDetected(TargetDetectedEvent evt)
    {
        AddLog($"<color=red>[{evt.AI.name}] DETECTED {evt.Target.name} via {evt.Type}</color>");
    }

    void OnTargetLost(TargetLostEvent evt)
    {
        AddLog($"<color=yellow>[{evt.AI.name}] Lost target</color>");
    }

    void OnStateChanged(AIStateChangedEvent evt)
    {
        aiStates[evt.AI] = evt.NewState;

        Color stateColor = GetStateColor(evt.NewState);
        string colorTag = ColorUtility.ToHtmlStringRGB(stateColor);
        AddLog($"<color=#{colorTag}>[{evt.AI.name}] {evt.PreviousState} → {evt.NewState}</color>");
    }

    void OnAlertRaised(AlertRaisedEvent evt)
    {
        AddLog($"<color=orange>[{evt.Source.name}] Raised {evt.Level} alert!</color>");
    }

    void OnInvestigationComplete(InvestigationCompleteEvent evt)
    {
        string result = evt.FoundAnything ? "Found something" : "Found nothing";
        AddLog($"<color=cyan>[{evt.AI.name}] Investigation: {result}</color>");
    }

    void AddLog(string message)
    {
        eventLog.Enqueue($"{Time.time:F1}s: {message}");
        if (eventLog.Count > maxLogEntries)
        {
            eventLog.Dequeue();
        }
    }

    Color GetStateColor(AIState state)
    {
        switch (state)
        {
            case AIState.Patrolling: return Color.green;
            case AIState.Investigating: return Color.yellow;
            case AIState.Searching: return new Color(1f, 0.5f, 0f);
            case AIState.Alerted: return new Color(1f, 0.3f, 0f);
            case AIState.Chasing: return Color.red;
            case AIState.Returning: return Color.cyan;
            default: return Color.white;
        }
    }

    void OnGUI()
    {
        if (!showEvents && !showAIStats) return;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.UpperLeft;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 12;
        textStyle.normal.textColor = Color.white;
        textStyle.richText = true;

        // AI Stats
        if (showAIStats)
        {
            GUI.Box(new Rect(10, 100, 250, 100 + (aiStates.Count * 20)), "AI Status", boxStyle);

            int index = 0;
            foreach (var kvp in aiStates)
            {
                if (kvp.Key == null) continue;

                Color stateColor = GetStateColor(kvp.Value);
                string colorHex = ColorUtility.ToHtmlStringRGB(stateColor);

                GUI.Label(new Rect(15, 125 + (index * 20), 240, 20),
                    $"{kvp.Key.name}: <color=#{colorHex}>{kvp.Value}</color>", textStyle);
                index++;
            }
        }

        // Event Log
        if (showEvents)
        {
            float y = Screen.height - 20 - (maxLogEntries * 20);
            GUI.Box(new Rect(10, y - 30, 600, (maxLogEntries * 20) + 40), "AI Event Log", boxStyle);

            int index = 0;
            foreach (string log in eventLog)
            {
                GUI.Label(new Rect(15, y + (index * 20), 590, 20), log, textStyle);
                index++;
            }
        }
    }
}