using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButtonClicked : MonoBehaviour
{
    protected int ClickedHash;
    protected int gobackHash;
    public Animator animator;

    public GameObject gameobject;
    public GameObject gameobject2;
    public CameraMouseRotate rotate;

    public GameObject returnButton;

 
    private void Start()
    {
        ClickedHash = Animator.StringToHash("clicked");
        gobackHash = Animator.StringToHash("return");

    }


    public void Clicked()
    {
        animator.SetTrigger(ClickedHash);

        gameobject.SetActive(true);
        transform.parent.gameObject.SetActive(false);

        LoadMenu.Instance.LoadSlots();
        returnButton.gameObject.SetActive(true);
        


    }

    public void GoBack()
    {


        animator.SetTrigger(gobackHash);
        gameobject.SetActive(false);
        returnButton.SetActive(false);
        gameobject2.SetActive(false);
        transform.parent.gameObject.SetActive(true);


    }

}
