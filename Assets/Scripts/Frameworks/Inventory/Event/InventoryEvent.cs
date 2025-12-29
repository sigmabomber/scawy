using UnityEngine;

namespace Doody.InventoryFramework
{
    /// <summary>
    /// Inventory-related events that can be subscribed to
    /// </summary>

    // Base inventory event
    public abstract class InventoryEvent
    {
        public string SystemId { get; protected set; }
        public ItemData ItemData { get; protected set; }
        public int Quantity { get; protected set; }
    }

    // Item added to inventory
    public class ItemAddedEvent : InventoryEvent
    {
        public GameObject ItemObject { get; private set; }
        public SlotPriority Priority { get; private set; }
        public bool WasStacked { get; private set; }

        public ItemAddedEvent(string systemId, ItemData itemData, int quantity,
                             GameObject itemObj, SlotPriority priority, bool wasStacked)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            ItemObject = itemObj;
            Priority = priority;
            WasStacked = wasStacked;
        }
    }

    // Item removed from inventory
    public class ItemRemovedEvent : InventoryEvent
    {
        public ItemRemovedEvent(string systemId, ItemData itemData, int quantity)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
        }
    }



    // Item equipped
    public class ItemEquippedEvent : InventoryEvent
    {
        public InventorySlotsUI Slot { get; private set; }
        public IEquippable Equippable { get; private set; }

        public ItemEquippedEvent(string systemId, ItemData itemData, int quantity,
                                InventorySlotsUI slot, IEquippable equippable)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            Slot = slot;
            Equippable = equippable;
        }
    }

    // Item unequipped
    public class ItemUnequippedEvent : InventoryEvent
    {
        public InventorySlotsUI Slot { get; private set; }

        public ItemUnequippedEvent(string systemId, ItemData itemData, int quantity,
                                  InventorySlotsUI slot)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            Slot = slot;
        }
    }

    // Item used (consumable)
    public class ItemUsedEvent : InventoryEvent
    {
        public InventorySlotsUI Slot { get; private set; }
        public bool WasConsumed { get; private set; }

        public ItemUsedEvent(string systemId, ItemData itemData, int quantity,
                            InventorySlotsUI slot, bool wasConsumed)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            Slot = slot;
            WasConsumed = wasConsumed;
        }
    }

    // Item dropped in world
    public class ItemDroppedEvent : InventoryEvent
    {
        public GameObject DroppedObject { get; private set; }
        public Vector3 DropPosition { get; private set; }

        public ItemDroppedEvent(string systemId, ItemData itemData, int quantity,
                               GameObject droppedObj, Vector3 dropPosition)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            DroppedObject = droppedObj;
            DropPosition = dropPosition;
        }
    }

    // Inventory opened/closed
    public class InventoryToggleEvent
    {
        public string SystemId { get; private set; }
        public bool IsOpen { get; private set; }

        public InventoryToggleEvent(string systemId, bool isOpen)
        {
            SystemId = systemId;
            IsOpen = isOpen;
        }
    }

    // Slot changed (item added/removed/moved)
    public class SlotChangedEvent
    {
        public string SystemId { get; private set; }
        public InventorySlotsUI Slot { get; private set; }
        public ItemData PreviousItem { get; private set; }
        public int PreviousQuantity { get; private set; }

        public SlotChangedEvent(string systemId, InventorySlotsUI slot,
                               ItemData previousItem, int previousQuantity)
        {
            SystemId = systemId;
            Slot = slot;
            PreviousItem = previousItem;
            PreviousQuantity = previousQuantity;
        }
    }

    // Inventory full
    public class InventoryFullEvent
    {
        public string SystemId { get; private set; }
        public ItemData ItemData { get; private set; }
        public int AttemptedQuantity { get; private set; }

        public InventoryFullEvent(string systemId, ItemData itemData, int attemptedQuantity)
        {
            SystemId = systemId;
            ItemData = itemData;
            AttemptedQuantity = attemptedQuantity;
        }
    }

    // Inventory system registered with framework
    public class InventorySystemRegistered
    {
        public string SystemId { get; private set; }

        public InventorySystemRegistered(string systemId)
        {
            SystemId = systemId;
        }
    }

    public class ItemCraftedEvent
    {
        public string SystemId { get; private set; }
        public ItemData ItemData { get; private set; }
        public int Quantity { get; private set; }
        public string RecipeName { get; private set; }

        public ItemCraftedEvent(string systemId, ItemData itemData, int quantity, string recipeName)
        {
            SystemId = systemId;
            ItemData = itemData;
            Quantity = quantity;
            RecipeName = recipeName;
        }
    }
}