using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{

    public GameObject inventory;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            bool isOpen = !inventory.activeSelf;

            inventory.SetActive(isOpen);

           
                Time.timeScale = isOpen ? 0 : 1;


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
}
