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

        GameProgressTracker.Instance?.SetBool("HasSeenIntro", true);

        // Start the save and wait for it to complete
        SaveEventManager.Instance.SaveToSlot(SaveEventManager.Instance.selectedSlot);

        // Wait for the save operation to finish
        while (SaveEventManager.Instance.isSaving)
        {
            yield return null;
        }

        // Small buffer
        yield return new WaitForSeconds(0.2f);

        // Now it's safe to change scenes
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevelIndex);


    }
}