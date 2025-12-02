using System.Collections;
using TMPro;
using Unity.VisualScripting;
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
    private IInteractable currentInteractable;
    private Color originalTextColor;
    private bool isShowingFeedback = false;


    public static InteractionSystem Instance;

    private void Start()
    {
        playerCamera = Camera.main;
        Instance = this;
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
        // If showing feedback, don't check for new interactables yet
        if (isShowingFeedback)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            // If no longer looking at an item, end feedback early
            if (!Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
            {
                StopAllCoroutines();
                isShowingFeedback = false;
                interactionText.color = originalTextColor;
            }
            return;
        }

        Ray checkRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit checkHit;

        // Debug raycast
        Debug.DrawRay(checkRay.origin, checkRay.direction * interactionRange, Color.green);

        if (Physics.Raycast(checkRay, out checkHit, interactionRange, interactableLayer))
        {

            IInteractable interactable = checkHit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                Debug.LogWarning($"Object {checkHit.collider.name} has no IInteractable component!");
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
                }
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    UpdateUI(interactable.GetInteractionPrompt());

                    if (currentHighlightedObject != checkHit.collider.gameObject)
                    {
                        RemoveOutline();
                        AddOutline(checkHit.collider.gameObject);
                    }
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;
            HideUI();
            RemoveOutline();
        }
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

        Outline outline = obj.GetComponent<Outline>();

        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }

        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = true;
    }

    private void RemoveOutline()
    {
        if (currentHighlightedObject != null)
        {
            Outline outline = currentHighlightedObject.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled = false;
            }

            currentHighlightedObject = null;
        }
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

        Color originalColor = interactionText.color;
        interactionText.text = message;
        interactionText.color = textColor;

        yield return new WaitForSeconds(1.5f);

        isShowingFeedback = false;

        interactionText.color = originalColor;

        // Check if still looking at an interactable
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                interactionText.text = interactable.GetInteractionPrompt();
            }
        }
    }
}