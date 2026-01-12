using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveCrashDetector : MonoBehaviour
{
    private List<string> crashLog = new List<string>();

    void Awake()
    {
        // Log all Awake calls
        crashLog.Add($"Awake: {gameObject.name} - {Time.time}");
    }

    void OnEnable()
    {
        crashLog.Add($"OnEnable: {gameObject.name} - {Time.time}");
    }

    void OnDisable()
    {
        crashLog.Add($"OnDisable: {gameObject.name} - {Time.time}");
    }

    void OnDestroy()
    {
        crashLog.Add($"OnDestroy: {gameObject.name} - {Time.time}");

        // Write crash log to file
        string logPath = Path.Combine(Application.persistentDataPath, "crash_log.txt");
        File.WriteAllLines(logPath, crashLog);
    }

    void Update()
    {
        // Monitor F5/F9
        if (Input.GetKeyDown(KeyCode.F5))
        {
            crashLog.Add($"F5 pressed at {Time.time}");
            LogSaveSystemState();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            crashLog.Add($"F9 pressed at {Time.time}");
            LogSaveSystemState();
        }
    }

    void LogSaveSystemState()
    {
        try
        {
            crashLog.Add($"SaveEventManager exists: {SaveEventManager.Instance != null}");

            if (SaveEventManager.Instance != null)
            {
                crashLog.Add($"Save exists: {SaveEventManager.Instance.SaveExists(1)}");
            }
        }
        catch (Exception e)
        {
            crashLog.Add($"LogSaveSystemState ERROR: {e.Message}");
        }
    }
}