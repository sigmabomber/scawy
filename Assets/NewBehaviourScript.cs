using UnityEngine;
using UnityEngine.UI;

public class SegmentedProgressBar : MonoBehaviour
{
    [SerializeField] private Image[] segments;
    [SerializeField] private Color filledColor = Color.green;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Progress Settings")]
    [SerializeField] public float maxValue = 100f;
    [SerializeField] public float fillDuration = 3f; 

    [Header("Debug")]
    [SerializeField] public float debugValue = 55f;
    [SerializeField] private bool autoFill = false; 

    private float currentProgress = 0f;
    private float fillStartTime;
    private bool isFilling = false;

    private void OnValidate()
    {
        debugValue = Mathf.Clamp(debugValue, 0, maxValue);
    }

    private void Update()
    {
        if (autoFill)
        {
            if (!isFilling)
            {
                StartFill();
            }

            float elapsed = Time.time - fillStartTime;
            float progress = Mathf.Clamp01(elapsed / fillDuration);
            currentProgress = progress * maxValue;

            if (progress >= 1f)
            {
                autoFill = false;
                isFilling = false;
            }
        }
        else
        {
            currentProgress = debugValue;
        }

        SetProgress(currentProgress, maxValue);
    }

    public void StartFill()
    {
        fillStartTime = Time.time;
        isFilling = true;
        currentProgress = 0f;
    }

    public void FillToValue(float targetValue, float duration)
    {
        fillDuration = duration;
        maxValue = targetValue;
        autoFill = true;
        StartFill();
    }

    public void SetProgress(float currentValue, float max)
    {
        currentValue = Mathf.Clamp(currentValue, 0, max);
        float segmentValue = (currentValue / max) * segments.Length;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segmentValue >= i + 1)
            {
                segments[i].fillAmount = 1f;
                segments[i].color = filledColor;
            }
            else if (segmentValue > i)
            {
                float fillAmount = segmentValue - i;
                segments[i].fillAmount = fillAmount;
                segments[i].color = filledColor;
            }
            else
            {
                segments[i].fillAmount = 0f;
                segments[i].color = emptyColor;
            }
        }
    }

    public void SetProgress(float currentValue)
    {
        SetProgress(currentValue, maxValue);
    }

    public float GetFillDuration()
    {
        return fillDuration;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }
}