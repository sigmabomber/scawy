
using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;

[RequireComponent(typeof(SoundEmitter))]
public class PlayerSoundManagerr : EventListener
{
    [Header("Sound Settings")]
    [SerializeField] private float walkSoundRadius = 5f;
    [SerializeField] private float sprintSoundRadius = 15f;
    [SerializeField] private float crouchSoundRadius = 0f;
    [SerializeField] private float soundInterval = 0.5f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float soundTimer;
    private bool isDetected;
    private int detectingAICount = 0;

    void Awake()
    {
        


        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Listen to detection events
        Listen<TargetDetectedEvent>(OnTargetDetected);
        Listen<TargetLostEvent>(OnTargetLost);
    }

    void Update()
    {
        HandleSoundGeneration();

        if (showDebugInfo)
        {
            
        }
    }

    void HandleSoundGeneration()
    {
        if (characterController == null || playerController == null)
            return;

        // Only make sound when moving
        if (characterController.velocity.magnitude > 0.1f)
        {
            soundTimer += Time.deltaTime;

            if (soundTimer >= soundInterval)
            {
                float radius;
            

                // Determine sound based on movement state
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
                {
                    // Crouching - silent
                    radius = crouchSoundRadius;
                 
                }
                else if (Input.GetKey(KeyCode.LeftShift) && playerController.canSprint)
                {
                    // Sprinting - loud
                    radius = sprintSoundRadius;
          
                }
                else
                {
                    // Walking - normal
                    radius = walkSoundRadius;
                
                }

                if (radius > 0)
                {
                  
                }

                soundTimer = 0f;
            }
        }
    }

    void OnTargetDetected(TargetDetectedEvent evt)
    {
        if (evt.Target == gameObject)
        {
            detectingAICount++;
            isDetected = true;
            Debug.Log($"<color=red>DETECTED by {evt.AI.name}! ({detectingAICount} AI detecting)</color>");
        }
    }

    void OnTargetLost(TargetLostEvent evt)
    {
        if (evt.LastTarget == gameObject)
        {
            detectingAICount = Mathf.Max(0, detectingAICount - 1);
            if (detectingAICount == 0)
            {
                isDetected = false;
                Debug.Log("<color=green>Lost detection - safe again!</color>");
            }
        }
    }

    private void OnGUI()
    
        
    
    {
        if (!showDebugInfo) return;
        

        
        string movementState = "WALKING";
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            movementState = "CROUCHING (Silent)";
        else if (Input.GetKey(KeyCode.LeftShift) && playerController.canSprint)
            movementState = "SPRINTING (Loud)";

        string detectionStatus = isDetected ?
            $"<color=red>DETECTED ({detectingAICount} AI)</color>" :
            "<color=green>UNDETECTED</color>";

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30), $"Movement: {movementState}", style);
        GUI.Label(new Rect(10, 35, 400, 30), detectionStatus, style);
        GUI.Label(new Rect(10, 60, 400, 30),
            $"Stamina: {playerController.GetStaminaPercentage() * 100:F0}%", style);
    }

    public bool IsDetected() => isDetected;
    public int GetDetectingAICount() => detectingAICount;
}
