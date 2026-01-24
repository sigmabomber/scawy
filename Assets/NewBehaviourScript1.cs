using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RotaryLockController : MonoBehaviour, IInteractable
{
    [Header("Lock Combination")]
    [SerializeField]
    private DirectionalCombination[] correctCombinations = new DirectionalCombination[]
    {
        new DirectionalCombination(7, Direction.Left),
        new DirectionalCombination(18, Direction.Right),
        new DirectionalCombination(32, Direction.Left)
    };

    [Header("Lock Settings")]
    [SerializeField] private float snapAngle = 9f;
    [SerializeField] private Transform dialTransform;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialRotateSound;
    [SerializeField] private AudioClip snapSound;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip unlockSound;

    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Examine Lock";
    [SerializeField] private Sprite interactionIcon;
    [SerializeField] private Transform cameraInteractPosition; // Empty GameObject positioned for lock view
    [SerializeField] private float cameraTransitionDuration = 0.8f;
    [SerializeField] private AnimationCurve cameraTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float crouchTilt = 15f; // Camera tilt angle to simulate crouching

    [Header("Return to Zero")]
    [SerializeField] private float returnSpeed = 180f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rotation Settings")]
    [SerializeField] private float rotationIncrement = 9f;
    [SerializeField] private bool rotateToNextNumber = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private List<DirectionalCombination> enteredCombination = new List<DirectionalCombination>();
    private int currentNumber = 0;
    private float currentRotation = 0f;
    private bool isUnlocked = false;
    private int lastSnappedNumber = 0;
    private bool isReturningToZero = false;
    private float returnStartRotation = 0f;
    private float returnProgress = 0f;
    private float returnDuration = 0f;
    private Direction lastRotationDirection = Direction.None;

    // Interaction state
    private bool isInteracting = false;
    private Transform playerCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;

    [System.Serializable]
    public class DirectionalCombination
    {
        public int number;
        public Direction direction;

        public DirectionalCombination(int number, Direction direction)
        {
            this.number = number;
            this.direction = direction;
        }
    }

    public enum Direction
    {
        None,
        Left,
        Right
    }

    void Start()
    {
        UpdateDialRotation(0f);

        if (cameraInteractPosition == null)
        {
            Debug.LogError("Camera Interact Position not assigned! Please assign an empty GameObject for camera positioning.");
        }
    }

    void Update()
    {
        if (!isInteracting) return;

        if (isUnlocked) return;

        if (isReturningToZero)
        {
            HandleReturnToZero();
        }
        else
        {
            HandleClickRotation();
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            EnterCurrentNumber();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLock();
        }

        // Exit interaction
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            ExitInteraction();
        }
    }

    #region IInteractable Implementation

    public void Interact()
    {
        if (isUnlocked)
        {
            if (enableDebugLogs)
                Debug.Log("Lock is already unlocked!");
            return;
        }

        if (!isInteracting)
        {
            StartInteraction();
        }
    }

    public bool CanInteract()
    {
        return !isInteracting && !isUnlocked;
    }

    public string GetInteractionPrompt()
    {
        if (isUnlocked)
            return "Unlocked";

        return interactionPrompt;
    }

    public Sprite GetInteractionIcon()
    {
        return interactionIcon;
    }

    #endregion

    #region Camera Interaction

    void StartInteraction()
    {
        if (cameraInteractPosition == null)
        {
            Debug.LogError("Cannot start interaction: Camera Interact Position not assigned!");
            return;
        }

        // Find the main camera
        InputScript.InputEnabled = false;
        playerCamera = Camera.main.transform;

        // Store original camera state
        originalCameraPosition = playerCamera.position;
        originalCameraRotation = playerCamera.rotation;
        originalCameraParent = playerCamera.parent;

        isInteracting = true;

        StartCoroutine(TransitionCameraToLock());

        if (enableDebugLogs)
            Debug.Log("Started lock interaction");
    }

    IEnumerator TransitionCameraToLock()
    {
        float elapsed = 0f;
        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        // Calculate target rotation with crouch tilt
        Quaternion targetRotation = cameraInteractPosition.rotation * Quaternion.Euler(crouchTilt, 0f, 0f);

        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = cameraTransitionCurve.Evaluate(elapsed / cameraTransitionDuration);

            playerCamera.position = Vector3.Lerp(startPos, cameraInteractPosition.position, t);
            playerCamera.rotation = Quaternion.Slerp(startRot, targetRotation, t);

            yield return null;
        }

        playerCamera.position = cameraInteractPosition.position;
        playerCamera.rotation = targetRotation;
    }

    void ExitInteraction()
    {
        if (!isInteracting) return;

        StartCoroutine(TransitionCameraBack());

        if (enableDebugLogs)
            Debug.Log("Exited lock interaction");
    }

    IEnumerator TransitionCameraBack()
    {
        float elapsed = 0f;
        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = cameraTransitionCurve.Evaluate(elapsed / cameraTransitionDuration);

            playerCamera.position = Vector3.Lerp(startPos, originalCameraPosition, t);
            playerCamera.rotation = Quaternion.Slerp(startRot, originalCameraRotation, t);

            yield return null;
        }

        playerCamera.position = originalCameraPosition;
        playerCamera.rotation = originalCameraRotation;

        isInteracting = false;
    }

    #endregion

    #region Lock Mechanics

    void HandleReturnToZero()
    {
        if (!isReturningToZero) return;

        returnProgress += Time.deltaTime / returnDuration;
        returnProgress = Mathf.Clamp01(returnProgress);

        float easedProgress = returnCurve.Evaluate(returnProgress);
        float newRotation = Mathf.LerpAngle(returnStartRotation, 0f, easedProgress);

        UpdateDialRotation(newRotation);

        if (returnProgress >= 1f)
        {
            isReturningToZero = false;
            currentRotation = 0f;
            currentNumber = 0;
            SnapToNearestNumber();

            lastRotationDirection = Direction.None;

            if (enableDebugLogs)
                Debug.Log("Return to zero complete");
        }
    }

    void HandleClickRotation()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            RotateClockwise();
            lastRotationDirection = Direction.Right;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            RotateCounterClockwise();
            lastRotationDirection = Direction.Left;
        }
    }

    void RotateClockwise()
    {
        if (rotateToNextNumber)
        {
            currentNumber = (currentNumber + 1) % 40;
            float targetRotation = (40 - currentNumber) % 40 * snapAngle;
            StartRotationAnimation(currentRotation, targetRotation);
        }
        else
        {
            float newRotation = currentRotation + rotationIncrement;
            newRotation = Mathf.Repeat(newRotation, 360f);
            StartRotationAnimation(currentRotation, newRotation);
        }

        PlaySound(dialRotateSound);

        if (enableDebugLogs)
            Debug.Log("Rotated Right (clockwise)");
    }

    void RotateCounterClockwise()
    {
        if (rotateToNextNumber)
        {
            currentNumber = (currentNumber - 1 + 40) % 40;
            float targetRotation = (40 - currentNumber) % 40 * snapAngle;
            StartRotationAnimation(currentRotation, targetRotation);
        }
        else
        {
            float newRotation = currentRotation - rotationIncrement;
            newRotation = Mathf.Repeat(newRotation, 360f);
            StartRotationAnimation(currentRotation, newRotation);
        }

        PlaySound(dialRotateSound);

        if (enableDebugLogs)
            Debug.Log("Rotated Left (counter-clockwise)");
    }

    void StartRotationAnimation(float startRotation, float targetRotation)
    {
        currentRotation = targetRotation;
        UpdateDialRotation(currentRotation);

        int newNumber = GetSnappedNumber(currentRotation);
        if (newNumber != lastSnappedNumber)
        {
            PlaySound(snapSound);
            lastSnappedNumber = newNumber;
        }

        if (enableDebugLogs)
            Debug.Log($"Rotated to: {currentNumber}, Rotation: {currentRotation}°");
    }

    void UpdateDialRotation(float rotation)
    {
        dialTransform.localRotation = Quaternion.Euler(0, rotation, 0);

        int newNumber = GetSnappedNumber(rotation);

        if (newNumber != currentNumber)
        {
            currentNumber = newNumber;
        }
    }

    int GetSnappedNumber(float rotation)
    {
        float normalizedRotation = Mathf.Repeat(rotation, 360f);
        float rawNumberFloat = normalizedRotation / snapAngle;
        int rawNumber = Mathf.RoundToInt(rawNumberFloat) % 40;
        int correctedNumber = (40 - rawNumber) % 40;
        return correctedNumber;
    }

    void SnapToNearestNumber()
    {
        int nearestNumber = GetSnappedNumber(currentRotation);
        float visualTargetRotation = (40 - nearestNumber) % 40 * snapAngle;
        currentRotation = visualTargetRotation;
        UpdateDialRotation(currentRotation);
        currentNumber = nearestNumber;
        PlaySound(snapSound);
    }

    void EnterCurrentNumber()
    {
        if (enteredCombination.Count >= 3 || isReturningToZero)
        {
            return;
        }

        if (lastRotationDirection == Direction.None)
        {
            if (enableDebugLogs)
                Debug.Log("Must rotate before entering number");
            return;
        }

        int currentIndex = enteredCombination.Count;

        if (currentIndex >= correctCombinations.Length)
        {
            return;
        }

        Direction requiredDirection = correctCombinations[currentIndex].direction;

        if (lastRotationDirection != requiredDirection)
        {
            if (enableDebugLogs)
                Debug.Log($"Wrong direction! Need {requiredDirection}, got {lastRotationDirection}");
            PlaySound(wrongSound);
            return;
        }

        DirectionalCombination entry = new DirectionalCombination(currentNumber, lastRotationDirection);
        enteredCombination.Add(entry);
        PlaySound(enterSound);

        if (enableDebugLogs)
            Debug.Log($"Entered: {currentNumber} with direction {lastRotationDirection}");

        if (enteredCombination.Count == 3)
        {
            CheckCombination();
        }
    }

    void StartReturnToZero()
    {
        isReturningToZero = true;
        returnStartRotation = currentRotation;
        returnProgress = 0f;
        float shortestAngle = Mathf.DeltaAngle(currentRotation, 0f);
        float distance = Mathf.Abs(shortestAngle);
        returnDuration = distance / returnSpeed;

        if (enableDebugLogs)
            Debug.Log($"Starting return to zero from {currentRotation}°, duration: {returnDuration:F2}s");
    }

    void CheckCombination()
    {
        bool correct = true;

        for (int i = 0; i < correctCombinations.Length; i++)
        {
            if (i >= enteredCombination.Count)
            {
                correct = false;
                break;
            }

            if (enteredCombination[i].number != correctCombinations[i].number ||
                enteredCombination[i].direction != correctCombinations[i].direction)
            {
                correct = false;
                break;
            }
        }

        if (correct)
        {
            UnlockSuccess();
        }
        else
        {
            UnlockFailed();
        }
    }

    void UnlockSuccess()
    {
        isUnlocked = true;
        PlaySound(unlockSound);
        OnUnlocked();

        // Auto-exit interaction after unlock
        Invoke(nameof(ExitInteraction), 1.5f);
    }

    void UnlockFailed()
    {
        ResetLock();
        PlaySound(wrongSound);
    }

    void ResetLock()
    {
        if (isReturningToZero) return;

        enteredCombination.Clear();
        lastRotationDirection = Direction.None;
        StartReturnToZero();
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    protected virtual void OnUnlocked()
    {
        // Override this in derived classes for custom unlock behavior
    }

    #endregion

    #region Public Getters

    public int GetCurrentNumber() => currentNumber;
    public bool IsUnlocked() => isUnlocked;
    public bool IsReturningToZero() => isReturningToZero;
    public bool IsInteracting() => isInteracting;

    public DirectionalCombination[] GetCurrentCombination()
    {
        return correctCombinations;
    }

    public List<DirectionalCombination> GetEnteredCombination()
    {
        return new List<DirectionalCombination>(enteredCombination);
    }

    #endregion
}