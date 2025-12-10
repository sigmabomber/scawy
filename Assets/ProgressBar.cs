
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Doody.GameEvents;
using Doody.InventoryFramework;
using Doody.Framework.Progressbar;

public class SegmentedProgressBar : EventListener
{
    [SerializeField] private Image[] segments;
    [SerializeField] private Color filledColor = Color.green;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Progress Settings")]
    [SerializeField] public float maxValue = 100f;
    [SerializeField] public float fillDuration = 3f; 


    private float currentProgress = 0f;
    private float fillStartTime;
    private bool isFilling = false;

    

    void Start()
    {
        Events.Subscribe<StartProgressBar>(StartFill, this);

    }

    private void Update()
    {

        if (!isFilling) return;
            float elapsed = Time.time - fillStartTime;
            float progress = Mathf.Clamp01(elapsed / fillDuration);
            currentProgress = progress * maxValue;

            if (progress >= 1f)
            {
                isFilling = false;
            }
        
     
        SetProgress(currentProgress, maxValue);
    }



    public void StartFill(StartProgressBar data)
    {
        fillStartTime = Time.time;
        isFilling = true;
        currentProgress = 0f;
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