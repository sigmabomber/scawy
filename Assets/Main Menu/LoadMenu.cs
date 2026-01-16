using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMenu : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform Container;

    private TMP_Text slotCount;
    private TMP_Text lastPlayed;
    private TMP_Text playTime;

    public static LoadMenu Instance;


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
            if (SaveEventManager.Instance.SaveExists(i))
                return;

            var data = SaveEventManager.Instance.GetSaveInfo(i);

            GameObject newSlot = Instantiate(slotPrefab);

            Transform parent =  newSlot.transform.Find("HasSave") ;
            lastPlayed = parent.Find("LastPlayed").GetComponent<TMP_Text>();
            playTime = parent.Find("Playtime").GetComponent<TMP_Text>();
            slotCount = newSlot.transform.Find("SlotCount").GetComponent<TMP_Text>();

            if (playTime == null || lastPlayed == null || slotCount == null)
            {
                Debug.LogError("Gameobject is missing!");
                return;
            }



            slotCount.text = $"Slot {i + 1}";
            lastPlayed.text = SaveEventManager.Instance.GetLastPlayed(i).ToString();
            
            playTime.text = SaveEventManager.Instance.GetFormattedPlaytime(i).ToString();

            

            newSlot.transform.SetParent(Container, false);
            newSlot.name = i.ToString();
            newSlot.GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(i));
            

        }



    }

    private void OnButtonClicked(int slotNumber)
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevelIndex);

        SaveEventManager.Instance.selectedSlot = slotNumber;

    }
}
