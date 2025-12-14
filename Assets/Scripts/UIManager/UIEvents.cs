using UnityEngine;

namespace Doody.Framework.UI
{
    /// <summary>
    /// UI opened event
    /// </summary>
    public class UIOpenedEvent
    {
        public GameObject UIObject { get; private set; }
        public bool PauseGame { get; private set; }
        public UIOpenedEvent(GameObject uiObject, bool pauseGame)
        {
            UIObject = uiObject;
            PauseGame = pauseGame;
        }
    }

    /// <summary>
    /// UI closed event
    /// </summary>
    public class UIClosedEvent
    {
        public GameObject UIObject { get; private set; }
        public bool ClosedByAnotherUI { get; private set; }
        public GameObject ReplacingUI { get; private set; }

        public UIClosedEvent(GameObject uiObject, bool closedByAnotherUI = false, GameObject replacingUI = null)
        {
            UIObject = uiObject;
            ClosedByAnotherUI = closedByAnotherUI;
            ReplacingUI = replacingUI;
        }
    }

    /// <summary>
    /// UI requested to open (before actually opening)
    /// </summary>
    public class UIRequestOpenEvent
    {
        public GameObject UIObject { get; private set; }
        public UIRequestOpenEvent(GameObject uiObject)
        {
            UIObject = uiObject;
        }
    }

    /// <summary>
    /// UI requested to close (before actually closing)
    /// </summary>
    public class UIRequestCloseEvent
    {
        public GameObject UIObject { get; private set; }
        public bool ClosedByAnotherUI { get; private set; }
        public GameObject ReplacingUI { get; private set; }

        public UIRequestCloseEvent(GameObject uiObject, bool closedByAnotherUI = false, GameObject replacingUI = null)
        {
            UIObject = uiObject;
            ClosedByAnotherUI = closedByAnotherUI;
            ReplacingUI = replacingUI;
        }
    }

    public class UIRequestToggleEvent
    {
        public GameObject UIObject { get; private set; }
        public UIRequestToggleEvent(GameObject uiObject)
        {
            UIObject = uiObject;
        }
    }
}