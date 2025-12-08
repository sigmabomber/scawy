using UnityEngine;
using UnityEngine.UI;

public class SegmentedProgressBar : MonoBehaviour
{
    [SerializeField] private Image[] segments; // Assign your segment images in inspector
    [SerializeField] private Color filledColor = Color.green;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Progress Settings")]
    [SerializeField] public float maxValue = 100f; // The maximum value (e.g., 100 for percentage, 10 for level 10, etc.)

    [Header("Debug")]
    [SerializeField] public float debugValue = 55f; // Current value (e.g., 55 out of 100)

    private void OnValidate()
    {
        // Clamp debug value to valid range
        debugValue = Mathf.Clamp(debugValue, 0, maxValue);
    }

    private void Update()
    {
        // Update in real-time as you change the value in Inspector
        SetProgress(debugValue, maxValue);
    }

    public void SetProgress(float currentValue, float max)
    {
        // Clamp current value
        currentValue = Mathf.Clamp(currentValue, 0, max);

        // Convert current value to segment value
        // e.g., if max is 100 and we have 6 segments: 55/100 * 6 = 3.3 segments filled
        float segmentValue = (currentValue / max) * segments.Length;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segmentValue >= i + 1)
            {
                // Fully filled segment
                segments[i].fillAmount = 1f;
                segments[i].color = filledColor;
            }
            else if (segmentValue > i)
            {
                // Partially filled segment
                float fillAmount = segmentValue - i; // Gets the decimal part
                segments[i].fillAmount = fillAmount;
                segments[i].color = filledColor;
            }
            else
            {
                // Empty segment
                segments[i].fillAmount = 0f;
                segments[i].color = emptyColor;
            }
        }
    }

    // Convenience method to set progress with the default max value
    public void SetProgress(float currentValue)
    {
        SetProgress(currentValue, maxValue);
    }
}