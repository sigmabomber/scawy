using Doody.GameEvents;
using Doody.Framework.Player.Effects;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EffectsManager : EventListener
{
    [Header("Effect Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private float updateInterval = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    // Active effects storage
    private Dictionary<string, ActiveEffect> activeEffects = new Dictionary<string, ActiveEffect>();
    private float nextUpdateTime;

    // Effect modifiers (accumulated from all active effects)
    private EffectModifiers currentModifiers = new EffectModifiers();

    // Base speeds (cached from player controller)
    private float baseWalkSpeed;
    private float baseMouseSensitivity;

    void Awake()
    {
        // Auto-find PlayerController if not assigned
        if (playerController == null)
            playerController = PlayerController.Instance ?? FindObjectOfType<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("[EffectsManager] PlayerController not found! Effects won't be applied.");
        }
    }

    void Start()
    {
        // Subscribe to effect events
        Listen<AddEffect>(OnAddEffect);
        Listen<RemoveEffect>(OnRemoveEffect);
        Listen<RemoveAllEffects>(OnRemoveAllEffects);
        Listen<GiveAllEffects>(OnGiveAllEffects);

        // Cache base speeds from player controller
        if (playerController != null)
        {
            baseWalkSpeed = playerController.walkSpeed;
            baseMouseSensitivity = playerController.mouseSensitivity;
        }

        nextUpdateTime = Time.time + updateInterval;

        if (debugMode)
            Debug.Log("[EffectsManager] Initialized and listening for effect events");
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateEffects();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    #region Event Handlers

    private void OnAddEffect(AddEffect effectEvent)
    {
        // Use reflection to get protected properties
        var typeField = typeof(EffectEvent).GetProperty("Type",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var durationField = typeof(EffectEvent).GetProperty("Duration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var strengthField = typeof(EffectEvent).GetProperty("Strength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var idField = typeof(AddEffect).GetProperty("ID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var type = (EffectEvent.EffectType)typeField.GetValue(effectEvent);
        var duration = (float)durationField.GetValue(effectEvent);
        var strength = (float)strengthField.GetValue(effectEvent);
        var id = (string)idField.GetValue(effectEvent);

        AddEffectInternal(type, duration, strength, id);
    }

    private void OnRemoveEffect(RemoveEffect effectEvent)
    {
        var idField = typeof(RemoveEffect).GetProperty("ID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var id = (string)idField.GetValue(effectEvent);

        RemoveEffectInternal(id);
    }

    private void OnRemoveAllEffects(RemoveAllEffects effectEvent)
    {
        RemoveAllEffectsInternal();
    }

    private void OnGiveAllEffects(GiveAllEffects effectEvent)
    {
        GiveAllEffectsInternal();
    }

    #endregion

    #region Internal Effect Management

    private void AddEffectInternal(EffectEvent.EffectType type, float duration, float strength, string id)
    {
        // Check if effect already exists
        if (activeEffects.ContainsKey(id))
        {
            if (debugMode)
                Debug.LogWarning($"[EffectsManager] Effect {id} already exists. Refreshing duration.");

            // Refresh the existing effect
            activeEffects[id].remainingDuration = duration;
            activeEffects[id].strength = strength;
            return;
        }

        // Create new active effect
        ActiveEffect newEffect = new ActiveEffect
        {
            id = id,
            type = type,
            remainingDuration = duration,
            strength = strength,
            startTime = Time.time
        };

        activeEffects[id] = newEffect;
        RecalculateModifiers();
        ApplyModifiersToPlayer();

        if (debugMode)
            Debug.Log($"[EffectsManager] Added {type} effect (ID: {id}, Duration: {duration}s, Strength: {strength})");

        // Start visual effect if you have one
        StartCoroutine(PlayEffectVisual(newEffect));
    }

    private void RemoveEffectInternal(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[EffectsManager] Attempted to remove effect with null/empty ID");
            return;
        }

        if (activeEffects.ContainsKey(id))
        {
            var effect = activeEffects[id];
            activeEffects.Remove(id);
            RecalculateModifiers();
            ApplyModifiersToPlayer();

            if (debugMode)
                Debug.Log($"[EffectsManager] Removed {effect.type} effect (ID: {id})");
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[EffectsManager] Effect {id} not found for removal");
        }
    }

    private void RemoveAllEffectsInternal()
    {
        int count = activeEffects.Count;
        activeEffects.Clear();
        RecalculateModifiers();
        ApplyModifiersToPlayer();

        if (debugMode)
            Debug.Log($"[EffectsManager] Removed all effects ({count} total)");
    }

    private void GiveAllEffectsInternal()
    {
        if (debugMode)
            Debug.Log("[EffectsManager] Giving all effects (Debug mode)");

        // Add one of each effect type for testing
        AddEffectInternal(EffectEvent.EffectType.Speed, 10f, 1.5f, "debug_speed");
        AddEffectInternal(EffectEvent.EffectType.Slow, 10f, 0.5f, "debug_slow");
        AddEffectInternal(EffectEvent.EffectType.Stamina, 10f, 2f, "debug_stamina");
        AddEffectInternal(EffectEvent.EffectType.Health, 10f, 1.2f, "debug_health");
    }

    #endregion

    #region Effect Updates

    private void UpdateEffects()
    {
        if (activeEffects.Count == 0)
            return;

        List<string> expiredEffects = new List<string>();

        // Update all active effects
        foreach (var kvp in activeEffects)
        {
            var effect = kvp.Value;
            effect.remainingDuration -= updateInterval;

            if (effect.remainingDuration <= 0)
            {
                expiredEffects.Add(kvp.Key);
            }
        }

        // Remove expired effects
        if (expiredEffects.Count > 0)
        {
            foreach (var id in expiredEffects)
            {
                var effect = activeEffects[id];
                activeEffects.Remove(id);

                if (debugMode)
                    Debug.Log($"[EffectsManager] Effect {effect.type} (ID: {id}) expired");
            }

            RecalculateModifiers();
            ApplyModifiersToPlayer();
        }
    }

    private void RecalculateModifiers()
    {
        // Reset modifiers
        currentModifiers = new EffectModifiers();

        // Accumulate all active effects
        foreach (var effect in activeEffects.Values)
        {
            switch (effect.type)
            {
                case EffectEvent.EffectType.Speed:
                    currentModifiers.speedMultiplier *= effect.strength;
                    currentModifiers.sensitivityMultiplier *= Mathf.Lerp(1f, effect.strength, 0.3f);
                    break;

                case EffectEvent.EffectType.Slow:
                    currentModifiers.speedMultiplier *= effect.strength;
                    currentModifiers.sensitivityMultiplier *= Mathf.Lerp(1f, effect.strength, 0.5f);
                    break;

                case EffectEvent.EffectType.Stamina:
                    currentModifiers.staminaMultiplier *= effect.strength;
                    // Stamina affects sprint availability
                    if (effect.strength > 1.5f)
                        currentModifiers.canAlwaysSprint = true;
                    break;

                case EffectEvent.EffectType.Health:
                    currentModifiers.healthMultiplier *= effect.strength;
                    break;
            }
        }

        if (debugMode)
        {
            Debug.Log($"[EffectsManager] Modifiers - Speed: {currentModifiers.speedMultiplier:F2}x, " +
                     $"Sensitivity: {currentModifiers.sensitivityMultiplier:F2}x, " +
                     $"Stamina: {currentModifiers.staminaMultiplier:F2}x, " +
                     $"Health: {currentModifiers.healthMultiplier:F2}x");
        }
    }

    private void ApplyModifiersToPlayer()
    {
        if (playerController == null)
            return;

        // Apply speed modifier
        playerController.walkSpeed = baseWalkSpeed * currentModifiers.speedMultiplier;

        // Apply sensitivity modifier
        playerController.mouseSensitivity = baseMouseSensitivity * currentModifiers.sensitivityMultiplier;

        // Apply stamina effects (super stamina disables sprint limit)
        if (currentModifiers.canAlwaysSprint)
            playerController.canSprint = true;
    }

    private IEnumerator PlayEffectVisual(ActiveEffect effect)
    {
        // Placeholder for visual effects
        // You can add particle systems, UI indicators, screen effects, etc.

        // Example: Flash screen color based on effect type
        // Color effectColor = GetEffectColor(effect.type);
        // StartCoroutine(FlashScreen(effectColor));

        yield return null;
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Get the current speed multiplier from all active effects
    /// </summary>
    public float GetSpeedMultiplier() => currentModifiers.speedMultiplier;

    /// <summary>
    /// Get the current sensitivity multiplier from all active effects
    /// </summary>
    public float GetSensitivityMultiplier() => currentModifiers.sensitivityMultiplier;

    /// <summary>
    /// Get the current stamina multiplier from all active effects
    /// </summary>
    public float GetStaminaMultiplier() => currentModifiers.staminaMultiplier;

    /// <summary>
    /// Get the current health multiplier from all active effects
    /// </summary>
    public float GetHealthMultiplier() => currentModifiers.healthMultiplier;

    /// <summary>
    /// Check if a specific effect is currently active
    /// </summary>
    public bool HasEffect(string id) => activeEffects.ContainsKey(id);

    /// <summary>
    /// Check if any effect of a specific type is active
    /// </summary>
    public bool HasEffectType(EffectEvent.EffectType type)
    {
        return activeEffects.Values.Any(e => e.type == type);
    }

    /// <summary>
    /// Get remaining duration of a specific effect
    /// </summary>
    public float GetEffectDuration(string id)
    {
        return activeEffects.TryGetValue(id, out var effect) ? effect.remainingDuration : 0f;
    }

    /// <summary>
    /// Get count of active effects
    /// </summary>
    public int GetActiveEffectCount() => activeEffects.Count;

    /// <summary>
    /// Get all active effect IDs
    /// </summary>
    public List<string> GetActiveEffectIds() => activeEffects.Keys.ToList();

    /// <summary>
    /// Get dictionary of active effects for UI display
    /// </summary>
    public Dictionary<string, (EffectEvent.EffectType type, float duration, float strength)> GetActiveEffectsInfo()
    {
        var info = new Dictionary<string, (EffectEvent.EffectType, float, float)>();
        foreach (var kvp in activeEffects)
        {
            info[kvp.Key] = (kvp.Value.type, kvp.Value.remainingDuration, kvp.Value.strength);
        }
        return info;
    }

    #endregion

    #region Debug

    void OnGUI()
    {
        if (!debugMode || activeEffects.Count == 0)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 500));

        // Background box
        GUI.Box(new Rect(0, 0, 350, 500), "");

        GUILayout.Label($"<b><size=16>Active Effects: {activeEffects.Count}</size></b>");
        GUILayout.Space(5);

        foreach (var kvp in activeEffects)
        {
            var effect = kvp.Value;
            Color color = GetEffectDebugColor(effect.type);
            GUI.color = color;
            GUILayout.Label($"<b>{effect.type}</b>: {effect.remainingDuration:F1}s ({effect.strength:F2}x)");
            GUI.color = Color.white;
        }

        GUILayout.Space(10);
        GUILayout.Label("<b><size=14>Current Modifiers:</size></b>");
        GUILayout.Label($"Speed: {currentModifiers.speedMultiplier:F2}x (Base: {baseWalkSpeed:F1} → Current: {playerController?.walkSpeed:F1})");
        GUILayout.Label($"Sensitivity: {currentModifiers.sensitivityMultiplier:F2}x");
        GUILayout.Label($"Stamina: {currentModifiers.staminaMultiplier:F2}x {(currentModifiers.canAlwaysSprint ? "(Unlimited Sprint)" : "")}");
        GUILayout.Label($"Health: {currentModifiers.healthMultiplier:F2}x");

        GUILayout.EndArea();
    }

    private Color GetEffectDebugColor(EffectEvent.EffectType type)
    {
        switch (type)
        {
            case EffectEvent.EffectType.Speed: return Color.cyan;
            case EffectEvent.EffectType.Slow: return new Color(1f, 0.5f, 0f); // Orange
            case EffectEvent.EffectType.Stamina: return Color.green;
            case EffectEvent.EffectType.Health: return Color.red;
            default: return Color.white;
        }
    }

    #endregion

    #region Helper Classes

    private class ActiveEffect
    {
        public string id;
        public EffectEvent.EffectType type;
        public float remainingDuration;
        public float strength;
        public float startTime;
    }

    private class EffectModifiers
    {
        public float speedMultiplier = 1f;
        public float sensitivityMultiplier = 1f;
        public float staminaMultiplier = 1f;
        public float healthMultiplier = 1f;
        public bool canAlwaysSprint = false;
    }

    #endregion
}