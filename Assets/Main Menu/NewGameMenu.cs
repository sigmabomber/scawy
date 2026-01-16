using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGameMenu : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform Container;

    private TMP_Text slotCount;
    private TMP_Text lastPlayed;
    private TMP_Text playTime;


    public float duration = 0.5f;

    public static NewGameMenu Instance;


    private void Start()
    {
        Instance = this;
    }
    private void ClearAllSlots()
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

    private void CreateAllSlots()
    {
        for (int i = 0; i < SaveEventManager.Instance.maxSlots; i++)
        {
            

            var data = SaveEventManager.Instance.GetSaveInfo(i);
            bool hasSave = SaveEventManager.Instance.SaveExists(i);

            GameObject newSlot = Instantiate(slotPrefab);
            Transform parent = hasSave ? newSlot.transform.Find("HasSave") : newSlot.transform.Find("NoSave");
            slotCount = newSlot.transform.Find("SlotCount").GetComponent<TMP_Text>();
            if (hasSave)
            {

                lastPlayed = parent.Find("LastPlayed").GetComponent<TMP_Text>();
                playTime = parent.Find("Playtime").GetComponent<TMP_Text>();


                if (playTime == null || lastPlayed == null || slotCount == null)
                {
                    Debug.LogError("Gameobject is missing!");
                    return;
                }

                lastPlayed.text = SaveEventManager.Instance.GetLastPlayed(i).ToString();

                playTime.text = SaveEventManager.Instance.GetFormattedPlaytime(i).ToString();
            }
            else
            {

            }
           
          

           

     
            slotCount.text = $"Slot {i + 1}";



            newSlot.transform.SetParent(Container, false);
            newSlot.name = i.ToString();
            if (hasSave)
            {
                newSlot.GetComponent<ButtonHoverEvent>().OnHoverEnter.AddListener(() => OnButtonHovered(parent.Find("Override")));
                newSlot.GetComponent<ButtonHoverEvent>().OnHoverExit.AddListener(() => OnButtonHoveredExit(parent.Find("Override")));
            }
            newSlot.GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(i));


        }



    }

    private void OnButtonHovered(Transform button)
    {
        StartCoroutine(FadeIn(button.GetComponent<CanvasGroup>()));
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
    IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
    private void OnButtonHoveredExit(Transform button)
    {
        StartCoroutine(FadeOut(button.GetComponent<CanvasGroup>()));
    }

    private void OnButtonClicked(int slotNumber)
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevelIndex);

        SaveEventManager.Instance.selectedSlot = slotNumber;

    }
}
