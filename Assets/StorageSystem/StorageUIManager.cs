using Doody.Framework.UI;
using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [Header("Controller Support")]
    public GameObject xboxInfoPanel;
    public GameObject psInfoPanel;

    private StorageContainer currentStorage;
    private List<StorageSlotUI> storageSlots = new List<StorageSlotUI>();
    private List<StorageSlotUI> playerNormalSlots = new List<StorageSlotUI>();
    private List<StorageSlotUI> playerDedicatedSlots = new List<StorageSlotUI>();
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

        if (StorageNavigation.Instance == null)
        {
            GameObject navObj = new GameObject("StorageNavigation");
            navObj.AddComponent<StorageNavigation>();
        }
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
            StorageSlotUI[] normalSlots = playerNormalSlotsContainer.GetComponentsInChildren<StorageSlotUI>(true);
            foreach (var slot in normalSlots)
            {
                playerNormalSlots.Add(slot);
                slot.slotType = StorageSlotUI.SlotType.Inventory;
                slot.slotPriority = SlotPriority.Normal;
            }
        }

        if (playerDedicatedSlotsContainer != null)
        {
            StorageSlotUI[] dedicatedSlots = playerDedicatedSlotsContainer.GetComponentsInChildren<StorageSlotUI>(true);
            foreach (var slot in dedicatedSlots)
            {
                playerDedicatedSlots.Add(slot);
                slot.slotType = StorageSlotUI.SlotType.Inventory;
                slot.slotPriority = SlotPriority.Dedicated;
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
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseStorage();
            }

          
        }
    }

    public void OpenStorage(StorageContainer storage)
    {
        if (storage == null) return;

        if (currentStorage != null && currentStorage != storage)
        {
            SaveCurrentStorageState();
        }

        if (storageUIPanel.activeSelf)
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

        if (!storageUIPanel.activeSelf)
        {
            storageUIPanel.SetActive(true);
        }

        if (StorageNavigation.Instance != null)
        {
            StorageNavigation.Instance.SetStorageOpen(true);
            UpdateControllerInfoPanels();
        }

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

        if (StorageNavigation.Instance != null)
        {
            StorageNavigation.Instance.SetStorageOpen(false);
        }

        if (xboxInfoPanel != null) xboxInfoPanel.SetActive(false);
        if (psInfoPanel != null) psInfoPanel.SetActive(false);

        currentStorage = null;
        isOpen = false;
    }

    private void UpdateControllerInfoPanels()
    {
        if (InputDetector.Instance == null) return;

        bool usingController = InputDetector.Instance.IsUsingController();
        if (!usingController) return;

        string controllerName = InputDetector.Instance.GetControllerName();

        if (xboxInfoPanel != null) xboxInfoPanel.SetActive(controllerName == "Xbox" || controllerName == "Generic");
        if (psInfoPanel != null) psInfoPanel.SetActive(controllerName == "PlayStation");
    }

    public void RefreshStorageSlots()
    {
        foreach (var slot in GetAllStorageSlotsIncludingInventory())
        {
            if (slot != null)
            {
                if (slot.ItemData != null)
                {
                    slot.SetItem(slot.ItemData, slot.Quantity, slot.ItemPrefab);
                }
            }
        }

        if (StorageNavigation.Instance != null)
        {
            StorageNavigation.Instance.RefreshSlots();
        }
    }

    public List<StorageSlotUI> GetAllStorageSlotsIncludingInventory()
    {
        var allSlots = new List<StorageSlotUI>();
        allSlots.AddRange(storageSlots);
        allSlots.AddRange(playerNormalSlots);
        allSlots.AddRange(playerDedicatedSlots);
        return allSlots;
    }

    public List<StorageSlotUI> GetAllStorageSlots()
    {
        return new List<StorageSlotUI>(storageSlots);
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
        LoadPlayerSlots(playerNormalSlots, realNormalSlots);

        var realDedicatedSlots = InventorySystem.Instance.dedicatedInventorySlots;
        LoadPlayerSlots(playerDedicatedSlots, realDedicatedSlots);
    }

    private void LoadPlayerSlots(List<StorageSlotUI> mirrorSlots, List<InventorySlotsUI> realSlots)
    {
        if (mirrorSlots == null || realSlots == null) return;

        for (int i = 0; i < mirrorSlots.Count && i < realSlots.Count; i++)
        {
            var mirrorSlot = mirrorSlots[i];
            var realSlot = realSlots[i];

            if (mirrorSlot != null && realSlot != null)
            {
                mirrorSlot.SetInventoryReference(realSlot, mirrorSlot.slotPriority);

                mirrorSlot.SyncFromInventory();
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

        foreach (var slot in GetAllPlayerStorageSlots())
        {
            if (slot.slotType == StorageSlotUI.SlotType.Inventory)
            {
                slot.SyncToInventory();
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

    public List<StorageSlotUI> GetAllPlayerStorageSlots()
    {
        var allSlots = new List<StorageSlotUI>();
        allSlots.AddRange(playerNormalSlots);
        allSlots.AddRange(playerDedicatedSlots);
        return allSlots;
    }

    public List<InventorySlotsUI> GetAllPlayerMirrorSlots()
    {
       
        return new List<InventorySlotsUI>();
    }

    private void CleanupStorageUIOnly()
    {
        Events.Publish(new StorageClosedEvent(currentStorage?.StorageId));
        currentStorage = null;
        isOpen = false;
    }

    public StorageContainer GetCurrentStorage() => currentStorage;
    public bool IsOpen => isOpen;
}