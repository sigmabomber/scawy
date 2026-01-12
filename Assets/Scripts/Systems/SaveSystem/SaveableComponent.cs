using UnityEngine;
using System;


public class SaveableComponent : MonoBehaviour
{
    [Header("Save Settings")]
    [Tooltip("What to call this in save file")]
    public string saveName = "Unnamed";

    [Tooltip("Should this be saved?")]
    public bool enableSaving = true;

    [Tooltip("Show debug messages")]
    public bool debugMode = false;


    [Header("Example: Player Position")]
    public Vector3 playerPosition;

    [Header("Example: Player Stats")]
    public int health = 100;
    public int ammo = 30;
    public float stamina = 100f;

    [Header("Example: Inventory")]
    public string[] inventoryItems = new string[10];
    public int[] itemCounts = new int[10];

    [Header("Example: Game States")]
    public bool doorUnlocked = false;
    public bool questCompleted = false;
    public int enemiesKilled = 0;


    public string GetSaveData()
    {
        if (!enableSaving) return "";

        // Create a simple data object
        SaveableData data = new SaveableData();

        // ⭐ ADD YOUR SAVE DATA HERE ⭐
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.scale = transform.localScale;
        data.health = health;
        data.ammo = ammo;
        data.stamina = stamina;
        data.doorUnlocked = doorUnlocked;
        data.questCompleted = questCompleted;
        data.enemiesKilled = enemiesKilled;

        // Convert to JSON
        string json = JsonUtility.ToJson(data);

        if (debugMode) Debug.Log($"[{saveName}] Saving: {json}");
        return json;
    }

    public void LoadSaveData(string json)
    {
        if (!enableSaving || string.IsNullOrEmpty(json)) return;

        try
        {
        
            SaveableData data = JsonUtility.FromJson<SaveableData>(json);

       
            transform.position = data.position;
            transform.rotation = data.rotation;
            transform.localScale = data.scale;
            health = data.health;
            ammo = data.ammo;
            stamina = data.stamina;
            doorUnlocked = data.doorUnlocked;
            questCompleted = data.questCompleted;
            enemiesKilled = data.enemiesKilled;

            if (debugMode) Debug.Log($"[{saveName}] Loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{saveName}] Load failed: {e.Message}");
        }
    }

    // ========== EXAMPLE METHODS ==========

    /// <summary>
    /// Example: Take damage
    /// </summary>
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (debugMode) Debug.Log($"[{saveName}] Health: {health}");
    }

    /// <summary>
    /// Example: Add item to inventory
    /// </summary>
    public void AddItem(string itemName, int count = 1)
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (string.IsNullOrEmpty(inventoryItems[i]))
            {
                inventoryItems[i] = itemName;
                itemCounts[i] = count;
                if (debugMode) Debug.Log($"[{saveName}] Added {count}x {itemName}");
                return;
            }
        }
        Debug.LogWarning($"[{saveName}] Inventory full!");
    }

    /// <summary>
    /// Example: Unlock a door
    /// </summary>
    public void UnlockDoor()
    {
        doorUnlocked = true;
        if (debugMode) Debug.Log($"[{saveName}] Door unlocked!");
    }

    /// <summary>
    /// Example: Complete quest
    /// </summary>
    public void CompleteQuest()
    {
        questCompleted = true;
        if (debugMode) Debug.Log($"[{saveName}] Quest completed!");
    }

    void Start()
    {
        if (debugMode)
        {
            Debug.Log($"[{saveName}] Saveable component ready!");
        }
    }
}

/// <summary>
/// Simple data structure for saving
/// </summary>
[System.Serializable]
public class SaveableData
{
    // Transform
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    // Stats
    public int health;
    public int ammo;
    public float stamina;

    // Game states
    public bool doorUnlocked;
    public bool questCompleted;
    public int enemiesKilled;

    // Add more variables as needed!
}