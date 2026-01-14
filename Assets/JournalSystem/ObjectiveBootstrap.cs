using UnityEngine;

public class ObjectiveBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Ensure ObjectiveManager exists
        if (ObjectiveManager.Instance == null)
        {
            GameObject managerObj = new GameObject("ObjectiveManager");
            managerObj.AddComponent<ObjectiveManager>();
            Debug.Log("ObjectiveManager created");
        }
    }
}