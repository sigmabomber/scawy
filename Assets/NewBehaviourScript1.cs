using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PadlockSystem : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class NumberRotation
    {
        public int number;
        public float rotationDegrees;
    }

    [System.Serializable]
    public class CombinationStep
    {
        public int number;
        public RotationDirection requiredDirection;
    }

    public enum RotationDirection
    {
        Left,   // Counter-clockwise (A key / Left Arrow)
        Right   // Clockwise (D key / Right Arrow)
    }

    [Header("Model References")]
    [Tooltip("The knob/dial that rotates")]
    [SerializeField] private Transform knobTransform;

    [Tooltip("Optional: The main lock body for unlock animation")]
    [SerializeField] private Transform lockBodyTransform;

    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Examine Padlock";
    [SerializeField] private Sprite interactionIcon;
    [SerializeField] private Transform cameraInteractPosition;
    [SerializeField] private float cameraTransitionDuration = 0.8f;
    [SerializeField] private AnimationCurve cameraTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float crouchTilt = 15f;

    [Header("Combination Settings")]
    [Tooltip("The correct combination with required rotation directions")]
    [SerializeField]
    private CombinationStep[] combination = new CombinationStep[]
    {
        new CombinationStep { number = 15, requiredDirection = RotationDirection.Left },
        new CombinationStep { number = 25, requiredDirection = RotationDirection.Right },
        new CombinationStep { number = 35, requiredDirection = RotationDirection.Left }
    };

    [Tooltip("Total numbers on the dial (39 for your lock)")]
    [SerializeField] private int totalNumbers = 39;

    [Header("Number to Rotation Mapping")]
    [Tooltip("Assign custom rotation for each number. Leave empty to auto-calculate.")]
    [SerializeField] private NumberRotation[] numberRotations = new NumberRotation[0];

    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation animation")]
    [SerializeField] private float rotationSpeed = 200f;

    [Tooltip("Which axis to rotate (Z for front-facing dial, Y for top-down)")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

    [Header("Wrong Combination Shake")]
    [Tooltip("Enable shake effect on wrong combination")]
    [SerializeField] private bool enableShake = true;

    [Tooltip("Intensity of the shake")]
    [SerializeField] private float shakeIntensity = 0.1f;

    [Tooltip("Duration of the shake")]
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip submitSound;
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip wrongCombinationSound;

    [Header("Events")]
    public UnityEvent onUnlock;
    public UnityEvent onWrongCombination;
    public UnityEvent onNumberChanged;
    public UnityEvent onInteractionStart;
    public UnityEvent onInteractionEnd;

    // State
    private int currentNumber = 0;
    private int currentStep = 0;
    private int[] enteredCombination;
    private RotationDirection[] enteredDirections;
    private RotationDirection lastRotationDirection = RotationDirection.Right;
    private int previousNumber = 0;
    private bool isUnlocked = false;
    private bool isRotating = false;
    private float currentRotation = 0f;
    private float degreesPerNumber;
    private Vector3 originalPosition;
    private bool isShaking = false;

    // Interaction state
    private bool isInteracting = false;
    private Transform playerCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;

    public enum RotationAxis { X, Y, Z }

    void Start()
    {
        if (knobTransform == null)
        {
            Debug.LogError("Knob Transform is not assigned!");
            enabled = false;
            return;
        }

        if (cameraInteractPosition == null)
        {
            Debug.LogError("Camera Interact Position not assigned! Please assign an empty GameObject for camera positioning.");
        }

        enteredCombination = new int[combination.Length];
        enteredDirections = new RotationDirection[combination.Length];
        degreesPerNumber = 360f / totalNumbers;

        // Store original position for shake effect
        originalPosition = transform.localPosition;

        // Initialize at 0
        currentNumber = 0;
        previousNumber = 0;
        UpdateKnobRotation();

        // Show combination requirements
        for (int i = 0; i < combination.Length; i++)
        {
            string dirText = combination[i].requiredDirection == RotationDirection.Left ? "LEFT" : "RIGHT";
        }
    }

    void Update()
    {
        if (!isInteracting) return;

        if (isUnlocked || isRotating || isShaking) return;

        HandleInput();

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

        onInteractionStart?.Invoke();
        Debug.Log("Started padlock interaction");
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

        // Reset the combination when exiting
        ResetCombination();

        StartCoroutine(TransitionCameraBack());

        onInteractionEnd?.Invoke();
        Debug.Log("Exited padlock interaction. Lock reset.");
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
        InputScript.InputEnabled = true;
    }

    #endregion

    void HandleInput()
    {
        // Rotate left (counter-clockwise) - decrease number
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RotateToNumber(currentNumber - 1, RotationDirection.Left);
        }

        // Rotate right (clockwise) - increase number
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            RotateToNumber(currentNumber + 1, RotationDirection.Right);
        }

        // Submit current number
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            SubmitNumber();
        }

        // Reset combination
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCombination();
        }
    }

    void RotateToNumber(int targetNumber, RotationDirection direction)
    {
        // Wrap around (0 to totalNumbers-1)
        if (targetNumber < 0)
            targetNumber = totalNumbers - 1;
        else if (targetNumber >= totalNumbers)
            targetNumber = 0;

        if (targetNumber != currentNumber)
        {
            previousNumber = currentNumber;
            currentNumber = targetNumber;
            lastRotationDirection = direction;

            StartCoroutine(AnimateRotation());
            PlaySound(clickSound);
            onNumberChanged?.Invoke();

            string dirText = direction == RotationDirection.Left ? "LEFT" : "RIGHT";
            Debug.Log($"Current number: {currentNumber} (approached from {dirText})");
        }
    }

    float GetRotationForNumber(int number)
    {
        // Check if custom rotation is defined
        if (numberRotations != null && numberRotations.Length > 0)
        {
            foreach (var nr in numberRotations)
            {
                if (nr.number == number)
                {
                    return -nr.rotationDegrees; // Negative for clockwise
                }
            }
        }

        // Default auto-calculated rotation
        return -(number * degreesPerNumber);
    }

    IEnumerator AnimateRotation()
    {
        isRotating = true;

        float targetRotation = GetRotationForNumber(currentNumber);

        // Calculate the shortest rotation direction
        float difference = targetRotation - currentRotation;

        // Normalize the difference to -180 to 180 range
        while (difference > 180f) difference -= 360f;
        while (difference < -180f) difference += 360f;

        float finalTarget = currentRotation + difference;

        while (Mathf.Abs(currentRotation - finalTarget) > 0.1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, finalTarget,
                                                rotationSpeed * Time.deltaTime);
            UpdateKnobRotation();
            yield return null;
        }

        currentRotation = finalTarget;
        UpdateKnobRotation();

        isRotating = false;
    }

    void UpdateKnobRotation()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                knobTransform.localRotation = Quaternion.Euler(currentRotation, 0, 0);
                break;
            case RotationAxis.Y:
                knobTransform.localRotation = Quaternion.Euler(0, currentRotation, 0);
                break;
            case RotationAxis.Z:
                knobTransform.localRotation = Quaternion.Euler(0, 0, currentRotation);
                break;
        }
    }

    void SubmitNumber()
    {
        if (currentStep >= combination.Length)
        {
            Debug.Log("All numbers already entered. Press R to reset.");
            return;
        }

        // Always accept and store the number, regardless of correctness
        PlaySound(submitSound);

        enteredCombination[currentStep] = currentNumber;
        enteredDirections[currentStep] = lastRotationDirection;

        string dirText = lastRotationDirection == RotationDirection.Left ? "LEFT" : "RIGHT";
        Debug.Log($"✓ Step {currentStep + 1}/{combination.Length}: Entered {currentNumber} from {dirText}");

        currentStep++;

        // Only check if all 3 numbers have been entered
        if (currentStep >= combination.Length)
        {
            CheckFullCombination();
        }
    }

    void CheckFullCombination()
    {
        bool allCorrect = true;

        // Check each number and direction
        for (int i = 0; i < combination.Length; i++)
        {
            if (enteredCombination[i] != combination[i].number)
            {
                allCorrect = false;
                Debug.Log($"Step {i + 1} wrong number: Expected {combination[i].number}, got {enteredCombination[i]}");
            }

            if (enteredDirections[i] != combination[i].requiredDirection)
            {
                allCorrect = false;
                string expectedDir = combination[i].requiredDirection == RotationDirection.Left ? "LEFT" : "RIGHT";
                string actualDir = enteredDirections[i] == RotationDirection.Left ? "LEFT" : "RIGHT";
                Debug.Log($"Step {i + 1} wrong direction: Expected {expectedDir}, got {actualDir}");
            }
        }

        if (allCorrect)
        {
            Unlock();
        }
        else
        {
            // Wrong combination - shake and reset to 0
            Debug.Log("✗ Full combination is wrong!");
            PlaySound(wrongCombinationSound);
            onWrongCombination?.Invoke();

            if (enableShake)
            {
                StartCoroutine(ShakeAndReset());
            }
            else
            {
                ResetCombination();
            }
        }
    }

    void Unlock()
    {
        isUnlocked = true;
        Debug.Log("★★★ LOCK OPENED! Correct combination with correct directions! ★★★");

        PlaySound(unlockSound);
        onUnlock?.Invoke();

        // Optional: Open the lock visually
        StartCoroutine(OpenLockAnimation());

        // Auto-exit interaction after unlock
        Invoke(nameof(ExitInteraction), 1.5f);
    }

    IEnumerator OpenLockAnimation()
    {
        if (lockBodyTransform != null)
        {
            Vector3 startPos = lockBodyTransform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 0.5f;

            float elapsed = 0f;
            float duration = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                lockBodyTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
        }
    }

    public void ResetCombination()
    {
        currentStep = 0;
        enteredCombination = new int[combination.Length];
        enteredDirections = new RotationDirection[combination.Length];
        lastRotationDirection = RotationDirection.Right;

        // Reset dial to 0
        currentNumber = 0;
        previousNumber = 0;
        StartCoroutine(AnimateRotationToZero());

        Debug.Log("Combination reset. Dial returning to 0.");
    }

    IEnumerator AnimateRotationToZero()
    {
        isRotating = true;

        float targetRotation = 0f;

        // Calculate the shortest rotation direction to 0
        float difference = targetRotation - currentRotation;

        // Normalize the difference to -180 to 180 range
        while (difference > 180f) difference -= 360f;
        while (difference < -180f) difference += 360f;

        float finalTarget = currentRotation + difference;

        while (Mathf.Abs(currentRotation - finalTarget) > 0.1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, finalTarget,
                                                rotationSpeed * Time.deltaTime);
            UpdateKnobRotation();
            yield return null;
        }

        currentRotation = finalTarget;
        UpdateKnobRotation();

        isRotating = false;
        Debug.Log("Start entering numbers again.");
    }

    IEnumerator ShakeAndReset()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Random shake offset
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetZ = Random.Range(-shakeIntensity, shakeIntensity);

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, offsetZ);

            yield return null;
        }

        // Return to original position
        transform.localPosition = originalPosition;
        isShaking = false;

        // Now reset the combination
        ResetCombination();
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Helper method to generate all number rotations automatically
    [ContextMenu("Generate All Number Rotations")]
    void GenerateAllNumberRotations()
    {
        numberRotations = new NumberRotation[totalNumbers];
        for (int i = 0; i < totalNumbers; i++)
        {
            numberRotations[i] = new NumberRotation
            {
                number = i,
                rotationDegrees = i * (360f / totalNumbers)
            };
        }
        Debug.Log($"Generated {totalNumbers} number rotations!");
    }

    // Public getters
    public int GetCurrentNumber() => currentNumber;
    public int GetCurrentStep() => currentStep;
    public bool IsUnlocked() => isUnlocked;
    public bool IsInteracting() => isInteracting;
    public int[] GetEnteredCombination() => enteredCombination;
    public CombinationStep[] GetCorrectCombination() => combination;
    public RotationDirection GetLastRotationDirection() => lastRotationDirection;
}