using Doody.Framework.UI;
using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : EventListener
{
    private GameObject currentUI;

    private void Start()
    {
        Events.Subscribe<UIRequestOpenEvent>(RequestUIOpen, this);
        Events.Subscribe<UIRequestCloseEvent>(RequestUIClose, this);
        Events.Subscribe<UIRequestToggleEvent>(RequestUIToggle, this);
    }

    private void RequestUIOpen(UIRequestOpenEvent data)
    {
        OpenUI(data.UIObject);
    }

    private void RequestUIClose(UIRequestCloseEvent data)
    {
        CloseUI(data.ClosedByAnotherUI, data.ReplacingUI);
    }

    private void RequestUIToggle(UIRequestToggleEvent data)
    {
        if (data.UIObject.activeSelf)
        {
            if (currentUI != data.UIObject)
                currentUI = data.UIObject;
            CloseUI();
        }
        else
        {
            OpenUI(data.UIObject);
        }
    }

    private void OpenUI(GameObject UIObject)
    {
        if (currentUI != null && currentUI != UIObject)
        {
            GameObject previousUI = currentUI;
            CloseUI(true, UIObject);
        }

        currentUI = UIObject;
        currentUI.SetActive(true);
        Time.timeScale = 0;

        Events.Publish(new UIOpenedEvent(currentUI, true));
    }

    private void CloseUI(bool closedByAnotherUI = false, GameObject replacingUI = null)
    {
        if (currentUI == null) return;

        GameObject closingUI = currentUI;
        closingUI.SetActive(false);

        Events.Publish(new UIClosedEvent(closingUI, closedByAnotherUI, replacingUI));

        currentUI = null;
        Time.timeScale = 1;
    }
}