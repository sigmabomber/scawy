using Doody.Framework.UI;
using Doody.GameEvents;
using Doody.InventoryFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the shared storage UI
/// ONE UI for ALL storage containers
/// REUSES slots instead of instantiating/destroying
/// </summary>
public class StorageUIManager : MonoBehaviour
{
    public static StorageUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject storageUIPanel;
    [SerializeField] private Transform storageSlotsContainer;
    [SerializeField] private Transform playerInventorySlotsContainer;

    private StorageContainer currentStorage;
    private List<StorageSlotUI> storageSlots = new List<StorageSlotUI>();
    private List<InventorySlotsUI> playerInventorySlots = new List<InventorySlotsUI>();
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

        // Initialize slots once at start
        InitializeSlots();
    }

    private void OnDestroy()
    {
        Events.UnsubscribeAll(this);
    }

    private void InitializeSlots()
    {
        if (slotsInitialized)
        {
            Debug.Log("[StorageUIManager] Slots already initialized");
            return;
        }

        Debug.Log($"[StorageUIManager] Initializing slots...");

        // Get existing storage slots from container
        StorageSlotUI[] existingStorageSlots = storageSlotsContainer.GetComponentsInChildren<StorageSlotUI>(true);
        foreach (var slot in existingStorageSlots)
        {
            storageSlots.Add(slot);
            Debug.Log($"[StorageUIManager] Found StorageSlot: {slot.gameObject.name}, Active: {slot.gameObject.activeInHierarchy}");
        }

        // Get existing player inventory slots from container
        InventorySlotsUI[] existingPlayerSlots = playerInventorySlotsContainer.GetComponentsInChildren<InventorySlotsUI>(true);
        foreach (var slot in existingPlayerSlots)
        {
            playerInventorySlots.Add(slot);
            Debug.Log($"[StorageUIManager] Found PlayerSlot: {slot.gameObject.name}, Active: {slot.gameObject.activeInHierarchy}, HasCanvas: {slot.GetComponentInParent<Canvas>() != null}");

            // Check if slot is properly set up
            var graphic = slot.GetComponent<UnityEngine.UI.Graphic>();
            Debug.Log($"[StorageUIManager]   Graphic raycast: {graphic?.raycastTarget}");
            Debug.Log($"[StorageUIManager]   Icon raycast: {slot.icon?.raycastTarget}");
        }

        Debug.Log($"[StorageUIManager] Initialized {storageSlots.Count} storage slots and {playerInventorySlots.Count} player slots");
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
        Debug.Log($"[StorageUIManager] ===== OpenStorage START =====");
        Debug.Log($"[StorageUIManager] Opening storage: {storage?.StorageId}");

        if (storage == null)
        {
            Debug.LogError("[StorageUIManager] Storage is null!");
            return;
        }

        // FIRST: Activate the UI panel before loading data
        if (storageUIPanel != null && !storageUIPanel.activeSelf)
        {
            storageUIPanel.SetActive(true);
            Debug.Log($"[StorageUIManager] Storage UI panel activated");
        }

        // Activate slot containers
        if (storageSlotsContainer != null && !storageSlotsContainer.gameObject.activeSelf)
        {
            storageSlotsContainer.gameObject.SetActive(true);
            Debug.Log($"[StorageUIManager] Storage slots container activated");
        }

        if (playerInventorySlotsContainer != null && !playerInventorySlotsContainer.gameObject.activeSelf)
        {
            playerInventorySlotsContainer.gameObject.SetActive(true);
            Debug.Log($"[StorageUIManager] Player slots container activated");
        }

        // Save previous storage if any
        if (currentStorage != null && currentStorage != storage)
        {
            Debug.Log($"[StorageUIManager] Saving previous storage: {currentStorage.StorageId}");
            SaveCurrentStorageState();
        }

        currentStorage = storage;
        isOpen = true;

        // Load storage data into slots
        LoadStorageData();

        // Load player inventory data into mirror slots
        LoadPlayerInventoryData();

        // Request to open through UIManager
        Events.Publish(new UIRequestOpenEvent(storageUIPanel));
        Debug.Log($"[StorageUIManager] Published UIRequestOpenEvent");

        // Ensure canvas group doesn't block raycasts
     



        // Pause and cursor
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Events.Publish(new StorageOpenedEvent(storage.StorageId));
        Debug.Log($"[StorageUIManager] ===== OpenStorage END =====");
    }
    public void CloseStorage()
    {
        if (!isOpen) return;

        SaveCurrentStorageState();
        SyncPlayerInventory();

        Events.Publish(new UIRequestCloseEvent(storageUIPanel));

        // Hide UI
        storageUIPanel.SetActive(false);

        // Unpause
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Events.Publish(new StorageClosedEvent(currentStorage?.StorageId));

        currentStorage = null;
        isOpen = false;

        Debug.Log("[Storage] Closed");
    }

    private void LoadStorageData()
    {
        if (currentStorage == null) return;

        Debug.Log($"[StorageUIManager] ===== LoadStorageData START =====");
        Debug.Log($"[StorageUIManager] Activating storage slots container...");

        // FIRST: Make sure the container is active
        if (storageSlotsContainer != null && !storageSlotsContainer.gameObject.activeSelf)
        {
            storageSlotsContainer.gameObject.SetActive(true);
            Debug.Log($"[StorageUIManager] Storage slots container activated");
        }

        // Clear all storage slots first
        foreach (var slot in storageSlots)
        {
            // ACTIVATE THE SLOT FIRST
            if (!slot.gameObject.activeSelf)
            {
                Debug.Log($"[StorageUIManager] Activating slot: {slot.gameObject.name}");
                slot.gameObject.SetActive(true);

                // Force a layout rebuild
                LayoutRebuilder.ForceRebuildLayoutImmediate(slot.GetComponent<RectTransform>());
            }

            slot.ClearSlot();
        }

        // Load data from storage container
        for (int i = 0; i < storageSlots.Count && i < currentStorage.SlotCount; i++)
        {
            StorageSlotData data = currentStorage.GetSlotData(i);

            if (data != null && data.itemData != null)
            {
                Debug.Log($"[StorageUIManager] Loading slot {i} with {data.itemData.name} ({data.quantity})");
                storageSlots[i].SetItem(data.itemData, data.quantity, data.itemPrefab);
            }
            else
            {
                Debug.Log($"[StorageUIManager] Slot {i} is empty");
            }

            // Link slot to storage
            storageSlots[i].SetStorageReference(currentStorage, i);

            // Force the slot to be interactable
            var canvasGroup = storageSlots[i].GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log($"[StorageUIManager] Set CanvasGroup for slot {i} - interactable: true, blocksRaycasts: true");
            }

            // Ensure the slot image has raycast target
            var slotImage = storageSlots[i].GetComponent<UnityEngine.UI.Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = true;
                Debug.Log($"[StorageUIManager] Slot {i} image raycast target: true");
            }
        }

        Debug.Log($"[StorageUIManager] ===== LoadStorageData END =====");
    }

    private void LoadPlayerInventoryData()
    {
        Debug.Log($"[StorageUIManager] ===== LoadPlayerInventoryData START =====");

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[StorageUIManager] InventorySystem.Instance is null!");
            return;
        }

        var realPlayerSlots = InventorySystem.Instance.normalInventorySlots;
        Debug.Log($"[StorageUIManager] Real player slots count: {realPlayerSlots?.Count}");

        // First, ensure all mirror slots are active and enabled
        for (int i = 0; i < playerInventorySlots.Count; i++)
        {
            var mirrorSlot = playerInventorySlots[i];

            Debug.Log($"[StorageUIManager] Processing mirror slot {i}: {mirrorSlot.gameObject.name}");

            // CRITICAL FIX: Make sure the GameObject is ACTIVE in hierarchy
            if (!mirrorSlot.gameObject.activeInHierarchy)
            {
                Debug.Log($"[StorageUIManager]   GameObject was inactive, activating now");
                mirrorSlot.gameObject.SetActive(true);

                // After activating, we need to ensure the RectTransform is properly set up
                var rectTransform = mirrorSlot.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            // Enable component
            mirrorSlot.enabled = true;

            // Initialize for storage UI
            mirrorSlot.InitializeForStorageUI();

            // Clear slot
            mirrorSlot.ClearSlot();

            Debug.Log($"[StorageUIManager]   Slot enabled: {mirrorSlot.enabled}, ActiveInHierarchy: {mirrorSlot.gameObject.activeInHierarchy}");
        }

        // Load data from real slots
        for (int i = 0; i < playerInventorySlots.Count && i < realPlayerSlots.Count; i++)
        {
            var realSlot = realPlayerSlots[i];
            var mirrorSlot = playerInventorySlots[i];

            Debug.Log($"[StorageUIManager] Loading slot {i}: Real has {realSlot.itemData?.itemName} ({realSlot.quantity}), Mirror was {mirrorSlot.itemData?.itemName}");

            if (realSlot.itemData != null)
            {
                mirrorSlot.SetItemPublic(realSlot.itemData, realSlot.quantity, realSlot.instantiatedPrefab);
                Debug.Log($"[StorageUIManager]   Set mirror slot to: {mirrorSlot.itemData?.itemName} ({mirrorSlot.quantity})");
            }

            mirrorSlot.slotPriority = realSlot.slotPriority;

            // Force graphic raycast targets - IMPORTANT for drag detection
            var graphic = mirrorSlot.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                Debug.Log($"[StorageUIManager]   Graphic raycast set to true");
            }

            if (mirrorSlot.icon != null)
            {
                mirrorSlot.icon.raycastTarget = true;
                Debug.Log($"[StorageUIManager]   Icon raycast set to true");
            }

            // Ensure the slot's parent is also active
            if (mirrorSlot.transform.parent != null && !mirrorSlot.transform.parent.gameObject.activeSelf)
            {
                Debug.Log($"[StorageUIManager]   Activating parent: {mirrorSlot.transform.parent.name}");
                mirrorSlot.transform.parent.gameObject.SetActive(true);
            }
        }

        Debug.Log($"[StorageUIManager] ===== LoadPlayerInventoryData END =====");
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

        Debug.Log($"[Storage] Saved state for: {currentStorage.StorageId}");
    }

    private void SyncPlayerInventory()
    {
        if (InventorySystem.Instance == null) return;

        var realPlayerSlots = InventorySystem.Instance.normalInventorySlots;

        for (int i = 0; i < playerInventorySlots.Count && i < realPlayerSlots.Count; i++)
        {
            if (playerInventorySlots[i] != null)
            {
                var mirrorSlot = playerInventorySlots[i];
                var realSlot = realPlayerSlots[i];

                // Sync data back to real inventory
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

        Debug.Log("[Storage] Synced player inventory");
    }

    private void CleanupStorageUIOnly()
    {
        Events.Publish(new StorageClosedEvent(currentStorage?.StorageId));
        currentStorage = null;
        isOpen = false;
    }

    public StorageContainer GetCurrentStorage() => currentStorage;
    public bool IsOpen => isOpen;
    public List<InventorySlotsUI> GetPlayerMirrorSlots() => playerInventorySlots;
}


