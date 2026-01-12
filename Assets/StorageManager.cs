using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all storage containers in the game
/// </summary>
public class StorageManager : MonoBehaviour
{
    public static StorageManager Instance { get; private set; }

    private Dictionary<string, StorageContainer> allStorageContainers = new Dictionary<string, StorageContainer>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Register a storage container with the manager
    /// </summary>
    public void RegisterStorage(StorageContainer storage)
    {
        if (!allStorageContainers.ContainsKey(storage.StorageId))
        {
            allStorageContainers.Add(storage.StorageId, storage);
            Debug.Log($"Registered storage: {storage.StorageId}");
        }
    }

    /// <summary>
    /// Unregister a storage container
    /// </summary>
    public void UnregisterStorage(StorageContainer storage)
    {
        if (allStorageContainers.ContainsKey(storage.StorageId))
        {
            allStorageContainers.Remove(storage.StorageId);
            Debug.Log($"Unregistered storage: {storage.StorageId}");
        }
    }

    /// <summary>
    /// Get all storage containers in the game
    /// </summary>
    public List<StorageContainer> GetAllStorageContainers()
    {
        return new List<StorageContainer>(allStorageContainers.Values);
    }

    /// <summary>
    /// Find a storage container by ID
    /// </summary>
    public StorageContainer GetStorageById(string storageId)
    {
        if (allStorageContainers.TryGetValue(storageId, out var storage))
        {
            return storage;
        }
        return null;
    }

    /// <summary>
    /// Get the number of registered storage containers
    /// </summary>
    public int GetStorageCount()
    {
        return allStorageContainers.Count;
    }

    /// <summary>
    /// Check if a storage container exists by ID
    /// </summary>
    public bool StorageExists(string storageId)
    {
        return allStorageContainers.ContainsKey(storageId);
    }

    /// <summary>
    /// Clear all registered storage containers (use when changing scenes)
    /// </summary>
    public void ClearAllStorages()
    {
        allStorageContainers.Clear();
        Debug.Log("Cleared all storage containers");
    }
}