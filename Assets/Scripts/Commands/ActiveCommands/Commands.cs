using Debugging;
using Doody.Framework.Player.Effects;
using Doody.GameEvents;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Doody.Framework.Player.Effects.EffectEvent;
public class InventoryCommands : MonoBehaviour
{
    private InventorySystem inventorySystem;
    private ItemDatabaseManager itemDatabaseManager;
    void Start()
    {
        inventorySystem = InventorySystem.Instance;

        if (ItemDatabaseManager.Instance == null)
        {
            ConsoleUI.PrintError("ItemDatabaseManager not found! Commands may not work.");
        }
    }

    [Command("additem", "Adds an item to inventory", "additem [itemId] [quantity=1]")]
    public void AddItemCommand(string[] args)
    {
        if (ItemDatabaseManager.Instance == null)
        {
            ConsoleUI.PrintError("ItemDatabaseManager not initialized!");
            return;
        }

        if (args.Length == 0)
        {
            ConsoleUI.PrintError("Usage: additem [itemId] [quantity=1]");
            ConsoleUI.Print("Example: additem flashlight 1");
            ShowAvailableItems();
            return;
        }

        string itemId = args[0].ToLower();
        int quantity = 1;

        if (args.Length > 1)
        {
            if (!int.TryParse(args[1], out quantity) || quantity <= 0)
            {
                ConsoleUI.PrintError($"Invalid quantity: {args[1]}");
                return;
            }
        }

        if (!ItemDatabaseManager.Instance.ItemExists(itemId))
        {
            ConsoleUI.PrintError($"Item '{itemId}' not found in database!");
            ShowAvailableItems();
            return;
        }

        ItemData itemData = ItemDatabaseManager.Instance.GetItem(itemId);
        if (itemData == null)
        {
            ConsoleUI.PrintError($"Failed to load item '{itemId}'");
            return;
        }

        SlotPriority slot = ItemDatabaseManager.Instance.GetSlotPriority(itemId);

        bool success;

        if (itemData.maxStack < 2)
        {
            int itemsAdded = 0;

            for (int i = 0; i < quantity; i++)
            {
                bool itemAdded = inventorySystem.AddItem(itemData, 1, slot);

                if (itemAdded)
                {
                    itemsAdded++;
                }
                else
                {
                    if (itemsAdded > 0)
                    {
                        ConsoleUI.PrintWarning($"Partially added {itemsAdded}x {itemData.itemName} ({quantity - itemsAdded} couldn't fit)");
                    }
                    else
                    {
                        ConsoleUI.PrintError($"Failed to add {itemData.itemName} (inventory may be full)");
                    }
                    return;
                }
            }

            success = true;
            ConsoleUI.PrintSuccess($"Added {quantity}x {itemData.itemName} to inventory");
        }
        else
        {
            success = inventorySystem.AddItem(itemData, quantity, slot);

            if (success)
            {
                ConsoleUI.PrintSuccess($"Added {quantity}x {itemData.itemName} to inventory");
            }
            else
            {
                ConsoleUI.PrintError($"Failed to add {itemData.itemName} (inventory may be full)");
            }
        }
    }
    [Command("give_all", "Gives one of every item in database", "give_all")]
    public void GiveAllCommand(string[] args)
    {
        if (ItemDatabaseManager.Instance == null)
        {
            ConsoleUI.PrintError("ItemDatabaseManager not initialized!");
            return;
        }

        var allItems = ItemDatabaseManager.Instance.GetAllItems();
        int successCount = 0;
        int failCount = 0;

        ConsoleUI.Print($"Adding {allItems.Count} items to inventory...");

        foreach (var itemData in allItems)
        {
            if (inventorySystem.AddItem(itemData, 1))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        if (failCount == 0)
        {
            ConsoleUI.PrintSuccess($"Successfully added all {successCount} items!");
        }
        else
        {
            ConsoleUI.PrintWarning($"Added {successCount} items, {failCount} failed (inventory may be full)");
        }
    }

    [Command("iteminfo", "Shows detailed info about an item", "iteminfo [itemId]")]
    public void ItemInfoCommand(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleUI.PrintError("Usage: iteminfo [itemId]");
            ShowAvailableItems();
            return;
        }

        string itemId = args[0].ToLower();
        ItemData itemData = ItemDatabaseManager.Instance.GetItem(itemId);

        if (itemData == null)
        {
            ConsoleUI.PrintError($"Item '{itemId}' not found!");
            return;
        }

        ConsoleUI.Print($"=== {itemData.itemName.ToUpper()} ===");
        ConsoleUI.Print($"ID: {itemId}");
        ConsoleUI.Print($"Stackable: {(itemData.maxStack > 1 ? $"Yes (max {itemData.maxStack})" : "No")}");

        if (itemData.prefab != null)
        {
            ConsoleUI.Print($"Has Prefab: Yes ({itemData.prefab.name})");
        }

        if (itemData is FlashlightItemData flashlight)
        {
            ConsoleUI.Print($"Type: Flashlight");
            ConsoleUI.Print($"Battery: {flashlight.maxBattery}");
        }
        else if (itemData is GunItemData gun)
        {
            ConsoleUI.Print($"Type: Gun");
            ConsoleUI.Print($"Ammo: {gun.maxAmmo}/{gun.maxAmmo}");
            ConsoleUI.Print($"Fire Rate: {gun.fireRate} RPM");
        }

        ConsoleUI.Print($"=== END INFO ===");
    }

