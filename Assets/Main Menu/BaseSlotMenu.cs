using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Base class for both NewGameMenu and LoadMenu to share common functionality
/// </summary>
public abstract class BaseSlotMenu : MonoBehaviour
{
    [Header("Slot Configuration")]
    public GameObject slotPrefab;
    public Transform Container;

    [Header("Fade Settings")]
    public CanvasGroup fadeInBlack;
    public float fadeDuration = 0.5f;

    protected TMP_Text slotCount;
    protected TMP_Text lastPlayed;
    protected TMP_Text playTime;

    protected virtual void Start()
    {
        // Override in derived classes if needed
    }

    #region Slot Management
    //

    protected void ClearAllSlots()
    {
        for (int i = 0; i < Container.childCount; i++)
        {
            if (Container.GetChild(i).name == "Return") continue;
            Destroy(Container.GetChild(i).gameObject);
        }
    }

    public void LoadSlots()
    {
        ClearAllSlots();
        CreateAllSlots();
    }

    protected abstract void CreateAllSlots();

    protected GameObject CreateSlotObject(int slotIndex, bool hasSave)
    {
        GameObject newSlot = Instantiate(slotPrefab);
        Transform parent = hasSave ? newSlot.transform.Find("HasSave") : newSlot.transform.Find("NoSave");

        if (parent != null)
        {
            parent.gameObject.SetActive(true);
        }

        // Set slot count
        slotCount = newSlot.transform.Find("SlotCount").GetComponent<TMP_Text>();
        if (slotCount != null)
        {
            slotCount.text = $"Slot {slotIndex + 1}";
        }

        // Populate save info if it exists
        if (hasSave && parent != null)
        {
            PopulateSaveInfo(parent, slotIndex);
        }

        newSlot.transform.SetParent(Container, false);
        newSlot.name = slotIndex.ToString();

        return newSlot;
    }

    protected void PopulateSaveInfo(Transform parent, int slotIndex)
    {
        lastPlayed = parent.Find("LastPlayed")?.GetComponent<TMP_Text>();
        playTime = parent.Find("Playtime")?.GetComponent<TMP_Text>();

        if (lastPlayed == null || playTime == null)
        {
            Debug.LogError($"Slot {slotIndex}: Missing LastPlayed or Playtime text component!");
            return;
        }

        lastPlayed.text = SaveEventManager.Instance.GetLastPlayed(slotIndex).ToString();
        playTime.text = SaveEventManager.Instance.GetFormattedPlaytime(slotIndex).ToString();
    }

    #endregion

    #region Fade Effects

    protected IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    protected IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    protected IEnumerator FadeAndLoadScene(int slotNumber, float waitTime = 0.9f)
    {
        yield return StartCoroutine(FadeIn(fadeInBlack));
        yield return new WaitForSeconds(waitTime);

        SaveEventManager.Instance.selectedSlot = slotNumber;

        // Read HasSeenIntro directly from the save file (before loading anything)
        bool? hasSeenIntroNullable = SaveEventManager.Instance.GetGameProgressBool(
            slotNumber,
            "HasSeenIntro"
        );

        bool hasSeenIntro = hasSeenIntroNullable ?? false; // Default to false if not found

        print($"HasSeenIntro from save file (slot {slotNumber}): {hasSeenIntro}");

        // Calculate which scene to load
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + (hasSeenIntro ? 2 : 1);

        print($"Loading scene index: {nextLevelIndex} (skipping intro: {hasSeenIntro})");

        SceneManager.LoadScene(nextLevelIndex);
    }
    #endregion
}