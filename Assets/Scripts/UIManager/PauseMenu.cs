using Doody.Framework.UI;
using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : EventListener
{
    public GameObject PuaseMenu;
    public GameObject SettingsMenu;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(Input.GetKeyUp(KeyCode.Escape))
        {
            bool isOpen = !PuaseMenu.activeSelf;
            Events.Publish(new UIRequestToggleEvent(PuaseMenu));


            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void ResumeClicked()
    {
        Events.Publish(new UIRequestCloseEvent(PuaseMenu));
    }
    public void QuitClicked()
    {
        Events.Publish(new UIRequestCloseEvent(PuaseMenu));


        SceneManager.LoadScene(0, LoadSceneMode.Single);

        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            if (obj.transform.parent == null)
            {
                Object.Destroy(obj);
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void SettingsClicked()
    {
        Events.Publish(new UIRequestOpenEvent(SettingsMenu));
    }

}