    [Command("search", "Searches for items by name", "search [search_term]")]
    public void SearchItemsCommand(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleUI.PrintError("Usage: search [search_term]");
            return;
        }

        string searchTerm = args[0].ToLower();
        var allItems = ItemDatabaseManager.Instance.GetAllItems();
        var results = allItems.Where(item =>
            item.itemName.ToLower().Contains(searchTerm)
        ).ToList();

        if (results.Count == 0)
        {
            ConsoleUI.Print($"No items found matching '{searchTerm}'");
            return;
        }

        ConsoleUI.Print($"=== SEARCH RESULTS ({results.Count}) ===");
        foreach (var item in results)
        {
            var itemId = FindItemId(item);
            ConsoleUI.Print($"{itemId}: {item.itemName}");
        }
    }

    private string FindItemId(ItemData itemData)
    {
        var allIds = ItemDatabaseManager.Instance.GetAllItemIds();
        foreach (var id in allIds)
        {
            if (ItemDatabaseManager.Instance.GetItem(id) == itemData)
                return id;
        }
        return "unknown";
    }

    private void ShowAvailableItems()
    {
        var itemIds = ItemDatabaseManager.Instance.GetAllItemIds();

        if (itemIds.Count == 0)
        {
            ConsoleUI.Print("Database is empty");
            return;
        }

        ConsoleUI.Print("Available items:");
        int count = 0;
        foreach (string itemId in itemIds)
        {
            ItemData itemData = ItemDatabaseManager.Instance.GetItem(itemId);
            string stackInfo = itemData.maxStack > 1 ? $"(stack x{itemData.maxStack})" : "";
            ConsoleUI.Print($"  {itemId.PadRight(15)} - {itemData.itemName} {stackInfo}");

            count++;
            if (count >= 20) 
            {
                ConsoleUI.Print($"  ... and {itemIds.Count - count} more items");
                break;
            }
        }
        ConsoleUI.Print($"Total: {itemIds.Count} items in database");
    }

    [Command("db_stats", "Shows database statistics", "db_stats")]
    public void DatabaseStatsCommand(string[] args)
    {
        if (ItemDatabaseManager.Instance == null)
        {
            ConsoleUI.PrintError("ItemDatabaseManager not initialized!");
            return;
        }

        var allItems = ItemDatabaseManager.Instance.GetAllItems();

        int stackableCount = allItems.Count(item => item.maxStack > 1);
        int nonStackableCount = allItems.Count(item => item.maxStack == 1);
        int hasPrefabCount = allItems.Count(item => item.prefab != null);

        ConsoleUI.Print("=== DATABASE STATISTICS ===");
        ConsoleUI.Print($"Total items: {allItems.Count}");
        ConsoleUI.Print($"Stackable items: {stackableCount}");
        ConsoleUI.Print($"Non-stackable items: {nonStackableCount}");
        ConsoleUI.Print($"Items with prefabs: {hasPrefabCount}");

        // Count by type
        int flashlightCount = allItems.Count(item => item is FlashlightItemData);
        int gunCount = allItems.Count(item => item is GunItemData);
        int healCount = allItems.Count(item => item is HealingItemData);

        if (flashlightCount > 0) ConsoleUI.Print($"Flashlights: {flashlightCount}");
        if (gunCount > 0) ConsoleUI.Print($"Guns: {gunCount}");
        if (healCount > 0) ConsoleUI.Print($"Healing items: {healCount}");
    }




    private void ShowAvailableEffects()
    {
        ConsoleUI.Print("Available Effects:");
        foreach (EffectType effect in Enum.GetValues(typeof(EffectType)))
        {
            ConsoleUI.Print(effect.ToString());
        }


    }

    [Command("give_effect", "Adds Status effect to player", "give_effect [EffectType] [duration] [strength]")]
    public void GiveEffectCommand(string[] args)
    {

        if (args.Length == 0)
        {
            ConsoleUI.PrintError("Usage: give_effect [effecttype] [duration = 1 (sec)] [strength = 1.2 (multiplier)]");
            ConsoleUI.Print("Example: give_effect speed 10 1.5");
            ShowAvailableEffects();
            return;
        }

        if (!Enum.TryParse<EffectType>(args[0], true, out var effectType))
        {
            ConsoleUI.PrintError($"Unknown effect type: {args[0]}");
            ShowAvailableEffects();
            return;
        }
        float duration = args.Length > 1 ? float.Parse(args[1]) : 1f;

        float strength = args.Length > 2 ? float.Parse(args[2]) : 1.2f;

        Events.Publish(new AddEffect(effectType, duration, strength));

        ConsoleUI.PrintSuccess($"Successfully applied {effectType} for {duration}s @ {strength}x");
    }

    [Command("give_all_effect", "Adds all status effects to player", "give_all_effect [duration] [strength")]

    public void GiveAllEffectsCommand(string[] args)
    {
        float duration = args.Length > 1 ? float.Parse(args[0]) : 1f;

        float strength = args.Length > 2 ? float.Parse(args[1]) : 1.2f;

        foreach (EffectType effect in Enum.GetValues(typeof(EffectType)))
        {

            Events.Publish(new AddEffect(effect, duration, strength));
        }

        
        ConsoleUI.PrintSuccess($"Successfully applied all effects for {duration}s @ {strength}x");
    }
}