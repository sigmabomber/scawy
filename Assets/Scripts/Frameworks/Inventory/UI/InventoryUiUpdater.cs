using Doody.GameEvents;
using Doody.InventoryFramework;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIUpdater : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public Image itemIcon;
    public GameObject notificationPanel;
    public TMP_Text notificationText;
    public Image notificationIcon;

    [Header("Settings")]
    public float notificationDuration = 2f;
    public Color successColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;

    private void Start()
    {
        SubscribeToEvents();

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        Events.Subscribe<ItemAddedEvent>(OnItemAdded, this);
        Events.Subscribe<ItemRemovedEvent>(OnItemRemoved, this);
        Events.Subscribe<ItemEquippedEvent>(OnItemEquipped, this);
        Events.Subscribe<ItemUnequippedEvent>(OnItemUnequipped, this);
        Events.Subscribe<ItemUsedEvent>(OnItemUsed, this);
        Events.Subscribe<ItemDroppedEvent>(OnItemDropped, this);
        Events.Subscribe<InventoryFullEvent>(OnInventoryFull, this);
        Events.Subscribe<InventoryToggleEvent>(OnInventoryToggle, this);
        Events.Subscribe<SlotChangedEvent>(OnSlotChanged, this);
    }

    private void OnDestroy()
    {
        Events.UnsubscribeAll(this);
    }

    private void OnItemAdded(ItemAddedEvent e)
    {
        string message = e.WasStacked ?
            $"Stacked {e.ItemData.itemName} (+{e.Quantity})" :
            $"Added {e.ItemData.itemName} x{e.Quantity}";

        ShowNotification(message, e.ItemData.icon, successColor);

        UpdateItemInfo(e.ItemData);
    }

    private void OnItemRemoved(ItemRemovedEvent e)
    {
        ShowNotification($"Removed {e.ItemData.itemName} x{e.Quantity}",
                        e.ItemData.icon, warningColor);
    }

    private void OnItemEquipped(ItemEquippedEvent e)
    {
        ShowNotification($"Equipped {e.ItemData.itemName}",
                        e.ItemData.icon, successColor);
    }

    private void OnItemUnequipped(ItemUnequippedEvent e)
    {
        ShowNotification($"Unequipped {e.ItemData.itemName}",
                        e.ItemData.icon, warningColor);
    }

    private void OnItemUsed(ItemUsedEvent e)
    {
        if (e.WasConsumed)
        {
            ShowNotification($"Used {e.ItemData.itemName}",
                            e.ItemData.icon, successColor);
        }
        else
        {
            ShowNotification($"Used {e.ItemData.itemName}",
                            e.ItemData.icon, Color.cyan);
        }
    }

    private void OnItemDropped(ItemDroppedEvent e)
    {
        ShowNotification($"Dropped {e.ItemData.itemName}",
                        e.ItemData.icon, warningColor);
    }

    private void OnInventoryFull(InventoryFullEvent e)
    {
        ShowNotification($"Inventory full! Could not add {e.ItemData.itemName}",
                        e.ItemData.icon, errorColor);
    }

    private void OnInventoryToggle(InventoryToggleEvent e)
    {
        if (e.IsOpen)
        {
            ClearItemInfo();
            ShowNotification("Inventory opened", null, Color.white);
        }
        else
        {
            ShowNotification("Inventory closed", null, Color.white);
        }
    }

    private void OnSlotChanged(SlotChangedEvent e)
    {
    }

    private void ShowNotification(string message, Sprite icon, Color color)
    {
        if (notificationPanel == null || notificationText == null) return;

        notificationText.text = message;
        notificationText.color = color;

        if (notificationIcon != null && icon != null)
        {
            notificationIcon.sprite = icon;
            notificationIcon.gameObject.SetActive(true);
        }
        else if (notificationIcon != null)
        {
            notificationIcon.gameObject.SetActive(false);
        }

        notificationPanel.SetActive(true);

        CancelInvoke(nameof(HideNotification));
        Invoke(nameof(HideNotification), notificationDuration);
    }

    private void HideNotification()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private void UpdateItemInfo(ItemData itemData)
    {
        if (itemNameText != null)
            itemNameText.text = itemData.itemName;

        if (itemIcon != null && itemData.icon != null)
        {
            itemIcon.sprite = itemData.icon;
            itemIcon.gameObject.SetActive(true);
        }
    }

    private void ClearItemInfo()
    {
        if (itemNameText != null)
            itemNameText.text = "";

        if (itemDescriptionText != null)
            itemDescriptionText.text = "";

        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);
    }
}