using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.PostProcessing;

public class ScaleUpUI : MonoBehaviour
{
    [Header("Scale Settings")]
    public Transform sprite;
    public Vector3 startScale = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 targetScale = Vector3.one;
    public float duration = 0.5f;
    public PostProcessVolume volume;

    [Header("Eye Blink Settings")]
    public float firstOpenDuration = 0.4f;
    public float quickCloseDuration = 0.15f;
    public float pauseClosedDuration = 0.6f;
    public float finalOpenDuration = 0.5f;
    public int postOpenBlinkCount = 2;
    public float blinkSpeed = 0.1f;
    public float pauseBetweenBlinks = 0.2f;
    public float blurFadeDuration = 0.3f;

    void Start()
    {
        sprite.localScale = startScale;
        if (volume != null)
        {
            volume.weight = 1f;
        }
    }

    public void StartEyes()
    {
        StartCoroutine(EyeSequence());
    }

    private IEnumerator EyeSequence()
    {
        Vector3 closedScale = new Vector3(1.3f, 0f, 1f);
        Vector3 partialOpen = new Vector3(1.3f, 1.12f, 1f);

        sprite.localScale = closedScale;

        yield return StartCoroutine(ScaleOverTime(closedScale, partialOpen, firstOpenDuration));

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(ScaleOverTime(partialOpen, closedScale, quickCloseDuration));

        StartCoroutine(FadeOutBlur(pauseClosedDuration));
        yield return new WaitForSeconds(pauseClosedDuration);

        yield return StartCoroutine(ScaleOverTime(closedScale, targetScale, finalOpenDuration));

        for (int i = 0; i < postOpenBlinkCount; i++)
        {
            yield return new WaitForSeconds(pauseBetweenBlinks);

            yield return StartCoroutine(ScaleOverTime(targetScale, closedScale, blinkSpeed));
            yield return StartCoroutine(ScaleOverTime(closedScale, targetScale, blinkSpeed));
        }
    }

    private IEnumerator FadeOutBlur(float duration)
    {
        if (volume == null) yield break;

        float timer = 0f;
        float startWeight = volume.weight;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            volume.weight = Mathf.Lerp(startWeight, 0f, t);
            yield return null;
        }

        volume.weight = 0f;
    }

    private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            sprite.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        sprite.localScale = to;
    }

    public void Restart()
    {
        StopAllCoroutines();
        sprite.localScale = startScale;
        if (volume != null)
        {
            volume.weight = 1f;
        }
        StartCoroutine(EyeSequence());
    }
}