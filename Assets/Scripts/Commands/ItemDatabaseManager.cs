using System.Collections.Generic;
using UnityEngine;

public class ItemDatabaseManager : MonoBehaviour
{
    public static ItemDatabaseManager Instance { get; private set; }

    [SerializeField] private ItemDatabase itemDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (itemDatabase != null)
        {
            itemDatabase.Initialize();
        }
        else
        {
            Debug.LogError("ItemDatabase not assigned to ItemDatabaseManager!");
        }
    }

    public ItemData GetItem(string itemId)
    {
        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase not initialized!");
            return null;
        }

        return itemDatabase.GetItemData(itemId);
    }

    public SlotPriority GetSlotPriority(string itemId)
    {
        if (itemDatabase == null) return SlotPriority.Normal;
        return itemDatabase.GetSlotPriority(itemId);
    }

    public GameObject GetPrefab(string itemId)
    {
        ItemData data = GetItem(itemId);
        return data?.prefab;
    }

    public List<string> GetAllItemIds()
    {
        if (itemDatabase == null) return new List<string>();
        return itemDatabase.GetAllItemIds();
    }

    public bool ItemExists(string itemId)
    {
        if (itemDatabase == null) return false;
        return itemDatabase.ItemExists(itemId);
    }

    public string GetItemName(string itemId)
    {
        ItemData data = GetItem(itemId);
        return data?.itemName ?? "Unknown Item";
    }

    public List<ItemData> GetAllItems()
    {
        if (itemDatabase == null) return new List<ItemData>();
        return itemDatabase.GetAllItems();
    }
}