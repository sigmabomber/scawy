using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
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

    // Outline caching - reuse outlines instead of constantly adding/removing
    private Dictionary<GameObject, Outline> outlineCache = new Dictionary<GameObject, Outline>();

    public static InteractionSystem Instance;

    private void Awake()
    {
        Instance = this;
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

    private void Update()
    {
        CheckForInteractable();

        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void CheckForInteractable()
    {
        if (isShowingFeedback)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (!Physics.Raycast(ray, out cachedHit, interactionRange, interactableLayer))
            {
                StopAllCoroutines();
                isShowingFeedback = false;
                interactionText.color = originalTextColor;
            }
            return;
        }

        Ray checkRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

#if UNITY_EDITOR
        if (showDebugRay)
            Debug.DrawRay(checkRay.origin, checkRay.direction * interactionRange, Color.green);
#endif

        if (Physics.Raycast(checkRay, out RaycastHit firstHit, interactionRange))
        {
            if (((1 << firstHit.collider.gameObject.layer) & interactableLayerValue) == 0)
            {
                if (currentInteractable != null)
                {
                    currentInteractable = null;
                    HideUI();
                    DisableCurrentOutline();
                }
                return;
            }

            cachedHit = firstHit;

            IInteractable interactable = cachedHit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                interactable = cachedHit.collider.GetComponentInParent<IInteractable>();

                if (interactable == null)
                {
                    GameObject hitObject = cachedHit.collider.gameObject;

                    ItemStateTracker stateTracker = hitObject.GetComponent<ItemStateTracker>();
                    if (stateTracker != null && stateTracker.IsInWorld)
                    {
                        ItemPickupInteractable pickup = hitObject.GetComponent<ItemPickupInteractable>();
                        if (pickup == null)
                        {
                            ItemData itemData = GetItemDataFromObject(hitObject);
                            if (itemData != null)
                            {
                                pickup = hitObject.AddComponent<ItemPickupInteractable>();
                                pickup.itemData = itemData;
                                pickup.quantity = 1;
                                interactable = pickup;
                            }
                        }
                        else
                        {
                            interactable = pickup;
                        }
                    }
                }

                if (interactable == null)
                {
                    Debug.LogWarning($"Object {cachedHit.collider.name} has no IInteractable component and cannot be made interactable!");
                }
            }

            if (interactable != null && interactable.CanInteract())
            {
                Sprite interactIcon = interactable.GetInteractionIcon();

                if (interactIcon == null)
                {
                    reticleUI.gameObject.SetActive(false);
                }
                else
                {
                    reticleUI.sprite = interactIcon;

                    reticleUI.gameObject.SetActive(true);
                }

                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    UpdateUI(interactable.GetInteractionPrompt());

                    if (currentHighlightedObject != cachedHit.collider.gameObject)
                    {
                        DisableCurrentOutline();
                        EnableOutline(cachedHit.collider.gameObject);
                    }
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;
            HideUI();
            DisableCurrentOutline();
        }
    }

    private ItemData GetItemDataFromObject(GameObject obj)
    {
        ItemDataComponent dataComp = obj.GetComponent<ItemDataComponent>();
        if (dataComp != null)
            return dataComp.itemData;

        return null;
    }

    private void UpdateUI(string prompt)
    {
        if (reticleUI != null)
        {
            reticleUI.gameObject.SetActive(true);
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

        // Check if we already have an outline cached for this object
        if (!outlineCache.TryGetValue(obj, out currentOutline))
        {
            // Try to get existing outline component
            currentOutline = obj.GetComponent<Outline>();

            // If no outline exists, create one
            if (currentOutline == null)
            {
                currentOutline = obj.AddComponent<Outline>();
            }

            // Cache it for future use
            outlineCache[obj] = currentOutline;
        }

        // Configure outline settings (only if needed)
        if (currentOutline.OutlineMode != Outline.Mode.OutlineAll ||
            currentOutline.OutlineColor != outlineColor ||
            currentOutline.OutlineWidth != outlineWidth)
        {
            currentOutline.OutlineMode = Outline.Mode.OutlineAll;
            currentOutline.OutlineColor = outlineColor;
            currentOutline.OutlineWidth = outlineWidth;
        }

        // Enable the outline
        if (!currentOutline.enabled)
        {
            currentOutline.enabled = true;
        }
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
        // Clean up cache
        outlineCache.Clear();
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

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out cachedHit, interactionRange, interactableLayer))
        {
            IInteractable interactable = cachedHit.collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                interactionText.text = interactable.GetInteractionPrompt();
            }
        }
    }

    public bool IsObjectInteractable(GameObject obj)
    {
        if (obj == null) return false;

        IInteractable interactable = obj.GetComponent<IInteractable>();
        if (interactable == null)
        {
            interactable = obj.GetComponentInParent<IInteractable>();
        }

        if (interactable != null)
        {
            return interactable.CanInteract();
        }

        return false;
    }

    public void RefreshCurrentInteractable()
    {
        if (currentHighlightedObject != null)
        {
            IInteractable interactable = currentHighlightedObject.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                currentInteractable = interactable;
                UpdateUI(interactable.GetInteractionPrompt());
            }
            else
            {
                currentInteractable = null;
                HideUI();
                DisableCurrentOutline();
            }
        }
    }

    // Optional: Call this periodically to clean up destroyed objects from cache
    public void CleanupOutlineCache()
    {
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in outlineCache)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            outlineCache.Remove(key);
        }
    }
}