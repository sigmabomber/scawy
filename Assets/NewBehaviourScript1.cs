using UnityEngine;
using System.Collections.Generic;

public class RotaryLockController : MonoBehaviour
{
    [SerializeField]
    private DirectionalCombination[] correctCombinations = new DirectionalCombination[]
    {
        new DirectionalCombination(7, Direction.Left),
        new DirectionalCombination(18, Direction.Right),
        new DirectionalCombination(32, Direction.Left)
    };

    [SerializeField] private float snapAngle = 9f;

    [SerializeField] private Transform dialTransform;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip dialRotateSound;
    [SerializeField] private AudioClip snapSound;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip unlockSound;

    private List<DirectionalCombination> enteredCombination = new List<DirectionalCombination>();
    private int currentNumber = 0;
    private float currentRotation = 0f;
    private bool isUnlocked = false;
    private int lastSnappedNumber = 0;

    [SerializeField] private bool enableDebugLogs = true;

    [SerializeField] private float returnSpeed = 180f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private bool isReturningToZero = false;
    private float returnStartRotation = 0f;
    private float returnProgress = 0f;
    private float returnDuration = 0f;

    [SerializeField] private float rotationIncrement = 9f;
    [SerializeField] private bool rotateToNextNumber = true;

    private Direction lastRotationDirection = Direction.None;

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
    }

    void Update()
    {
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
    }

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

       // StartReturnToZero();

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
    }

    public int GetCurrentNumber() => currentNumber;
    public bool IsUnlocked() => isUnlocked;
    public bool IsReturningToZero() => isReturningToZero;

    public DirectionalCombination[] GetCurrentCombination()
    {
        return correctCombinations;
    }

    public List<DirectionalCombination> GetEnteredCombination()
    {
        return new List<DirectionalCombination>(enteredCombination);
    }
}