using UnityEngine;
using Doody.GameEvents;

public class FocusOnObject
{
    public Transform TargetObject { get; set; }
    public FocusOnObject(Transform obj)
    {
        TargetObject = obj;
    }
}

public class UnFocusObject
{
    public UnFocusObject() { }
}

public class PlrCameraZoomIn : EventListener
{
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float zoomedFOV = 40f; // Zoomed-in Field of View
    [SerializeField] private float normalFOV = 60f; // Normal Field of View
    [SerializeField] private float zoomSpeed = 5f; // Smoothness of zoom

    [Header("Target Settings")]
    [SerializeField] private Transform targetObject; // Object to look at
    [SerializeField] private Vector3 targetOffset = Vector3.zero; // Offset from target

    private bool isZoomed = false;
    private float currentFOV;
    private Quaternion originalRotation;
    private bool hasSavedOriginalState = false;

    // Start is called before the first frame update
    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = GetComponent<Camera>();
            }
        }

        if (playerCamera != null)
        {
            currentFOV = playerCamera.fieldOfView;
        }

        Events.Subscribe<FocusOnObject>(FocusOnTarget, this);
        Events.Subscribe<UnFocusObject>(UnFocusOnTarget, this);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCameraZoom();
    }

    void FocusOnTarget(FocusOnObject data)
    {
        SaveOriginalState();
        targetObject = data.TargetObject;
        isZoomed = true;
    }

    void UnFocusOnTarget(UnFocusObject data)
    {
        isZoomed = false;
        targetObject = null;
    }

    void SaveOriginalState()
    {
        if (playerCamera != null)
        {
            originalRotation = playerCamera.transform.rotation;
            hasSavedOriginalState = true;
        }
    }

    void UpdateCameraZoom()
    {
        if (playerCamera == null) return;

        // Smoothly interpolate FOV
        float targetFOV = isZoomed ? zoomedFOV : normalFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
        playerCamera.fieldOfView = currentFOV;

        // Handle looking at target when zoomed
        if (isZoomed && targetObject != null)
        {
            LookAtTargetSmoothly();
        }
        else if (!isZoomed && hasSavedOriginalState)
        {
            ReturnToOriginalRotation();
        }
    }

    void LookAtTargetSmoothly()
    {
        // Calculate target position with offset
        Vector3 targetPosition = targetObject.position + targetOffset;

        // Calculate direction to target
        Vector3 direction = targetPosition - playerCamera.transform.position;

        // Create target rotation looking at the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly interpolate rotation only (no position change)
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * zoomSpeed
        );
    }

    void ReturnToOriginalRotation()
    {
        // Smoothly return to original rotation only (no position change)
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            originalRotation,
            Time.deltaTime * zoomSpeed
        );
    }

    public bool IsZoomed()
    {
        return isZoomed;
    }

    // Set target object at runtime
    public void SetTargetObject(Transform newTarget)
    {
        targetObject = newTarget;
    }

    // Set zoom FOV at runtime
    public void SetZoomFOV(float newZoomedFOV)
    {
        zoomedFOV = newZoomedFOV;
    }

    public void ManualFocus(Transform target)
    {
        SaveOriginalState();
        targetObject = target;
        isZoomed = true;
    }

    public void ManualUnfocus()
    {
        isZoomed = false;
        targetObject = null;
    }

  
}