using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class KeypadSystem : MonoBehaviour
{
    [Header("Keypad Layout")]
    [HideInInspector] public GameObject key1;
    [HideInInspector] public GameObject key2;
    [HideInInspector] public GameObject key3;
    [HideInInspector] public GameObject key4;
    [HideInInspector] public GameObject key5;
    [HideInInspector] public GameObject key6;
    [HideInInspector] public GameObject key7;
    [HideInInspector] public GameObject key8;
    [HideInInspector] public GameObject key9;
  

    [Header("Settings")]
    public string correctCode = "1234";
    private string currentInput = "";

    public void OnKeyPressed(int keyNumber)
    {
        currentInput += keyNumber.ToString();
        Debug.Log("Current Input: " + currentInput);

        if (currentInput.Length >= correctCode.Length)
        {
            CheckCode();
        }
    }

    private void CheckCode()
    {
        if (currentInput == correctCode)
        {
            Debug.Log("Code Correct!");
            OnCodeCorrect();
        }
        else
        {
            Debug.Log("Code Incorrect!");
            OnCodeIncorrect();
        }

        currentInput = "";
    }

    private void OnCodeCorrect()
    {
    
    }

    private void OnCodeIncorrect()
    {
     
    }

    public void ClearInput()
    {
        currentInput = "";
        Debug.Log("Input cleared");
    }
}