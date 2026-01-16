using Doody.Framework.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDialogue : MonoBehaviour
{
    public float duration = 0.5f;
    public CanvasGroup group;
    public NPCInteraction npc;
    private void OnTriggerEnter(Collider other)
    {

        if (other.tag != "Player") return;
        print("ee");

        npc.Interact();

        StartCoroutine(FadeIn(group));
    }

    IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        canvasGroup.gameObject.SetActive(true);
        yield return new WaitForSeconds(2);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(1f);


    }
}
