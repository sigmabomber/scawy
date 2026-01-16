using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeIntoBlack : MonoBehaviour
{
    public float duration = 0.5f;

    public CanvasGroup group;


    public void FadeBlack()
    {
        StartCoroutine(FadeIn(group));
    }
    IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
