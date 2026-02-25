using System;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;

/// <summary>
/// Flexible progress tracker that can store any type of data dynamically.
/// No hardcoded variables - add whatever you need from any script!
/// </summary>
public class GameProgressTracker : MonoBehaviour
{
    public static GameProgressTracker Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool logAllChanges = false;

    // All data stored in dictionaries for maximum flexibility
    private Dictionary<string, bool> boolData = new Dictionary<string, bool>();
    private Dictionary<string, int> intData = new Dictionary<string, int>();
    private Dictionary<string, float> floatData = new Dictionary<string, float>();
    private Dictionary<string, string> stringData = new Dictionary<string, string>();
    private Dictionary<string, List<string>> listData = new Dictionary<string, List<string>>();

    private bool isInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeEvents();
        InitializeDefaults();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Events.Unsubscribe<SaveDataRequestEvent>(OnSaveRequested);
            Events.Unsubscribe<LoadDataEvent>(OnLoadRequested);
        }
    }

    private void InitializeEvents()
    {
        try
        {
            Events.Subscribe<SaveDataRequestEvent>(OnSaveRequested, this);
            Events.Subscribe<LoadDataEvent>(OnLoadRequested, this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Failed to subscribe to events: {e.Message}");
        }
    }

    private void InitializeDefaults()
    {
        isInitialized = true;

        if (debugMode)
        {
            Debug.Log("[GameProgress] Initialized - ready to track any data!");
        }
    }

    // ========== BOOLEAN VALUES ==========

    /// <summary>
    /// Set a boolean value (e.g., hasSeenIntro, tutorialComplete, bossDefeated)
    /// </summary>
    public void SetBool(string key, bool value)
    {
        bool changed = !boolData.ContainsKey(key) || boolData[key] != value;

        boolData[key] = value;

        if (debugMode && logAllChanges && changed)
        {
            Debug.Log($"[GameProgress] Bool '{key}' = {value}");
        }
    }

    /// <summary>
    /// Get a boolean value with optional default
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        return boolData.ContainsKey(key) ? boolData[key] : defaultValue;
    }

    /// <summary>
    /// Check if a boolean key exists
    /// </summary>
    public bool HasBool(string key)
    {
        return boolData.ContainsKey(key);
    }

    // ========== INTEGER VALUES ==========

    /// <summary>
    /// Set an integer value (e.g., currentLevel, enemiesKilled, gold)
    /// </summary>
    public void SetInt(string key, int value)
    {
        bool changed = !intData.ContainsKey(key) || intData[key] != value;

        intData[key] = value;

        if (debugMode && logAllChanges && changed)
        {
            Debug.Log($"[GameProgress] Int '{key}' = {value}");
        }
    }

    /// <summary>
    /// Get an integer value with optional default
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        return intData.ContainsKey(key) ? intData[key] : defaultValue;
    }

    /// <summary>
    /// Increment an integer value
    /// </summary>
    public void IncrementInt(string key, int amount = 1)
    {
        int current = GetInt(key, 0);
        SetInt(key, current + amount);
    }

    /// <summary>
    /// Decrement an integer value
    /// </summary>
    public void DecrementInt(string key, int amount = 1)
    {
        int current = GetInt(key, 0);
        SetInt(key, current - amount);
    }

    /// <summary>
    /// Check if an integer key exists
    /// </summary>
    public bool HasInt(string key)
    {
        return intData.ContainsKey(key);
    }

    // ========== FLOAT VALUES ==========

    /// <summary>
    /// Set a float value (e.g., musicVolume, difficulty, completionPercentage)
    /// </summary>
    public void SetFloat(string key, float value)
    {
        bool changed = !floatData.ContainsKey(key) || !Mathf.Approximately(floatData[key], value);

        floatData[key] = value;

        if (debugMode && logAllChanges && changed)
        {
            Debug.Log($"[GameProgress] Float '{key}' = {value}");
        }
    }

    /// <summary>
    /// Get a float value with optional default
    /// </summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        return floatData.ContainsKey(key) ? floatData[key] : defaultValue;
    }

    /// <summary>
    /// Increment a float value
    /// </summary>
    public void IncrementFloat(string key, float amount)
    {
        float current = GetFloat(key, 0f);
        SetFloat(key, current + amount);
    }

    /// <summary>
    /// Check if a float key exists
    /// </summary>
    public bool HasFloat(string key)
    {
        return floatData.ContainsKey(key);
    }

    // ========== STRING VALUES ==========

    /// <summary>
    /// Set a string value (e.g., playerName, lastScene, dialogueChoice)
    /// </summary>
    public void SetString(string key, string value)
    {
        bool changed = !stringData.ContainsKey(key) || stringData[key] != value;

        stringData[key] = value ?? string.Empty;

        if (debugMode && logAllChanges && changed)
        {
            Debug.Log($"[GameProgress] String '{key}' = {value}");
        }
    }

    /// <summary>
    /// Get a string value with optional default
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        return stringData.ContainsKey(key) ? stringData[key] : defaultValue;
    }

    /// <summary>
    /// Check if a string key exists
    /// </summary>
    public bool HasString(string key)
    {
        return stringData.ContainsKey(key);
    }

    // ========== LIST VALUES ==========

    /// <summary>
    /// Add an item to a list (e.g., defeatedBosses, visitedLocations, unlockedAchievements)
    /// </summary>
    public void AddToList(string key, string item)
    {
        if (!listData.ContainsKey(key))
        {
            listData[key] = new List<string>();
        }

        if (!listData[key].Contains(item))
        {
            listData[key].Add(item);

            if (debugMode && logAllChanges)
            {
                Debug.Log($"[GameProgress] Added '{item}' to list '{key}'");
            }
        }
    }

    /// <summary>
    /// Remove an item from a list
    /// </summary>
    public void RemoveFromList(string key, string item)
    {
        if (listData.ContainsKey(key) && listData[key].Contains(item))
        {
            listData[key].Remove(item);

            if (debugMode && logAllChanges)
            {
                Debug.Log($"[GameProgress] Removed '{item}' from list '{key}'");
            }
        }
    }

    /// <summary>
    /// Check if a list contains an item
    /// </summary>
    public bool ListContains(string key, string item)
    {
        return listData.ContainsKey(key) && listData[key].Contains(item);
    }

    /// <summary>
    /// Get the entire list (returns a copy)
    /// </summary>
    public List<string> GetList(string key)
    {
        if (listData.ContainsKey(key))
        {
            return new List<string>(listData[key]);
        }
        return new List<string>();
    }

    /// <summary>
    /// Get the count of items in a list
    /// </summary>
    public int GetListCount(string key)
    {
        return listData.ContainsKey(key) ? listData[key].Count : 0;
    }

    /// <summary>
    /// Clear a list
    /// </summary>
    public void ClearList(string key)
    {
        if (listData.ContainsKey(key))
        {
            listData[key].Clear();

            if (debugMode && logAllChanges)
            {
                Debug.Log($"[GameProgress] Cleared list '{key}'");
            }
        }
    }

    // ========== DELETE VALUES ==========

    /// <summary>
    /// Delete a boolean key
    /// </summary>
    public void DeleteBool(string key)
    {
        if (boolData.ContainsKey(key))
        {
            boolData.Remove(key);
            if (debugMode) Debug.Log($"[GameProgress] Deleted bool '{key}'");
        }
    }

    /// <summary>
    /// Delete an integer key
    /// </summary>
    public void DeleteInt(string key)
    {
        if (intData.ContainsKey(key))
        {
            intData.Remove(key);
            if (debugMode) Debug.Log($"[GameProgress] Deleted int '{key}'");
        }
    }

    /// <summary>
    /// Delete a float key
    /// </summary>
    public void DeleteFloat(string key)
    {
        if (floatData.ContainsKey(key))
        {
            floatData.Remove(key);
            if (debugMode) Debug.Log($"[GameProgress] Deleted float '{key}'");
        }
    }

    /// <summary>
    /// Delete a string key
    /// </summary>
    public void DeleteString(string key)
    {
        if (stringData.ContainsKey(key))
        {
            stringData.Remove(key);
            if (debugMode) Debug.Log($"[GameProgress] Deleted string '{key}'");
        }
    }

    /// <summary>
    /// Delete a list key
    /// </summary>
    public void DeleteList(string key)
    {
        if (listData.ContainsKey(key))
        {
            listData.Remove(key);
            if (debugMode) Debug.Log($"[GameProgress] Deleted list '{key}'");
        }
    }

    // ========== UTILITY ==========

    /// <summary>
    /// Reset all progress data
    /// </summary>
    public void ResetAllProgress()
    {
        boolData.Clear();
        intData.Clear();
        floatData.Clear();
        stringData.Clear();
        listData.Clear();

        if (debugMode)
        {
            Debug.Log("[GameProgress] All progress reset");
        }
    }

    /// <summary>
    /// Get total count of all stored values
    /// </summary>
    public int GetTotalValueCount()
    {
        return boolData.Count + intData.Count + floatData.Count + stringData.Count + listData.Count;
    }

    /// <summary>
    /// Log all current progress data
    /// </summary>
    public void DebugLogAllProgress()
    {
        Debug.Log("========== GAME PROGRESS TRACKER ==========");
        Debug.Log($"Total Values Stored: {GetTotalValueCount()}");

        Debug.Log($"\n--- BOOLEANS ({boolData.Count}) ---");
        foreach (var kvp in boolData)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }

        Debug.Log($"\n--- INTEGERS ({intData.Count}) ---");
        foreach (var kvp in intData)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }

        Debug.Log($"\n--- FLOATS ({floatData.Count}) ---");
        foreach (var kvp in floatData)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }

        Debug.Log($"\n--- STRINGS ({stringData.Count}) ---");
        foreach (var kvp in stringData)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }

        Debug.Log($"\n--- LISTS ({listData.Count}) ---");
        foreach (var kvp in listData)
        {
            Debug.Log($"  {kvp.Key} = [{string.Join(", ", kvp.Value)}]");
        }

        Debug.Log("============================================");
    }

    /// <summary>
    /// Get all keys of a specific type
    /// </summary>
    public List<string> GetAllKeys(string type = "all")
    {
        var keys = new List<string>();

        switch (type.ToLower())
        {
            case "bool":
                keys.AddRange(boolData.Keys);
                break;
            case "int":
                keys.AddRange(intData.Keys);
                break;
            case "float":
                keys.AddRange(floatData.Keys);
                break;
            case "string":
                keys.AddRange(stringData.Keys);
                break;
            case "list":
                keys.AddRange(listData.Keys);
                break;
            default:
                keys.AddRange(boolData.Keys);
                keys.AddRange(intData.Keys);
                keys.AddRange(floatData.Keys);
                keys.AddRange(stringData.Keys);
                keys.AddRange(listData.Keys);
                break;
        }

        return keys;
    }

    // ========== SAVE/LOAD INTEGRATION ==========

    private void OnSaveRequested(SaveDataRequestEvent request)
    {
        try
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[GameProgress] Not initialized, skipping save");
                return;
            }

            // Convert dictionaries to serializable format
            var saveData = new GameProgressSaveData();

            // Convert bool dictionary
            saveData.boolKeys = new string[boolData.Count];
            saveData.boolValues = new bool[boolData.Count];
            int i = 0;
            foreach (var kvp in boolData)
            {
                saveData.boolKeys[i] = kvp.Key;
                saveData.boolValues[i] = kvp.Value;
                i++;
            }

            // Convert int dictionary
            saveData.intKeys = new string[intData.Count];
            saveData.intValues = new int[intData.Count];
            i = 0;
            foreach (var kvp in intData)
            {
                saveData.intKeys[i] = kvp.Key;
                saveData.intValues[i] = kvp.Value;
                i++;
            }

            // Convert float dictionary
            saveData.floatKeys = new string[floatData.Count];
            saveData.floatValues = new float[floatData.Count];
            i = 0;
            foreach (var kvp in floatData)
            {
                saveData.floatKeys[i] = kvp.Key;
                saveData.floatValues[i] = kvp.Value;
                i++;
            }

            // Convert string dictionary
            saveData.stringKeys = new string[stringData.Count];
            saveData.stringValues = new string[stringData.Count];
            i = 0;
            foreach (var kvp in stringData)
            {
                saveData.stringKeys[i] = kvp.Key;
                saveData.stringValues[i] = kvp.Value;
                i++;
            }

            // Convert list dictionary
            saveData.listKeys = new string[listData.Count];
            saveData.listValuesJson = new string[listData.Count];
            i = 0;
            foreach (var kvp in listData)
            {
                saveData.listKeys[i] = kvp.Key;
                saveData.listValuesJson[i] = string.Join("|", kvp.Value); // Simple delimiter
                i++;
            }

            string json = JsonUtility.ToJson(saveData);

            Events.Publish(new SaveDataResponseEvent
            {
                systemName = "GameProgress",
                saveData = json,
                operationId = request.operationId,
                responseTime = DateTime.Now,
                success = true
            });

            if (debugMode)
            {
                Debug.Log($"[GameProgress] Save data sent ({json.Length} bytes, {GetTotalValueCount()} values)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Save error: {e.Message}");
        }
    }
    private void OnLoadRequested(LoadDataEvent loadEvent)
    {
        try
        {
            if (loadEvent.systemData == null || !loadEvent.systemData.ContainsKey("GameProgress"))
            {
                if (debugMode) Debug.Log("[GameProgress] No saved data found, starting fresh");
                return;
            }

            string json = loadEvent.systemData["GameProgress"];

            if (string.IsNullOrEmpty(json))
            {
                if (debugMode) Debug.Log("[GameProgress] Empty save data, starting fresh");
                return;
            }

            var saveData = JsonUtility.FromJson<GameProgressSaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("[GameProgress] Failed to deserialize save data");
                return;
            }

            // Convert arrays back to dictionaries
            boolData = new Dictionary<string, bool>();
            if (saveData.boolKeys != null && saveData.boolValues != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.boolKeys.Length, saveData.boolValues.Length); i++)
                {
                    boolData[saveData.boolKeys[i]] = saveData.boolValues[i];
                }
            }

            intData = new Dictionary<string, int>();
            if (saveData.intKeys != null && saveData.intValues != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.intKeys.Length, saveData.intValues.Length); i++)
                {
                    intData[saveData.intKeys[i]] = saveData.intValues[i];
                }
            }

            floatData = new Dictionary<string, float>();
            if (saveData.floatKeys != null && saveData.floatValues != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.floatKeys.Length, saveData.floatValues.Length); i++)
                {
                    floatData[saveData.floatKeys[i]] = saveData.floatValues[i];
                }
            }

            stringData = new Dictionary<string, string>();
            if (saveData.stringKeys != null && saveData.stringValues != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.stringKeys.Length, saveData.stringValues.Length); i++)
                {
                    stringData[saveData.stringKeys[i]] = saveData.stringValues[i];
                }
            }

            listData = new Dictionary<string, List<string>>();
            if (saveData.listKeys != null && saveData.listValuesJson != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.listKeys.Length, saveData.listValuesJson.Length); i++)
                {
                    var items = saveData.listValuesJson[i].Split('|');
                    listData[saveData.listKeys[i]] = new List<string>(items);
                }
            }

            isInitialized = true;

            if (debugMode)
            {
                Debug.Log($"[GameProgress] Loaded successfully - {GetTotalValueCount()} values restored");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Load error: {e.Message}");
        }
    }
}

// ========== SAVE DATA STRUCTURE ==========

[System.Serializable]
public class GameProgressSaveData
{
    // Bool data
    public string[] boolKeys;
    public bool[] boolValues;

    // Int data
    public string[] intKeys;
    public int[] intValues;

    // Float data
    public string[] floatKeys;
    public float[] floatValues;

    // String data
    public string[] stringKeys;
    public string[] stringValues;

    // List data
    public string[] listKeys;
    public string[] listValuesJson; // Stored as pipe-delimited strings
}