using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewButtonClicked : MonoBehaviour
{
    protected int ClickedHash;
    public Animator animator;

    public GameObject gameobject;

    public GameObject ReturnObj;


    private void Start()
    {
        ClickedHash = Animator.StringToHash("clicked");

    }


    public void Clicked()
    {
        animator.SetTrigger(ClickedHash);

        gameobject.SetActive(true);
        transform.parent.gameObject.SetActive(false);

        NewGameMenu.Instance.LoadSlots();
        ReturnObj.SetActive(true);



    }

}
