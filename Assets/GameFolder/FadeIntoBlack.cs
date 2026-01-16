using System.Collections;
using Doody.Framework.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeIntoBlack : MonoBehaviour, IDialogueCustomAction
{
    public float duration = 0.5f;
    public CanvasGroup group;
    public string eventID = "fade_black"; 

    void Start()
    {
        DialogueEventDispatcher.Instance.RegisterEvent(eventID, Execute);
    }

    void OnDestroy()
    {
        if (DialogueEventDispatcher.Instance != null)
        {
            DialogueEventDispatcher.Instance.UnregisterEvent(eventID, Execute);
        }
    }

    public void Execute()
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

        yield return new WaitForSeconds(1f);
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevelIndex);
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            if (obj.transform.parent == null)
            {
                Object.Destroy(obj);
            }
        }

    }
}