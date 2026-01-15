using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButtonClicked : MonoBehaviour
{
    protected int ClickedHash;
    public Animator animator;

    public GameObject gameobject;


    private void Start()
    {
        ClickedHash = Animator.StringToHash("clicked");
    }


    public void Clicked()
    {
        animator.SetTrigger(ClickedHash);

        gameobject.SetActive(true);
        transform.parent.gameObject.SetActive(false);
    }
}
