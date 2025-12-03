using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemEntry
    {
        public string id;
        public ItemData itemData;
        public SlotPriority slotPriority = SlotPriority.Normal;
    }

    [SerializeField] private List<ItemEntry> items = new List<ItemEntry>();

    private Dictionary<string, ItemData> itemDataLookup = new Dictionary<string, ItemData>();
    private Dictionary<string, SlotPriority> slotPriorityLookup = new Dictionary<string, SlotPriority>();

    public void Initialize()
    {
        itemDataLookup.Clear();
        slotPriorityLookup.Clear();

        foreach (var entry in items)
        {
            if (string.IsNullOrEmpty(entry.id) || entry.itemData == null)
            {
                Debug.LogWarning($"Invalid item entry in database: {entry.id}");
                continue;
            }

            string lowerId = entry.id.ToLower();
            itemDataLookup[lowerId] = entry.itemData;
            slotPriorityLookup[lowerId] = entry.slotPriority;
        }

    }

    public ItemData GetItemData(string itemId)
    {
        if (itemDataLookup.TryGetValue(itemId.ToLower(), out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"Item '{itemId}' not found in database");
        return null;
    }

    public SlotPriority GetSlotPriority(string itemId)
    {
        if (slotPriorityLookup.TryGetValue(itemId.ToLower(), out SlotPriority priority))
        {
            return priority;
        }
        return SlotPriority.Normal;
    }

    public GameObject GetPrefab(string itemId)
    {
        ItemData data = GetItemData(itemId);
        return data?.prefab; 
    }

    public List<string> GetAllItemIds()
    {
        return new List<string>(itemDataLookup.Keys);
    }

    public bool ItemExists(string itemId)
    {
        return itemDataLookup.ContainsKey(itemId.ToLower());
    }

    public List<ItemData> GetAllItems()
    {
        List<ItemData> allItems = new List<ItemData>();
        foreach (var entry in items)
        {
            if (entry.itemData != null)
                allItems.Add(entry.itemData);
        }
        return allItems;
    }

    public List<ItemEntry> GetAllEntries()
    {
        return new List<ItemEntry>(items);
    }

#if UNITY_EDITOR
    public void AddItem(string id, ItemData itemData, SlotPriority priority = SlotPriority.Normal)
    {
        items.Add(new ItemEntry { id = id, itemData = itemData, slotPriority = priority });
    }

    public void ClearDatabase()
    {
        items.Clear();
        itemDataLookup.Clear();
        slotPriorityLookup.Clear();
    }
#endif
}