using System;
using System.Collections;
using System.Collections.Generic;
using Doody.GameEvents;
using Doody.GameEvents.Health;
using UnityEngine;
using UnityEngine.UI;

public class HealthManager : EventListener
{
    public int maxHealth = 4;
    public int currentHealth;
#pragma warning disable CS0414
    private bool isDead = false;

    public bool isInvincible = false;

    [Header("Camera Shake Settings")]
    [SerializeField] private CameraRecoil cameraRecoil;
    [SerializeField] private Vector3 damageRotationRecoil = new Vector3(-2f, 0.5f, 0.2f);
    [SerializeField] private Vector3 damagePositionRecoil = new Vector3(0f, -0.05f, -0.1f);
    [SerializeField] private float shakeIntensityMultiplier = 1f;

    [Header("Blood Edge Settings")]
    [SerializeField] private List<Sprite> bloodEdges = new List<Sprite>();
    [SerializeField] private Image bloodEdgeRenderer; 
    [SerializeField] private float bloodEdgeDisplayTime = 1.5f;

    [Header("Pulse Settings (2/4 HP and below)")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.3f; 
    [SerializeField] private float maxAlpha = 1f; 

    private Coroutine bloodEdgeCoroutine;
    private Coroutine pulseCoroutine;
    private bool shouldPulse = false;

    public static HealthManager instance;

    void Start()
    {
        instance = this;
        currentHealth = maxHealth;
        Events.Subscribe<AddHealthEvent>(AddHealth, this);
        Events.Subscribe<RemoveHealthEvent>(RemoveHealth, this);

        if (cameraRecoil == null)
        {
            cameraRecoil = FindObjectOfType<CameraRecoil>();
            if (cameraRecoil == null)
            {
                Debug.LogWarning("CameraRecoil component not found. Camera shake will not work.");
            }
        }

        if (bloodEdgeRenderer == null)
        {
            bloodEdgeRenderer = GetComponentInChildren<Image>();
            if (bloodEdgeRenderer == null)
            {
                Debug.LogWarning("Blood Edge SpriteRenderer not found. Please assign one.");
            }
        }

        if (bloodEdgeRenderer != null)
        {
            bloodEdgeRenderer.enabled = false;
        }
    }

    private void AddHealth(AddHealthEvent data)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + data.Amount, 0, maxHealth);

        UpdateBloodEdge(previousHealth);
    }

    private void RemoveHealth(RemoveHealthEvent data)
    {
        if (isInvincible) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - data.Amount, 0, maxHealth);

        ShakeCamera(data.Amount);

        UpdateBloodEdge(previousHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            Events.Publish(new DeadEvent());
        }
    }

    private void UpdateBloodEdge(int previousHealth)
    {
        float healthPercentage = (float)currentHealth / maxHealth * 100f;

        StopPulse();

        if (healthPercentage == 100f)
        {
            HideBloodEdge();
        }
        else if (healthPercentage > 75f)
        {
            HideBloodEdge();
        }
        else if (healthPercentage > 50f)
        {
            ShowBloodEdge(0);
        }
        else if (healthPercentage > 25f)
        {
            ShowBloodEdge(1);
            StartPulse();
        }
        else if (healthPercentage > 0f)
        {
            ShowBloodEdge(2);
            StartPulse();
        }
        else
        {
            HideBloodEdge();
        }
    }

    private void ShowBloodEdge(int index)
    {
        if (bloodEdgeRenderer == null || bloodEdges == null || index >= bloodEdges.Count)
        {
            Debug.LogWarning($"Cannot show blood edge at index {index}. Check SpriteRenderer and bloodEdges list.");
            return;
        }

        bloodEdgeRenderer.sprite = bloodEdges[index];
        bloodEdgeRenderer.enabled = true;

        Color currentColor = bloodEdgeRenderer.color;
        currentColor.a = 1f;
        bloodEdgeRenderer.color = currentColor;

        if (bloodEdgeDisplayTime > 0 && !shouldPulse)
        {
            if (bloodEdgeCoroutine != null)
            {
                StopCoroutine(bloodEdgeCoroutine);
            }
            bloodEdgeCoroutine = StartCoroutine(HideBloodEdgeAfterDelay(bloodEdgeDisplayTime));
        }
    }

    private void HideBloodEdge()
    {
        if (bloodEdgeRenderer != null)
        {
            bloodEdgeRenderer.enabled = false;

            if (bloodEdgeCoroutine != null)
            {
                StopCoroutine(bloodEdgeCoroutine);
                bloodEdgeCoroutine = null;
            }
        }

        StopPulse();
    }

    private IEnumerator HideBloodEdgeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!shouldPulse)
        {
            HideBloodEdge();
        }
    }

    private void StartPulse()
    {
        shouldPulse = true;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(PulseTransparency());
    }

    private void StopPulse()
    {
        shouldPulse = false;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (bloodEdgeRenderer != null && bloodEdgeRenderer.enabled)
        {
            Color currentColor = bloodEdgeRenderer.color;
            currentColor.a = 1f;
            bloodEdgeRenderer.color = currentColor;
        }
    }

    private IEnumerator PulseTransparency()
    {
        while (shouldPulse && bloodEdgeRenderer != null && bloodEdgeRenderer.enabled)
        {
            float pulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; 
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulseValue);

            Color currentColor = bloodEdgeRenderer.color;
            currentColor.a = alpha;
            bloodEdgeRenderer.color = currentColor;

            yield return null;
        }
    }

    private void ShakeCamera(int damageAmount = 1)
    {
        if (cameraRecoil == null) return;

        float intensity = Mathf.Clamp(damageAmount * 0.5f, 0.5f, 3f) * shakeIntensityMultiplier;

        Vector3 scaledRotation = damageRotationRecoil * intensity;
        Vector3 scaledPosition = damagePositionRecoil * intensity;

        cameraRecoil.ApplyRecoil(scaledRotation, scaledPosition);

        Debug.Log($"Camera shake applied with intensity: {intensity}");
    }

    public void TriggerDamageShake(float customIntensity = 1f)
    {
        if (cameraRecoil == null) return;

        Vector3 scaledRotation = damageRotationRecoil * customIntensity;
        Vector3 scaledPosition = damagePositionRecoil * customIntensity;

        cameraRecoil.ApplyRecoil(scaledRotation, scaledPosition);
    }

    private IEnumerator ShakeCameraSequence(float duration, float intensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cameraRecoil == null) yield break;

            float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);
            TriggerDamageShake(currentIntensity);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void UpdateBloodEdgeVisuals()
    {
        UpdateBloodEdge(currentHealth);
    }




}