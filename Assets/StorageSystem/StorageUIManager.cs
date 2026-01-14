using Doody.Framework.UI;
using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the shared storage UI with priority slot support
/// </summary>
public class StorageUIManager : MonoBehaviour
{
    public static StorageUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject storageUIPanel;
    [SerializeField] private Transform storageSlotsContainer;
    [SerializeField] private Transform playerNormalSlotsContainer;
    [SerializeField] private Transform playerDedicatedSlotsContainer; 

    private StorageContainer currentStorage;
    private List<StorageSlotUI> storageSlots = new List<StorageSlotUI>();
    private List<InventorySlotsUI> playerNormalSlots = new List<InventorySlotsUI>();
    private List<InventorySlotsUI> playerDedicatedSlots = new List<InventorySlotsUI>(); 
    private bool isOpen = false;
    private bool slotsInitialized = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (storageUIPanel != null)
            storageUIPanel.SetActive(false);

        Events.Subscribe<UIClosedEvent>(OnUIClosedEvent, this);
        InitializeSlots();
    }

    private void OnDestroy()
    {
        Events.UnsubscribeAll(this);
    }
    

    private void InitializeSlots()
    {
        if (slotsInitialized) return;

        StorageSlotUI[] existingStorageSlots = storageSlotsContainer.GetComponentsInChildren<StorageSlotUI>(true);
        foreach (var slot in existingStorageSlots)
        {
            storageSlots.Add(slot);
        }

        if (playerNormalSlotsContainer != null)
        {
            InventorySlotsUI[] normalSlots = playerNormalSlotsContainer.GetComponentsInChildren<InventorySlotsUI>(true);
            foreach (var slot in normalSlots)
            {
                playerNormalSlots.Add(slot);
            }
        }

        if (playerDedicatedSlotsContainer != null)
        {
            InventorySlotsUI[] dedicatedSlots = playerDedicatedSlotsContainer.GetComponentsInChildren<InventorySlotsUI>(true);
            foreach (var slot in dedicatedSlots)
            {
                playerDedicatedSlots.Add(slot);
            }
        }

        slotsInitialized = true;
    }

    private void OnUIClosedEvent(UIClosedEvent data)
    {
        if (data.UIObject == storageUIPanel && isOpen)
        {
            SaveCurrentStorageState();
            SyncPlayerInventory();
            CleanupStorageUIOnly();
        }
    }

    private void Update()
    {
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
        {
            CloseStorage();
        }
    }

    public void OpenStorage(StorageContainer storage)
    {
        if (storage == null) return;

        if (currentStorage != null && currentStorage != storage)
        {
            SaveCurrentStorageState();
        }

        if(storageUIPanel.activeSelf)
        {

            Events.Publish(new UIRequestCloseEvent(storageUIPanel));
            return;
        }

        currentStorage = storage;
        isOpen = true;

        

        LoadStorageData();
        LoadPlayerInventoryData();

        Events.Publish(new UIRequestOpenEvent(storageUIPanel));

        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Events.Publish(new StorageOpenedEvent(storage.StorageId));
    }

    public void CloseStorage()
    {
        if (!isOpen) return;

        SaveCurrentStorageState();
        SyncPlayerInventory();

        Events.Publish(new UIRequestCloseEvent(storageUIPanel));

        storageUIPanel.SetActive(false);

        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Events.Publish(new StorageClosedEvent(currentStorage?.StorageId));

        currentStorage = null;
        isOpen = false;

    }

    private void LoadStorageData()
    {
        if (currentStorage == null) return;

        foreach (var slot in storageSlots)
        {
            slot.ClearSlot();
        }

        for (int i = 0; i < storageSlots.Count && i < currentStorage.SlotCount; i++)
        {
            StorageSlotData data = currentStorage.GetSlotData(i);

            if (data != null && data.itemData != null)
            {
                storageSlots[i].SetItem(data.itemData, data.quantity, data.itemPrefab);
            }

            storageSlots[i].SetStorageReference(currentStorage, i);
        }

    }

    private void LoadPlayerInventoryData()
    {
        if (InventorySystem.Instance == null) return;

        var realNormalSlots = InventorySystem.Instance.normalInventorySlots;
        LoadPlayerSlots(playerNormalSlots, realNormalSlots, SlotPriority.Normal);

        var realDedicatedSlots = InventorySystem.Instance.dedicatedInventorySlots;
        LoadPlayerSlots(playerDedicatedSlots, realDedicatedSlots, SlotPriority.Dedicated);
    }

    private void LoadPlayerSlots(List<InventorySlotsUI> mirrorSlots, List<InventorySlotsUI> realSlots, SlotPriority priority)
    {
        if (mirrorSlots == null || realSlots == null) return;

        for (int i = 0; i < mirrorSlots.Count; i++)
        {
            var mirrorSlot = mirrorSlots[i];

            if (!mirrorSlot.gameObject.activeInHierarchy)
            {
                mirrorSlot.gameObject.SetActive(true);
            }

            mirrorSlot.enabled = true;
            mirrorSlot.InitializeForStorageUI();
            mirrorSlot.ClearSlot();

            mirrorSlot.slotPriority = priority;

            mirrorSlot.useItem = false;
        }

        for (int i = 0; i < mirrorSlots.Count && i < realSlots.Count; i++)
        {
            var realSlot = realSlots[i];
            var mirrorSlot = mirrorSlots[i];

            if (realSlot.itemData != null)
            {
                mirrorSlot.SetItemPublic(realSlot.itemData, realSlot.quantity, realSlot.instantiatedPrefab);

                if (realSlot.itemData is FlashlightItemData flashData)
                {
                    if (mirrorSlot.usageSlider != null)
                    {
                        mirrorSlot.usageSlider.gameObject.SetActive(true);
                        mirrorSlot.usageSlider.maxValue = flashData.maxBattery;

                        if (realSlot.instantiatedPrefab != null)
                        {
                            FlashlightBehavior flashBehavior = realSlot.instantiatedPrefab.GetComponent<FlashlightBehavior>();
                            if (flashBehavior != null)
                            {
                                mirrorSlot.usageSlider.value = flashBehavior.GetCurrentBattery();
                            }
                            else
                            {
                                mirrorSlot.usageSlider.value = flashData.maxBattery;
                            }
                        }
                        else
                        {
                            mirrorSlot.usageSlider.value = flashData.maxBattery;
                        }
                    }
                }
                else
                {
                    if (mirrorSlot.usageSlider != null)
                    {
                        mirrorSlot.usageSlider.gameObject.SetActive(false);
                    }
                }
            }

            var graphic = mirrorSlot.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            if (mirrorSlot.icon != null)
            {
                mirrorSlot.icon.raycastTarget = true;
            }
        }
    }

    private void SaveCurrentStorageState()
    {
        if (currentStorage == null) return;

        for (int i = 0; i < storageSlots.Count; i++)
        {
            if (storageSlots[i] != null)
            {
                var slotUI = storageSlots[i];
                currentStorage.SetSlotData(i, slotUI.ItemData, slotUI.Quantity, slotUI.ItemPrefab);
            }
        }

    }

    private void SyncPlayerInventory()
    {
        if (InventorySystem.Instance == null) return;

        var realNormalSlots = InventorySystem.Instance.normalInventorySlots;
        SyncPlayerSlots(playerNormalSlots, realNormalSlots);

        var realDedicatedSlots = InventorySystem.Instance.dedicatedInventorySlots;
        SyncPlayerSlots(playerDedicatedSlots, realDedicatedSlots);

    }

    private void SyncPlayerSlots(List<InventorySlotsUI> mirrorSlots, List<InventorySlotsUI> realSlots)
    {
        if (mirrorSlots == null || realSlots == null) return;

        for (int i = 0; i < mirrorSlots.Count && i < realSlots.Count; i++)
        {
            if (mirrorSlots[i] != null)
            {
                var mirrorSlot = mirrorSlots[i];
                var realSlot = realSlots[i];

                if (mirrorSlot.itemData != null)
                {
                    realSlot.SetItem(mirrorSlot.itemData, mirrorSlot.quantity, mirrorSlot.instantiatedPrefab);
                }
                else
                {
                    realSlot.ClearSlot();
                }
            }
        }
    }
    /// <summary>
    /// Save storage and sync player inventory immediately (called after each transfer)
    /// </summary>
    public void SaveAndSyncImmediate()
    {
        SaveCurrentStorageState();
        SyncPlayerInventory();
    }
    public List<StorageSlotUI> GetStorageSlots()
    {
        return storageSlots;
    }
    private void CleanupStorageUIOnly()
    {
        Events.Publish(new StorageClosedEvent(currentStorage?.StorageId));
        currentStorage = null;
        isOpen = false;
    }

    public StorageContainer GetCurrentStorage() => currentStorage;
    public bool IsOpen => isOpen;

    public List<InventorySlotsUI> GetAllPlayerMirrorSlots()
    {
        var allSlots = new List<InventorySlotsUI>();
        allSlots.AddRange(playerNormalSlots);
        allSlots.AddRange(playerDedicatedSlots);
        return allSlots;
    }
}

