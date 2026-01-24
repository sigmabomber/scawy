using Doody.Settings;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionSystem : InputScript
{
    [Header("Settings")]

    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Gamepad Settings")]
    [SerializeField] private bool allowGamepadInteraction = true;

    [Header("UI References")]
    [SerializeField] private Image reticleUI;
    [SerializeField] private TMP_Text interactionText;

    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    [Header("Optimization")]
    [SerializeField] private int raycastsPerSecond = 30;
    [SerializeField] private int uiUpdatePerSecond = 10; 



    private Camera playerCamera;
    private GameObject currentHighlightedObject;
    private Outline currentOutline;
    private IInteractable currentInteractable;
    private Color originalTextColor;
    private bool isShowingFeedback = false;

    private RaycastHit cachedHit;
    private int interactableLayerValue;

    private Dictionary<GameObject, IInteractable> interactableCache = new();
    private Dictionary<GameObject, Outline> outlineCache = new();
    private Dictionary<GameObject, ItemStateTracker> stateTrackerCache = new();

    private float raycastInterval;
    private float uiUpdateInterval;
    private float lastRaycastTime;
    private float lastUIUpdateTime;
    private bool hasValidTarget;
    private bool isCurrentInteractableValid; 

    private Ray reusableRay;
    private readonly Vector3 viewportCenter = new(0.5f, 0.5f, 0);

    // Cache for dynamic updates
    private string cachedPrompt = "";
    private Sprite cachedIcon = null;
    private float lastInteractableCheckTime = 0f;
    private float interactableCheckInterval = 0.2f; 

    public static InteractionSystem Instance;

    private void Awake()
    {
        Instance = this;
        raycastInterval = 1f / raycastsPerSecond;
        uiUpdateInterval = 1f / uiUpdatePerSecond;
    }

    private void Start()
    {
        playerCamera = Camera.main;
        interactableLayerValue = interactableLayer.value;

        if (interactionText != null)
            originalTextColor = interactionText.color;

        if (reticleUI != null)
        {
            reticleUI.gameObject.SetActive(false);
            interactionText.gameObject.SetActive(false);
        }
    }

    protected override void HandleInput()
    {
        // Throttle raycasts
        if (Time.time - lastRaycastTime >= raycastInterval)
        {
            lastRaycastTime = Time.time;
            CheckForInteractable();
        }

        // Update UI more frequently than raycasts
        if (Time.time - lastUIUpdateTime >= uiUpdateInterval)
        {
            lastUIUpdateTime = Time.time;
            UpdateCurrentInteractableUI();
        }

        if (currentInteractable == null)
            return;

        bool keyboardInteract = InputWrapper.Instance.Interact;
        bool gamepadInteract = false;

        if (allowGamepadInteraction)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepadInteract = gamepad.aButton.wasPressedThisFrame;
            }
        }

        if (keyboardInteract || gamepadInteract)
        {
            currentInteractable.Interact();

            // Force update after interaction
            ForceUpdateCurrentInteractable();
        }
    }

    private void CheckForInteractable()
    {
        if (isShowingFeedback)
        {
            reusableRay = playerCamera.ViewportPointToRay(viewportCenter);

            if (!Physics.Raycast(reusableRay, out cachedHit, interactionRange, interactableLayer))
            {
                StopAllCoroutines();
                isShowingFeedback = false;
                interactionText.color = originalTextColor;
            }
            return;
        }

        reusableRay = playerCamera.ViewportPointToRay(viewportCenter);

        if (Physics.Raycast(reusableRay, out RaycastHit hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if we need to revalidate current interactable
            if (currentInteractable != null && Time.time - lastInteractableCheckTime >= interactableCheckInterval)
            {
                lastInteractableCheckTime = Time.time;
                isCurrentInteractableValid = currentInteractable.CanInteract();

                if (!isCurrentInteractableValid)
                {
                    // Current interactable became invalid
                    ClearCurrentInteractable();
                    return;
                }
            }

            if (currentHighlightedObject == hitObject && currentInteractable != null && isCurrentInteractableValid)
            {
                hasValidTarget = true;
                return;
            }

            cachedHit = hit;
            IInteractable interactable = GetCachedInteractable(hitObject);

            if (interactable != null && interactable.CanInteract())
            {
                if (currentInteractable != interactable)
                {
                    SetCurrentInteractable(interactable, hitObject);
                }
                else
                {
                    // Same interactable, but might have changed state
                    UpdateUIForInteractable(interactable);
                }

                hasValidTarget = true;
                return;
            }
        }

        // No valid interactable in raycast
        if (hasValidTarget || currentInteractable != null)
        {
            ClearCurrentInteractable();
            hasValidTarget = false;
        }
    }

    private void SetCurrentInteractable(IInteractable interactable, GameObject hitObject)
    {
        currentInteractable = interactable;
        isCurrentInteractableValid = true;
        lastInteractableCheckTime = Time.time;

        UpdateUIForInteractable(interactable);

        if (currentHighlightedObject != hitObject)
        {
            DisableCurrentOutline();
            EnableOutline(hitObject);
        }
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        isCurrentInteractableValid = false;
        currentHighlightedObject = null;
        HideUI();
        DisableCurrentOutline();
    }

    private void UpdateCurrentInteractableUI()
    {
        if (currentInteractable == null || isShowingFeedback) return;

        // Update UI even if we're still looking at the same object
        UpdateUIForInteractable(currentInteractable);
    }

    private void UpdateUIForInteractable(IInteractable interactable)
    {
        if (interactable == null) return;

        string newPrompt = interactable.GetInteractionPrompt();
        Sprite newIcon = interactable.GetInteractionIcon();

        // Only update if something changed
        if (newPrompt != cachedPrompt || newIcon != cachedIcon)
        {
            cachedPrompt = newPrompt;
            cachedIcon = newIcon;
            UpdateUI(newPrompt, newIcon);
        }
    }

    private IInteractable GetCachedInteractable(GameObject obj)
    {
        if (interactableCache.TryGetValue(obj, out var cached))
        {
            // Check if the cached interactable is still valid (object might have been destroyed)
            if (cached == null || (cached as MonoBehaviour) == null)
            {
                interactableCache.Remove(obj);
                return null;
            }
            return cached;
        }

        IInteractable interactable = obj.GetComponent<IInteractable>() ??
                                     obj.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            ItemStateTracker tracker = GetCachedStateTracker(obj);
            if (tracker != null && tracker.IsInWorld)
            {
                ItemPickupInteractable pickup = obj.GetComponent<ItemPickupInteractable>();
                if (pickup == null)
                {
                    ItemData data = GetItemDataFromObject(obj);
                    if (data != null)
                    {
                        pickup = obj.AddComponent<ItemPickupInteractable>();
                        pickup.itemData = data;
                        pickup.quantity = 1;
                    }
                }
                interactable = pickup;
            }
        }

        if (interactable != null)
            interactableCache[obj] = interactable;

        return interactable;
    }

    private ItemStateTracker GetCachedStateTracker(GameObject obj)
    {
        if (stateTrackerCache.TryGetValue(obj, out var tracker))
        {
            // Check if tracker is still valid
            if (tracker == null)
            {
                stateTrackerCache.Remove(obj);
                return null;
            }
            return tracker;
        }

        tracker = obj.GetComponent<ItemStateTracker>();
        if (tracker != null)
            stateTrackerCache[obj] = tracker;

        return tracker;
    }

    private ItemData GetItemDataFromObject(GameObject obj)
    {
        var comp = obj.GetComponent<ItemDataComponent>();
        return comp != null ? comp.itemData : null;
    }

    private void UpdateUI(string prompt, Sprite icon)
    {
        if (reticleUI == null) return;

        reticleUI.gameObject.SetActive(icon != null);
        if (icon != null)
            reticleUI.sprite = icon;

        interactionText.text = InputDetector.Instance.IsUsingController() ? $"{prompt} \n[<sprite name=xbox_a>]" : $"{prompt}\n [{SettingsManager.Instance.GetKeyBinding("Interact").ToString()}]";
        interactionText.gameObject.SetActive(true);
    }

    private void HideUI()
    {
        if (reticleUI == null) return;

        reticleUI.gameObject.SetActive(false);
        interactionText.gameObject.SetActive(false);

        // Clear cache when hiding UI
        cachedPrompt = "";
        cachedIcon = null;
    }

    private void EnableOutline(GameObject obj)
    {
        currentHighlightedObject = obj;

        if (!outlineCache.TryGetValue(obj, out currentOutline))
        {
            currentOutline = obj.GetComponent<Outline>() ?? obj.AddComponent<Outline>();
            currentOutline.OutlineMode = Outline.Mode.OutlineVisible;
            currentOutline.OutlineColor = outlineColor;
            currentOutline.OutlineWidth = outlineWidth;

            outlineCache[obj] = currentOutline;
        }

        currentOutline.enabled = true;
    }

    private void DisableCurrentOutline()
    {
        if (currentOutline != null)
            currentOutline.enabled = false;

        currentOutline = null;
        currentHighlightedObject = null;
    }

    public void ForceUpdateCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            // Force re-check the interactable
            isCurrentInteractableValid = currentInteractable.CanInteract();
            if (!isCurrentInteractableValid)
            {
                ClearCurrentInteractable();
            }
            else
            {
                // Update UI immediately
                UpdateUIForInteractable(currentInteractable);
            }
        }

        // Also clear cache to force fresh lookups
        ClearStaleCacheEntries();
    }

    private void ClearStaleCacheEntries()
    {
        // Remove null entries from caches
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in interactableCache)
        {
            if (kvp.Value == null || (kvp.Value as MonoBehaviour) == null)
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
            interactableCache.Remove(key);

        toRemove.Clear();

        foreach (var kvp in stateTrackerCache)
        {
            if (kvp.Value == null)
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
            stateTrackerCache.Remove(key);
    }

    private void OnDisable()
    {
        DisableCurrentOutline();
        HideUI();
    }

    private void OnDestroy()
    {
        interactableCache.Clear();
        outlineCache.Clear();
        stateTrackerCache.Clear();
    }

    public void ShowFeedback(string message, Color color)
    {
        if (interactionText != null)
            StartCoroutine(ShowFeedbackCoroutine(message, color));
    }

    private IEnumerator ShowFeedbackCoroutine(string message, Color color)
    {
        isShowingFeedback = true;
        interactionText.text = message;
        interactionText.color = color;

        yield return new WaitForSeconds(1.5f);

        isShowingFeedback = false;
        interactionText.color = originalTextColor;

        // Force update after feedback ends
        ForceUpdateCurrentInteractable();
    }
}