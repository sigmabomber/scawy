using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfirmationMenu : MonoBehaviour
{
  

    public void AgreedClicked()
    {
        NewGameMenu.Instance.OnConfirmOverwrite();
    }
    public void CancelClicked()
    {
     NewGameMenu.Instance.OnCancelOverwrite();
    }
}
