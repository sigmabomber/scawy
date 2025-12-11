using System.Collections;
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

    private Camera playerCamera;
    private GameObject currentHighlightedObject;
    private Outline currentOutline; 
    private IInteractable currentInteractable;
    private Color originalTextColor;
    private bool isShowingFeedback = false;

    private RaycastHit cachedHit;

    public static InteractionSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerCamera = Camera.main;

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

        Debug.DrawRay(checkRay.origin, checkRay.direction * interactionRange, Color.green);

        // **FIX: Cast ray on ALL layers first to check for obstacles**
        if (Physics.Raycast(checkRay, out RaycastHit firstHit, interactionRange))
        {
            // Check if the first thing we hit is on the interactable layer
            if (((1 << firstHit.collider.gameObject.layer) & interactableLayer) == 0)
            {
                // Hit something that's NOT interactable - there's an obstacle
                if (currentInteractable != null)
                {
                    currentInteractable = null;
                    HideUI();
                    RemoveOutline();
                }
                return;
            }

            // The first hit IS an interactable object, proceed normally
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
                        RemoveOutline();
                        AddOutline(cachedHit.collider.gameObject);
                    }
                }
                return;
            }
        }

        // Clear state if nothing valid is being looked at
        if (currentInteractable != null)
        {
            currentInteractable = null;
            HideUI();
            RemoveOutline();
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

    private void AddOutline(GameObject obj)
    {
        currentHighlightedObject = obj;

        // Try to get existing outline first (optimization)
        currentOutline = obj.GetComponent<Outline>();

        if (currentOutline == null)
        {
            currentOutline = obj.AddComponent<Outline>();

            // Configure outline settings once when creating
            currentOutline.OutlineMode = Outline.Mode.OutlineAll;
            currentOutline.OutlineColor = outlineColor;
            currentOutline.OutlineWidth = outlineWidth;
        }
        else
        {
            // Update settings in case they changed
            currentOutline.OutlineMode = Outline.Mode.OutlineAll;
            currentOutline.OutlineColor = outlineColor;
            currentOutline.OutlineWidth = outlineWidth;
        }

        currentOutline.enabled = true;
    }

    private void RemoveOutline()
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
        RemoveOutline();
        HideUI();
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
                RemoveOutline();
            }
        }
    }
}