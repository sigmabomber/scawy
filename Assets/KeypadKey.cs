using UnityEngine;
using System.Collections;

public class KeypadKey : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float pressDepth = 0.1f;
    [SerializeField] private float pressDuration = 0.1f;
    [SerializeField] private float releaseDuration = 0.1f;

    [Header("Key Settings")]
    [SerializeField] private int keyNumber;
    [SerializeField] private KeypadSystem keypadSystem;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;

    private Vector3 originalPos;
    private bool isAnimating = false;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void OnMouseDown()
    {
        PressKey();
    }
    //

    public void PressKey()
    {
        if (!isAnimating)
        {
            StartCoroutine(PressAnimation());
        }
    }

    private IEnumerator PressAnimation()
    {
        isAnimating = true;

        if (keypadSystem != null)
        {
            keypadSystem.OnKeyPressed(keyNumber);
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }

        Vector3 pressedPos = originalPos - transform.forward * pressDepth;

        float elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            transform.localPosition = Vector3.Lerp(originalPos, pressedPos, t);
            yield return null;
        }

        transform.localPosition = pressedPos;

        elapsed = 0f;
        while (elapsed < releaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / releaseDuration;
            transform.localPosition = Vector3.Lerp(pressedPos, originalPos, t);
            yield return null;
        }

        transform.localPosition = originalPos;
        isAnimating = false;
    }
}