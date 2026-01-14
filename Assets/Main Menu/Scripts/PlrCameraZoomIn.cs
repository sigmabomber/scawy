using UnityEngine;
using Doody.GameEvents;

public class FocusOnObject
{
    public Transform TargetObject { get; set; }
    public FocusOnObject(Transform obj) { TargetObject = obj; }
}

public class UnFocusObject
{
    public UnFocusObject() { }
}

public class PlrCameraZoomIn : EventListener
{
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float zoomedFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Target Settings")]
    [SerializeField] private Transform targetObject;
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    private bool isZoomed = false;
    private float currentFOV;

    private bool returningToOriginal = false;
    private Quaternion originalRotation; // Store the rotation when we START zooming
    private bool hasStoredOriginalRotation = false;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main ?? GetComponent<Camera>();
        }

        currentFOV = playerCamera != null ? playerCamera.fieldOfView : 60f;

        Events.Subscribe<FocusOnObject>(FocusOnTarget, this);
        Events.Subscribe<UnFocusObject>(UnFocusOnTarget, this);
    }

    // Changed to LateUpdate to run after other camera controllers
    void LateUpdate()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (playerCamera == null) return;

        // Smooth FOV interpolation
        float targetFOV = isZoomed ? zoomedFOV : normalFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
        playerCamera.fieldOfView = currentFOV;

        // Handle rotation
        if (isZoomed && targetObject != null)
        {
            // Store original rotation when we first start zooming
            if (!hasStoredOriginalRotation)
            {
                originalRotation = playerCamera.transform.rotation;
                hasStoredOriginalRotation = true;
            }
            returningToOriginal = false;
            LookAtTargetSmoothly();
        }
        else if (!isZoomed && returningToOriginal)
        {
            ReturnToOriginalRotation();
        }
    }

    void FocusOnTarget(FocusOnObject data)
    {
        targetObject = data.TargetObject;
        isZoomed = true;
        hasStoredOriginalRotation = false; // Reset so we capture new original rotation
    }

    void UnFocusOnTarget(UnFocusObject data)
    {
        isZoomed = false;
        targetObject = null;
        returningToOriginal = true;
        // originalRotation is already stored from when we zoomed in
    }

    void LookAtTargetSmoothly()
    {
        Vector3 targetPos = targetObject.position + targetOffset;
        Vector3 direction = targetPos - playerCamera.transform.position;

        // Check if direction is valid
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smooth rotation
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    void ReturnToOriginalRotation()
    {
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            originalRotation,
            Time.deltaTime * rotationSpeed
        );

        // Stop interpolating when very close
        if (Quaternion.Angle(playerCamera.transform.rotation, originalRotation) < 0.1f)
        {
            playerCamera.transform.rotation = originalRotation;
            returningToOriginal = false;
            hasStoredOriginalRotation = false; // Reset for next zoom
        }
    }

    // Optional runtime methods
    public void ManualFocus(Transform target)
    {
        targetObject = target;
        isZoomed = true;
        hasStoredOriginalRotation = false;
    }

    public void ManualUnfocus()
    {
        isZoomed = false;
        targetObject = null;
        returningToOriginal = true;
    }

    public bool IsZoomed() => isZoomed;

    public void SetTargetObject(Transform newTarget) => targetObject = newTarget;

    public void SetZoomFOV(float newZoomedFOV) => zoomedFOV = newZoomedFOV;
}