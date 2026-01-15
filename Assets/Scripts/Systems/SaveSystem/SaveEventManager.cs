using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using Doody.GameEvents;


public class SaveEventManager : MonoBehaviour
{
    public static SaveEventManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string encryptionKey = "epstein67";
    [SerializeField] private int quickSaveSlot = 1;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float saveTimeout = 5f;
    [SerializeField] private int maxSaveFileSizeMB = 10;
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float retryDelaySeconds = 0.5f;

    [SerializeField] private int maxSlots = 4;

    [Header("UI Integration")]
    [SerializeField] private bool showUIStatus = true;

    private readonly string backupExtension = ".backup";
    private readonly string tempExtension = ".temp";
    private readonly string corruptExtension = ".corrupt";

    private bool isSaving = false;
    private bool isLoading = false;
    private bool isShuttingDown = false;
    private string saveDirectory;
    private string fallbackSaveDirectory;
    private Dictionary<string, string> collectedSaveData = new Dictionary<string, string>();
    private DateTime saveStartTime;
    private HashSet<string> pendingSystems = new HashSet<string>();
    private Coroutine activeOperation;

    void Awake()
    {
        // Singleton with extra safety
        try
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSaveDirectories();
            InitializeEventSubscriptions();

        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] CRITICAL: Awake failed: {e.Message}\n{e.StackTrace}");
          
        }
    }

    private void InitializeSaveDirectories()
    {
        try
        {
            // Primary save location
            saveDirectory = Path.Combine(Application.persistentDataPath, "GameSaves");

            // Fallback location
            fallbackSaveDirectory = Path.Combine(Application.temporaryCachePath, "GameSaves_Fallback");

            // Create both directories
            CreateDirectorySafe(saveDirectory);
            CreateDirectorySafe(fallbackSaveDirectory);

            // Validate primary directory is writable
            if (!IsDirectoryWritable(saveDirectory))
            {
                Debug.LogWarning("[SaveSystem] Primary directory not writable, swapping to fallback");
                var temp = saveDirectory;
                saveDirectory = fallbackSaveDirectory;
                fallbackSaveDirectory = temp;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Directory initialization failed: {e.Message}");
            // Use temp as last resort
            saveDirectory = Application.temporaryCachePath;
            fallbackSaveDirectory = Application.temporaryCachePath;
        }
    }

    private void InitializeEventSubscriptions()
    {
        try
        {
            Events.Subscribe<SaveDataResponseEvent>(OnSaveDataReceived, this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Event subscription failed: {e.Message}");
        }
    }

    void OnDestroy()
    {
        isShuttingDown = true;

        try
        {
            if (activeOperation != null)
            {
                StopCoroutine(activeOperation);
                activeOperation = null;
            }

            Events.Unsubscribe<SaveDataResponseEvent>(OnSaveDataReceived);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] OnDestroy error: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    void Update()
    {
        if (isShuttingDown) return;

        try
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                StartSafeQuickSave();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                StartSafeQuickLoad();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Update error: {e.Message}");
        }
    }

    // ========== PUBLIC API ==========

    public void StartSafeQuickSave()
    {
        if (isShuttingDown) return;

        try
        {
            if (isSaving || isLoading)
            {
                if (debugMode) Debug.Log("[SaveSystem] Operation in progress, ignoring save request");
                return;
            }

            if (activeOperation != null)
            {
                StopCoroutine(activeOperation);
            }

            activeOperation = StartCoroutine(SafeQuickSaveCoroutine());
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to start save: {e.Message}");
            isSaving = false;
        }
    }

    public void StartSafeQuickLoad()
    {
        if (isShuttingDown) return;

        try
        {
            if (isSaving || isLoading)
            {
                if (debugMode) Debug.Log("[SaveSystem] Operation in progress, ignoring load request");
                return;
            }

            if (activeOperation != null)
            {
                StopCoroutine(activeOperation);
            }

            activeOperation = StartCoroutine(SafeQuickLoadCoroutine());
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to start load: {e.Message}");
            isLoading = false;
        }
    }

    // ========== SAVE/LOAD ==========

    private IEnumerator SafeQuickSaveCoroutine()
    {
        isSaving = true;
        saveStartTime = DateTime.Now;

        Debug.Log("[SaveSystem] Starting quick save...");

        SaveOperationResult result = new SaveOperationResult();

        ShowUISaving();

        // Retry logic
        for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
        {
            if (isShuttingDown) break;

            if (attempt > 0)
            {
                Debug.LogWarning($"[SaveSystem] Retry attempt {attempt + 1}/{maxRetryAttempts}");
                yield return new WaitForSeconds(retryDelaySeconds);
            }

            result = new SaveOperationResult();
            yield return ExecuteWithTimeout(SaveGameCoroutine(quickSaveSlot, result), saveTimeout + 5f);

            if (result.success)
            {
                Debug.Log($"[SaveSystem] Quick save successful: {result.message}");
                break;
            }

            Debug.LogWarning($"[SaveSystem] Save attempt {attempt + 1} failed: {result.message}");
        }

        if (!result.success)
        {
            Debug.LogError($"[SaveSystem] Quick save failed after {maxRetryAttempts} attempts");
            yield return TryRestoreBackup(quickSaveSlot);
        }

        isSaving = false;
        activeOperation = null;
    }

    private IEnumerator SafeQuickLoadCoroutine()
    {
        isLoading = true;

        Debug.Log("[SaveSystem] Starting quick load...");

        LoadOperationResult result = new LoadOperationResult();

        if (!SaveExists(quickSaveSlot))
        {
            Debug.LogWarning("[SaveSystem] No quick save found!");
            ShowUILoadFailed("No save file found");
            isLoading = false;
            activeOperation = null;
            yield break;
        }

        ShowUILoading();

        // Retry logic
        for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
        {
            if (isShuttingDown) break;

            if (attempt > 0)
            {
                Debug.LogWarning($"[SaveSystem] Retry attempt {attempt + 1}/{maxRetryAttempts}");
                yield return new WaitForSeconds(retryDelaySeconds);
            }

            result = new LoadOperationResult();
            yield return ExecuteWithTimeout(LoadGameCoroutine(quickSaveSlot, result), saveTimeout + 10f);

            if (result.success)
            {
                Debug.Log($"[SaveSystem] Quick load successful: {result.message}");
                break;
            }

            Debug.LogWarning($"[SaveSystem] Load attempt {attempt + 1} failed: {result.message}");
        }

        if (!result.success)
        {
            Debug.LogError($"[SaveSystem] Quick load failed after {maxRetryAttempts} attempts");
            ShowUILoadFailed(result.message);
        }

        isLoading = false;
        activeOperation = null;
    }

    // ========== CORE ERROR HANDLING ==========

    private IEnumerator SaveGameCoroutine(int slotNumber, SaveOperationResult result)
    {
        if (isShuttingDown)
        {
            result.success = false;
            result.message = "System shutting down";
            yield break;
        }

        Debug.Log($"[SaveSystem] SaveGameCoroutine: Slot {slotNumber}");

        string filePath = null;
        string tempPath = null;
        string backupPath = null;

        try
        {
            filePath = GetFilePath(slotNumber);
            tempPath = filePath + tempExtension;
            backupPath = filePath + backupExtension;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to get file paths: {e.Message}");
            result.success = false;
            result.message = "Path generation failed";
            yield break;
        }

        // 1. Collect data from systems
        collectedSaveData.Clear();
        pendingSystems.Clear();

        string operationId = Guid.NewGuid().ToString();

        var requestEvent = new SaveDataRequestEvent
        {
            saveSlot = slotNumber,
            operationId = operationId,
            requestTime = DateTime.Now
        };

        Events.Publish(requestEvent);

        // Wait for responses with timeout
        float elapsed = 0f;
        while (elapsed < saveTimeout && !isShuttingDown)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (collectedSaveData.Count == 0)
        {
            Debug.LogWarning("[SaveSystem] No save data collected, saving empty state");
            collectedSaveData["_empty"] = "{}";
        }

        // 2. Create save package
        SavePackage savePackage;
        try
        {
            savePackage = new SavePackage
            {
                saveSlot = slotNumber,
                saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                sceneName = GetCurrentSceneName(),
                totalSystems = collectedSaveData.Count,
                systemsResponded = collectedSaveData.Count,
                operationId = Guid.NewGuid().ToString()
            };

            savePackage.SetSystemData(collectedSaveData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Package creation error: {e.Message}");
            result.success = false;
            result.message = $"Package creation failed: {e.Message}";
            yield break;
        }

        // 3. Serialize to JSON
        string json;
        try
        {
            json = JsonUtility.ToJson(savePackage, false);

            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("Serialization produced empty JSON");
            }

            if (json.Length > maxSaveFileSizeMB * 1024 * 1024)
            {
                Debug.LogWarning($"[SaveSystem] Save data very large: {json.Length / 1024}KB");
                // Continue anyway - let file system handle it
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Serialization error: {e.Message}");
            result.success = false;
            result.message = $"Serialization failed: {e.Message}";
            yield break;
        }

        // 4. Encrypt (with fallback to plain text)
        string dataToSave;
        try
        {
            dataToSave = SimpleEncrypt(json);

            if (string.IsNullOrEmpty(dataToSave))
            {
                Debug.LogWarning("[SaveSystem] Encryption failed, saving as plain text");
                dataToSave = json;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Encryption error (using plain text): {e.Message}");
            dataToSave = json;
        }

        // 5. Atomic file write with comprehensive error handling
        bool writeSuccess = false;
        try
        {
            // Clean up any existing temp files first
            SafeDeleteFile(tempPath);

            // Create backup if file exists
            if (File.Exists(filePath))
            {
                SafeCopyFile(filePath, backupPath);
            }

            // Write to temp file first
            File.WriteAllText(tempPath, dataToSave, Encoding.UTF8);

            // Verify temp file exists and has content
            if (!File.Exists(tempPath))
            {
                throw new IOException("Temp file not created");
            }

            FileInfo tempInfo = new FileInfo(tempPath);
            if (tempInfo.Length == 0)
            {
                throw new IOException("Temp file is empty");
            }

            // Verify content
            string verifyText = File.ReadAllText(tempPath, Encoding.UTF8);
            if (string.IsNullOrEmpty(verifyText) || verifyText != dataToSave)
            {
                throw new IOException("File verification failed - content mismatch");
            }

            // Replace final file with temp file
            SafeReplaceFile(tempPath, filePath);

            // Final verification
            if (!File.Exists(filePath))
            {
                throw new IOException("Final file not created");
            }

            FileInfo finalInfo = new FileInfo(filePath);
            if (finalInfo.Length == 0)
            {
                throw new IOException("Final file is empty");
            }

            writeSuccess = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] File write failed: {e.Message}");
            result.success = false;
            result.message = $"Write failed: {e.Message}";

            // Recovery attempt
            try
            {
                SafeDeleteFile(tempPath);

                if (File.Exists(backupPath))
                {
                    SafeCopyFile(backupPath, filePath);
                    Debug.LogWarning("[SaveSystem] Restored from backup after failure");
                }
                else if (File.Exists(filePath))
                {
                    // Mark as corrupt but don't delete
                    File.Move(filePath, filePath + corruptExtension + DateTime.Now.Ticks);
                }
            }
            catch (Exception recoveryError)
            {
                Debug.LogError($"[SaveSystem] Recovery also failed: {recoveryError.Message}");
            }

            yield break;
        }
        finally
        {
            // Cleanup
            if (writeSuccess)
            {
                SafeDeleteFile(tempPath);
                SafeDeleteFile(backupPath);
            }
        }

        // 6. Success notification
        result.success = true;
        result.message = $"Saved {collectedSaveData.Count} systems";
        result.slotNumber = slotNumber;
        result.filePath = filePath;

        Events.Publish(new SaveOperationCompleteEvent
        {
            saveSlot = slotNumber,
            success = true,
            systemsSaved = collectedSaveData.Count,
            saveTime = DateTime.Parse(savePackage.saveTime),
            operationId = savePackage.operationId
        });

        Debug.Log($"[SaveSystem] Save successful: {collectedSaveData.Count} systems, {json.Length} bytes");
    }

    private IEnumerator LoadGameCoroutine(int slotNumber, LoadOperationResult result)
    {
        if (isShuttingDown)
        {
            result.success = false;
            result.message = "System shutting down";
            yield break;
        }

        Debug.Log($"[SaveSystem] LoadGameCoroutine: Slot {slotNumber}");

        string filePath;
        string backupPath;

        // 1. Get paths
        try
        {
            filePath = GetFilePath(slotNumber);
            backupPath = filePath + backupExtension;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to get file paths: {e.Message}");
            result.success = false;
            result.message = "Path generation failed";
            yield break;
        }

        // 2. Read file
        string encrypted;
        try
        {
            if (!File.Exists(filePath))
            {
                if (File.Exists(backupPath))
                {
                    Debug.LogWarning("[SaveSystem] Using backup file");
                    SafeCopyFile(backupPath, filePath);
                }
                else
                {
                    throw new FileNotFoundException("Save file and backup not found");
                }
            }

            encrypted = File.ReadAllText(filePath, Encoding.UTF8);

            if (string.IsNullOrEmpty(encrypted))
                throw new InvalidDataException("Save file is empty");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] File read error: {e.Message}");
            result.success = false;
            result.message = $"Read failed: {e.Message}";
            yield break;
        }

        // 3. Decrypt
        string json;
        try
        {
            json = SimpleDecrypt(encrypted);

            if (string.IsNullOrEmpty(json))
                json = encrypted;
        }
        catch
        {
            json = encrypted;
        }

        // 4. Deserialize
        SavePackage savePackage;
        Dictionary<string, string> systemData;

        try
        {
            savePackage = JsonUtility.FromJson<SavePackage>(json);

            if (savePackage == null)
                throw new InvalidDataException("Failed to parse save data");

            savePackage.systemNames ??= Array.Empty<string>();
            savePackage.systemDataArray ??= Array.Empty<string>();

            if (savePackage.systemNames.Length != savePackage.systemDataArray.Length)
            {
                int min = Mathf.Min(savePackage.systemNames.Length, savePackage.systemDataArray.Length);
                Array.Resize(ref savePackage.systemNames, min);
                Array.Resize(ref savePackage.systemDataArray, min);
            }

            systemData = savePackage.GetSystemData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Deserialization error: {e.Message}");
            result.success = false;
            result.message = $"Parse failed: {e.Message}";
            yield break;
        }

        // 5. Scene decision (NO yielding here)
        bool needsSceneLoad = false;
        string targetScene = null;

        try
        {
            string currentScene = GetCurrentSceneName();

            if (!string.IsNullOrEmpty(savePackage.sceneName) &&
                savePackage.sceneName != currentScene)
            {
                needsSceneLoad = true;
                targetScene = savePackage.sceneName;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Scene prep error: {e.Message}");
            result.success = false;
            result.message = $"Scene prep error: {e.Message}";
            yield break;
        }

        // 6. Scene loading (YIELDS ONLY HERE)
        if (needsSceneLoad)
        {
            Debug.Log($"[SaveSystem] Loading scene: {targetScene}");

            var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetScene);

            if (asyncOp == null)
            {
                result.success = false;
                result.message = "Scene load failed to start";
                yield break;
            }

            asyncOp.allowSceneActivation = true;

            float elapsed = 0f;
            const float timeout = 30f;

            while (!asyncOp.isDone && elapsed < timeout && !isShuttingDown)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!asyncOp.isDone)
            {
                result.success = false;
                result.message = "Scene load timeout";
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        // 7. Send data
        if (systemData != null && systemData.Count > 0)
        {
            Events.Publish(new LoadDataEvent
            {
                saveSlot = slotNumber,
                systemData = systemData,
                saveTime = DateTime.Parse(savePackage.saveTime),
                operationId = savePackage.operationId
            });

            yield return new WaitForSeconds(0.1f);
        }

        // 8. Success
        result.success = true;
        result.message = $"Loaded {systemData?.Count ?? 0} systems";
        result.slotNumber = slotNumber;

        Events.Publish(new LoadOperationCompleteEvent
        {
            saveSlot = slotNumber,
            success = true,
            systemsLoaded = systemData?.Count ?? 0,
            saveTime = DateTime.Parse(savePackage.saveTime),
            operationId = savePackage.operationId
        });

        Debug.Log($"[SaveSystem] Load successful: {systemData?.Count ?? 0} systems");
    }

    // ========== EVENT HANDLER ==========

    private void OnSaveDataReceived(SaveDataResponseEvent response)
    {
        try
        {
            if (!isSaving || string.IsNullOrEmpty(response.systemName)) return;

            pendingSystems.Remove(response.systemName);

            if (!collectedSaveData.ContainsKey(response.systemName))
            {
                string data = response.saveData ?? "{}";
                collectedSaveData[response.systemName] = data;

                if (debugMode)
                {
                    Debug.Log($"[SaveSystem] Response from {response.systemName} ({data.Length} chars)");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] OnSaveDataReceived error: {e.Message}");
        }
    }

    // ========== RECOVERY METHODS ==========

    private IEnumerator TryRestoreBackup(int slotNumber)
    {
        bool restored = false;

        string filePath = GetFilePath(slotNumber);
        string backupPath = filePath + backupExtension;

        if (File.Exists(backupPath))
        {
            Debug.LogWarning($"[SaveSystem] Attempting backup restore for slot {slotNumber}...");

            SafeCopyFile(backupPath, filePath);
            Debug.Log("[SaveSystem] Backup restored successfully");

            restored = SaveExists(slotNumber);
        }
        else
        {
            Debug.LogWarning("[SaveSystem] No backup available for recovery");
        }

        // Yield OUTSIDE all logic
        if (restored)
        {
            yield return new WaitForSeconds(1f);
            StartSafeQuickLoad();
        }
    }

    // ========== UI INTEGRATION ==========

    private void ShowUISaving()
    {
        try
        {
            if (showUIStatus && SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowSaving();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] UI update error: {e.Message}");
        }
    }

    private void ShowUILoading()
    {
        try
        {
            if (showUIStatus && SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowLoading();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] UI update error: {e.Message}");
        }
    }

    private void ShowUILoadFailed(string error)
    {
        try
        {
            if (showUIStatus && SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowLoadFailed(error);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] UI update error: {e.Message}");
        }
    }

    // ========== SAFE FILE OPERATIONS ==========

    private void SafeReplaceFile(string sourcePath, string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(sourcePath, destinationPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] File replace error: {e.Message}");
            throw;
        }
    }

    private void SafeCopyFile(string sourcePath, string destinationPath)
    {
        try
        {
            byte[] data = File.ReadAllBytes(sourcePath);

            if (data == null || data.Length == 0)
            {
                throw new IOException("Source file is empty or unreadable");
            }

            File.WriteAllBytes(destinationPath, data);

            // Verify copy
            FileInfo destInfo = new FileInfo(destinationPath);
            if (destInfo.Length != data.Length)
            {
                throw new IOException("Copy verification failed - size mismatch");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] File copy error: {e.Message}");
            throw;
        }
    }

    private void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Silently ignore deletion errors
        }
    }

    private void CreateDirectorySafe(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to create directory {path}: {e.Message}");
        }
    }

    private bool IsDirectoryWritable(string path)
    {
        try
        {
            string testFile = Path.Combine(path, $"_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetCurrentSceneName()
    {
        try
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        catch
        {
            return "Unknown";
        }
    }

    // ========== HELPER METHODS ==========

    public bool SaveExists(int slotNumber)
    {
        try
        {
            string filePath = GetFilePath(slotNumber);
            return File.Exists(filePath) || File.Exists(filePath + backupExtension);
        }
        catch
        {
            return false;
        }
    }

    public SaveSlotInfo GetSaveInfo(int slotNumber)
    {
        var info = new SaveSlotInfo
        {
            slotNumber = slotNumber,
            exists = false,
            saveTime = "N/A",
            fileSizeKB = 0
        };

        try
        {
            string filePath = GetFilePath(slotNumber);
            string primaryPath = File.Exists(filePath) ? filePath : filePath + backupExtension;

            if (File.Exists(primaryPath))
            {
                var fileInfo = new FileInfo(primaryPath);
                info.exists = true;
                info.fileSizeKB = (int)(fileInfo.Length / 1024);

                try
                {
                    string encrypted = File.ReadAllText(primaryPath, Encoding.UTF8);
                    string json = SimpleDecrypt(encrypted);
                    var package = JsonUtility.FromJson<SavePackage>(json);
                    if (package != null && !string.IsNullOrEmpty(package.saveTime))
                    {
                        info.saveTime = package.saveTime;
                    }
                    else
                    {
                        info.saveTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                catch
                {
                    info.saveTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] GetSaveInfo error for slot {slotNumber}: {e.Message}");
        }

        return info;
    }

    public void DeleteSave(int slotNumber)
    {
        try
        {
            string filePath = GetFilePath(slotNumber);
            SafeDeleteFile(filePath);
            SafeDeleteFile(filePath + backupExtension);
            SafeDeleteFile(filePath + tempExtension);

            Debug.Log($"[SaveSystem] Deleted save slot {slotNumber}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to delete save: {e.Message}");
        }
    }

    public void ValidateAllSaves()
    {
        try
        {
            Debug.Log("[SaveSystem] Validating all saves...");

            for (int i = 1; i <= maxSlots; i++)
            {
                if (SaveExists(i))
                {
                    try
                    {
                        string filePath = GetFilePath(i);
                        string encrypted = File.ReadAllText(filePath, Encoding.UTF8);
                        string json = SimpleDecrypt(encrypted);
                        var package = JsonUtility.FromJson<SavePackage>(json);

                        if (package != null)
                        {
                            Debug.Log($"[SaveSystem] Slot {i}: OK - {package.systemsResponded} systems, {package.saveTime}");
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveSystem] Slot {i}: CORRUPT - Failed to parse");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveSystem] Slot {i}: ERROR - {e.Message}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] ValidateAllSaves error: {e.Message}");
        }
    }

    public bool AnySaveExists()
    {
        for (int i = 1; i <= maxSlots; i++)
        {
            if (SaveExists(i))
            {
                return true;
            }
        }
        return false;
    }

    private string GetFilePath(int slotNumber)
    {
        try
        {
            return Path.Combine(saveDirectory, $"save_{slotNumber:000}.dat");
        }
        catch
        {
            return Path.Combine(Application.temporaryCachePath, $"save_{slotNumber:000}.dat");
        }
    }

    private string SimpleEncrypt(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        try
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

            for (int i = 0; i < textBytes.Length; i++)
            {
                textBytes[i] = (byte)(textBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(textBytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Encryption failed: {e.Message}");
            return text;
        }
    }

    private string SimpleDecrypt(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        try
        {
            byte[] textBytes = Convert.FromBase64String(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

            for (int i = 0; i < textBytes.Length; i++)
            {
                textBytes[i] = (byte)(textBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(textBytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Decryption failed (may be plain text): {e.Message}");
            return text;
        }
    }

    // ========== TIMEOUT UTILITY ==========

    private IEnumerator ExecuteWithTimeout(IEnumerator operation, float timeout)
    {
        float elapsed = 0f;
        bool completed = false;

        while (elapsed < timeout && !isShuttingDown)
        {
            bool moveNext = operation.MoveNext();

            if (!moveNext)
            {
                completed = true;
                break;
            }

            yield return operation.Current;
            elapsed += Time.deltaTime;
        }

        if (!completed && elapsed >= timeout)
        {
            Debug.LogWarning($"[SaveSystem] Operation timeout after {timeout}s");
        }
    }
}

// ========== EVENT DEFINITIONS ==========

[System.Serializable]
public class SaveDataRequestEvent
{
    public int saveSlot;
    public string operationId;
    public DateTime requestTime;
    public int expectedSystems = 0;
}

[System.Serializable]
public class SaveDataResponseEvent
{
    public string systemName;
    public string saveData;
    public int totalSystems = 1;
    public DateTime responseTime;
    public string operationId;
    public bool success = true;
}

[System.Serializable]
public class LoadDataEvent
{
    public int saveSlot;
    public Dictionary<string, string> systemData;
    public DateTime saveTime;
    public string operationId;
}

[System.Serializable]
public class SaveOperationCompleteEvent
{
    public int saveSlot;
    public bool success;
    public int systemsSaved;
    public string errorMessage;
    public DateTime saveTime;
    public string operationId;
}

[System.Serializable]
public class LoadOperationCompleteEvent
{
    public int saveSlot;
    public bool success;
    public int systemsLoaded;
    public string errorMessage;
    public DateTime saveTime;
    public string operationId;
}

[System.Serializable]
public class SaveOperationResult
{
    public bool success;
    public string message;
    public int slotNumber;
    public string filePath;
    public DateTime timestamp = DateTime.Now;
}

[System.Serializable]
public class LoadOperationResult
{
    public bool success;
    public string message;
    public int slotNumber;
    public DateTime timestamp = DateTime.Now;
}

[System.Serializable]
public class SavePackage
{
    public int saveSlot;
    public string saveTime;
    public string sceneName;
    public string[] systemNames;
    public string[] systemDataArray;
    public int totalSystems;
    public int systemsResponded;
    public string gameVersion;
    public string operationId;
    public int checksum;

    public SavePackage()
    {
        try
        {
            gameVersion = Application.version;
            operationId = Guid.NewGuid().ToString();
        }
        catch
        {
            gameVersion = "Unknown";
            operationId = DateTime.Now.Ticks.ToString();
        }
    }

    public Dictionary<string, string> GetSystemData()
    {
        var dict = new Dictionary<string, string>();

        try
        {
            if (systemNames != null && systemDataArray != null)
            {
                int count = Mathf.Min(systemNames.Length, systemDataArray.Length);
                for (int i = 0; i < count; i++)
                {
                    if (!string.IsNullOrEmpty(systemNames[i]))
                    {
                        dict[systemNames[i]] = systemDataArray[i] ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SavePackage] GetSystemData error: {e.Message}");
        }

        return dict;
    }

    public void SetSystemData(Dictionary<string, string> data)
    {
        try
        {
            if (data == null || data.Count == 0)
            {
                systemNames = new string[0];
                systemDataArray = new string[0];
                return;
            }

            systemNames = new string[data.Count];
            systemDataArray = new string[data.Count];

            int index = 0;
            foreach (var kvp in data)
            {
                systemNames[index] = kvp.Key ?? $"System_{index}";
                systemDataArray[index] = kvp.Value ?? string.Empty;
                index++;
            }

            checksum = CalculateChecksum();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SavePackage] SetSystemData error: {e.Message}");
            systemNames = new string[0];
            systemDataArray = new string[0];
        }
    }

    private int CalculateChecksum()
    {
        try
        {
            int sum = 0;
            if (systemDataArray != null)
            {
                foreach (var data in systemDataArray)
                {
                    if (data != null)
                    {
                        sum += data.Length;
                    }
                }
            }
            return sum;
        }
        catch
        {
            return 0;
        }
    }
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotNumber;
    public bool exists;
    public string saveTime;
    public int fileSizeKB;
    public bool hasBackup;
    public string status = "Unknown";
}