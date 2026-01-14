using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : InputScript
{
    [Header("Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI References")]
    [SerializeField] private Image reticleUI;
    [SerializeField] private TMP_Text interactionText;

    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    [Header("Optimization")]
    [SerializeField] private int raycastsPerSecond = 30; // Reduce from 60+ to 30

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = false;

    private Camera playerCamera;
    private GameObject currentHighlightedObject;
    private Outline currentOutline;
    private IInteractable currentInteractable;
    private Color originalTextColor;
    private bool isShowingFeedback = false;

    private RaycastHit cachedHit;
    private int interactableLayerValue;

    // Component caching - avoid repeated GetComponent calls
    private Dictionary<GameObject, IInteractable> interactableCache = new Dictionary<GameObject, IInteractable>();
    private Dictionary<GameObject, Outline> outlineCache = new Dictionary<GameObject, Outline>();
    private Dictionary<GameObject, ItemStateTracker> stateTrackerCache = new Dictionary<GameObject, ItemStateTracker>();

    // Raycast throttling
    private float raycastInterval;
    private float lastRaycastTime;
    private bool hasValidTarget;

    // Object pools for reduced allocations
    private Ray reusableRay;
    private Vector3 viewportCenter = new Vector3(0.5f, 0.5f, 0);

    public static InteractionSystem Instance;

    private void Awake()
    {
        Instance = this;
        raycastInterval = 1f / raycastsPerSecond;
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

        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
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

#if UNITY_EDITOR
        if (showDebugRay)
            Debug.DrawRay(reusableRay.origin, reusableRay.direction * interactionRange, Color.green);
#endif

        if (Physics.Raycast(reusableRay, out RaycastHit firstHit, interactionRange, interactableLayer))
        {
            GameObject hitObject = firstHit.collider.gameObject;

            // Early out if we're still looking at the same object
            if (currentHighlightedObject == hitObject && currentInteractable != null)
            {
                hasValidTarget = true;
                return;
            }

            cachedHit = firstHit;

            // Use cached interactable lookup
            IInteractable interactable = GetCachedInteractable(hitObject);

            if (interactable != null && interactable.CanInteract())
            {
                // Only update if object changed
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;

                    // Batch UI updates
                    Sprite interactIcon = interactable.GetInteractionIcon();
                    string prompt = interactable.GetInteractionPrompt();

                    UpdateUI(prompt, interactIcon);

                    if (currentHighlightedObject != hitObject)
                    {
                        DisableCurrentOutline();
                        EnableOutline(hitObject);
                    }
                }
                hasValidTarget = true;
                return;
            }
        }

        // Clear current target if nothing valid found
        if (hasValidTarget || currentInteractable != null)
        {
            currentInteractable = null;
            HideUI();
            DisableCurrentOutline();
            hasValidTarget = false;
        }
    }

    private IInteractable GetCachedInteractable(GameObject obj)
    {
        // Check cache first
        if (interactableCache.TryGetValue(obj, out IInteractable cached))
        {
            return cached;
        }

        // Find interactable
        IInteractable interactable = obj.GetComponent<IInteractable>();

        if (interactable == null)
        {
            interactable = obj.GetComponentInParent<IInteractable>();
        }

        // Handle ItemStateTracker case
        if (interactable == null)
        {
            ItemStateTracker stateTracker = GetCachedStateTracker(obj);

            if (stateTracker != null && stateTracker.IsInWorld)
            {
                ItemPickupInteractable pickup = obj.GetComponent<ItemPickupInteractable>();

                if (pickup == null)
                {
                    ItemData itemData = GetItemDataFromObject(obj);
                    if (itemData != null)
                    {
                        pickup = obj.AddComponent<ItemPickupInteractable>();
                        pickup.itemData = itemData;
                        pickup.quantity = 1;
                    }
                }

                interactable = pickup;
            }
        }

        // Cache the result (even if null to avoid repeated lookups)
        if (interactable != null)
        {
            interactableCache[obj] = interactable;
        }

        return interactable;
    }

    private ItemStateTracker GetCachedStateTracker(GameObject obj)
    {
        if (stateTrackerCache.TryGetValue(obj, out ItemStateTracker tracker))
        {
            return tracker;
        }

        tracker = obj.GetComponent<ItemStateTracker>();
        if (tracker != null)
        {
            stateTrackerCache[obj] = tracker;
        }

        return tracker;
    }

    private ItemData GetItemDataFromObject(GameObject obj)
    {
        ItemDataComponent dataComp = obj.GetComponent<ItemDataComponent>();
        return dataComp != null ? dataComp.itemData : null;
    }

    private void UpdateUI(string prompt, Sprite icon = null)
    {
        if (reticleUI != null)
        {
            if (icon == null)
            {
                reticleUI.gameObject.SetActive(false);
            }
            else
            {
                reticleUI.sprite = icon;
                reticleUI.gameObject.SetActive(true);
            }

            interactionText.text = prompt;
            interactionText.gameObject.SetActive(true);
        }
    }

    private void HideUI()
    {
        if (reticleUI != null)
        {
            reticleUI.gameObject.SetActive(false);
            interactionText.gameObject.SetActive(false);
        }
    }

    private void EnableOutline(GameObject obj)
    {
        currentHighlightedObject = obj;

        // Check cache first
        if (!outlineCache.TryGetValue(obj, out currentOutline))
        {
            currentOutline = obj.GetComponent<Outline>();

            if (currentOutline == null)
            {
                currentOutline = obj.AddComponent<Outline>();

                // Set properties once when creating
                currentOutline.OutlineMode = Outline.Mode.OutlineAll;
                currentOutline.OutlineColor = outlineColor;
                currentOutline.OutlineWidth = outlineWidth;
            }

            outlineCache[obj] = currentOutline;
        }

        // Just enable if already configured
        currentOutline.enabled = true;
    }

    private void DisableCurrentOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }

        currentHighlightedObject = null;
    }

    private void OnDisable()
    {
        DisableCurrentOutline();
        HideUI();
    }

    private void OnDestroy()
    {
        // Clean up all caches
        interactableCache.Clear();
        outlineCache.Clear();
        stateTrackerCache.Clear();
    }

    public void ShowFeedback(string message, Color textColor)
    {
        if (interactionText != null)
        {
            StartCoroutine(ShowFeedbackCoroutine(message, textColor));
        }
    }

    private IEnumerator ShowFeedbackCoroutine(string message, Color textColor)
    {
        isShowingFeedback = true;

        interactionText.text = message;
        interactionText.color = textColor;

        yield return new WaitForSeconds(1.5f);

        isShowingFeedback = false;
        interactionText.color = originalTextColor;

        reusableRay = playerCamera.ViewportPointToRay(viewportCenter);

        if (Physics.Raycast(reusableRay, out cachedHit, interactionRange, interactableLayer))
        {
            IInteractable interactable = GetCachedInteractable(cachedHit.collider.gameObject);
            if (interactable != null && interactable.CanInteract())
            {
                interactionText.text = interactable.GetInteractionPrompt();
            }
        }
    }

    public bool IsObjectInteractable(GameObject obj)
    {
        if (obj == null) return false;

        IInteractable interactable = GetCachedInteractable(obj);
        return interactable != null && interactable.CanInteract();
    }

    public void RefreshCurrentInteractable()
    {
        if (currentHighlightedObject != null)
        {
            IInteractable interactable = GetCachedInteractable(currentHighlightedObject);
            if (interactable != null && interactable.CanInteract())
            {
                currentInteractable = interactable;
                UpdateUI(interactable.GetInteractionPrompt(), interactable.GetInteractionIcon());
            }
            else
            {
                currentInteractable = null;
                HideUI();
                DisableCurrentOutline();
            }
        }
    }

    // Call this when objects are destroyed or removed from the world
    public void InvalidateCache(GameObject obj)
    {
        interactableCache.Remove(obj);
        stateTrackerCache.Remove(obj);
        outlineCache.Remove(obj);
    }

    // Periodic cleanup of destroyed objects
    public void CleanupCaches()
    {
        CleanupCache(interactableCache);
        CleanupCache(outlineCache);
        CleanupCache(stateTrackerCache);
    }

    private void CleanupCache<T>(Dictionary<GameObject, T> cache)
    {
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in cache)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            cache.Remove(key);
        }
    }
}