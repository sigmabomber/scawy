using UnityEngine;
using UnityEngine.UI;
using Doody.GameEvents;
using Doody.Framework.Progressbar;
using TMPro;

public class ProgressBarUI : EventListener
{
    [SerializeField] private Image[] segments;
    [SerializeField] private Color filledColor = Color.green;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Progress Settings")]
    [SerializeField] public float maxValue = 100f;
    [SerializeField] public float fillDuration = 3f;

    public TMP_Text progressText;
    private GameObject progressBar;
    private float currentProgress = 0f;
    private float fillStartTime;
    private bool isFilling = false;
    private string originalProgressName = "";

    private float animationTimer = 0f;
    private float animationSpeed = 0.5f; 
    private int animationState = 0; 

    private GameObject currentObj;

    void Start()
    {
        Events.Subscribe<StartProgressBar>(StartFill, this);
        Events.Subscribe<ProgressbarInteruppted>(StopFill, this);

        progressBar = gameObject;
        progressBar.SetActive(false);
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
            Events.Publish(new ProgressbarCompleted(currentObj));
            progressBar.SetActive(false);

            if (progressText != null)
                progressText.text = originalProgressName;
        }

        UpdateProgressTextAnimation();

        SetProgress(currentProgress, maxValue);
    }

    private void UpdateProgressTextAnimation()
    {
        if (progressText == null || string.IsNullOrEmpty(originalProgressName)) return;

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            animationState = (animationState + 1) % 3; 

            switch (animationState)
            {
                case 0:
                    progressText.text = originalProgressName + ".";
                    break;
                case 1:
                    progressText.text = originalProgressName + "..";
                    break;
                case 2:
                    progressText.text = originalProgressName + "...";
                    break;
            }
        }
    }

    public void StartFill(StartProgressBar data)
    {
        fillDuration = data.Duration;
        currentObj = data.ItemObject;
        fillStartTime = Time.time;
        isFilling = true;
        currentProgress = 0f;

        originalProgressName = data.ProgressName;
        animationTimer = 0f;
        animationState = 0;

        if (progressText != null)
            progressText.text = originalProgressName + ".";

        progressBar.SetActive(true);
    }

    private void StopFill(ProgressbarInteruppted data)
    {
        isFilling = false;
        currentProgress = 0f;
        progressBar.SetActive(false);

        if (progressText != null && !string.IsNullOrEmpty(originalProgressName))
            progressText.text = originalProgressName;
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