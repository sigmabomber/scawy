using UnityEngine;
using UnityEngine.Rendering;

public class AISetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    [SerializeField] private bool setupOnStart = false;

    [Header("Player Setup")]
    [SerializeField] private GameObject playerObject;

    [Header("AI Setup")]
    [SerializeField] private GameObject[] aiAgents;

    void Start()
    {
        if (setupOnStart)
        {
            QuickSetup();
        }
    }

    [ContextMenu("Quick Setup")]
    void QuickSetup()
    {
        // Setup Player
        if (playerObject != null)
        {
            SetupPlayer(playerObject);
        }

        // Setup AI
        foreach (var ai in aiAgents)
        {
            if (ai != null)
            {
                SetupAI(ai);
            }
        }

        // Add coordinator if not exists
        if (AICoordinator.Instance == null)
        {
            GameObject coordinator = new GameObject("AI Coordinator");
            coordinator.AddComponent<AICoordinator>();
        }

        // Add debug UI if not exists
        if (FindObjectOfType<AIDebugUI>() == null)
        {
            GameObject debugUI = new GameObject("AI Debug UI");
            debugUI.AddComponent<AIDebugUI>();
        }

        Debug.Log("<color=green>Quick Setup Complete!</color>");
    }

    void SetupPlayer(GameObject player)
    {
        if (!player.GetComponent<PlayerSoundManager>())
        {
            player.AddComponent<PlayerSoundManager>();
            Debug.Log($"Added PlayerSoundManager to {player.name}");
        }

        if (!player.GetComponent<SoundEmitter>())
        {
            player.AddComponent<SoundEmitter>();
        }

        // Set player layer
        player.layer = LayerMask.NameToLayer("Player");
    }

    void SetupAI(GameObject ai)
    {
        if (!ai.GetComponent<AI>())
        {
            ai.AddComponent<AI>();
            Debug.Log($"Added AdvancedAI to {ai.name}");
        }

        if (!ai.GetComponent<UnityEngine.AI.NavMeshAgent>())
        {
            ai.AddComponent<UnityEngine.AI.NavMeshAgent>();
        }

        // Set AI layer
        ai.layer = LayerMask.NameToLayer("AI");
    }
}