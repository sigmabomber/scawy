using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Screamer : BaseAI
{
    [Header("Hearing Settings")]
    [SerializeField] private float baseHearingRange = 25f;
    [SerializeField] private AnimationCurve hearingFalloff = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private LayerMask soundObstructionMask = 1 << 0; // Default layer only
    
    [Header("Sound Reactivity")]
    [SerializeField] private float minHearableStrength = 0.1f;
    [SerializeField] private float playerSoundMultiplier = 1.5f;
    [SerializeField] private float alertStateHearingBoost = 1.3f;
    
    [Header("Sound Types & Priorities")]
    [SerializeField] private SoundTypePriority[] soundPriorities = new SoundTypePriority[]
    {
        new SoundTypePriority { soundTag = "footstep", priority = 0.5f, rangeMultiplier = 0.8f },
        new SoundTypePriority { soundTag = "footstep_run", priority = 0.8f, rangeMultiplier = 1.2f },
        new SoundTypePriority { soundTag = "footstep_crouch", priority = 0.3f, rangeMultiplier = 0.5f },
        new SoundTypePriority { soundTag = "jump", priority = 0.7f, rangeMultiplier = 1.0f },
        new SoundTypePriority { soundTag = "door", priority = 0.9f, rangeMultiplier = 1.5f },
        new SoundTypePriority { soundTag = "gunshot", priority = 1.0f, rangeMultiplier = 2.0f },
        new SoundTypePriority { soundTag = "voice", priority = 0.9f, rangeMultiplier = 1.5f },
        new SoundTypePriority { soundTag = "item", priority = 0.6f, rangeMultiplier = 0.9f }
    };
    
    [System.Serializable]
    private class SoundTypePriority
    {
        public string soundTag;
        public float priority;     // 0-1, how important this sound is
        public float rangeMultiplier; // How far this sound travels
    }
    
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float normalMoveSpeed = 3.5f;
    [SerializeField] private float rotationThreshold = 5f;
    
    [Header("Charge Attack")]
    [SerializeField] private float chargeDuration = 3f;
    [SerializeField] private float chargeStopDistance = 1f;
    [SerializeField] private float chargeRecoveryTime = 1f;
    [SerializeField] private float headbuttDamage = 30f;
    [SerializeField] private float headbuttRange = 2f;
    [SerializeField] private float collisionAttackCooldown = 1f;
    
    [Header("Idle Behavior")]
    [SerializeField] private bool shouldWander = false;
    [SerializeField] private float idleRotationSpeed = 15f;
    [SerializeField] private float idleMoveInterval = 8f;
    [SerializeField] private float wanderRadius = 10f;
    
    [Header("Health System")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Transform weakSpot;
    [SerializeField] private float weakSpotMultiplier = 2f;
    
    [Header("Collision Detection")]
    [SerializeField] private LayerMask attackLayers;
    
    // Sound memory
    private class RememberedSound
    {
        public Vector3 position;
        public float strength;
        public string tag;
        public GameObject source;
        public float timeHeard;
        public float priority;
        public bool isPlayerSound;
    }
    
    private List<RememberedSound> soundMemory = new List<RememberedSound>();
    private float soundMemoryDuration = 8f;
    private float soundCleanupInterval = 2f;
    private float lastSoundCleanupTime;
    
    // Current state
    private float currentHealth;
    private Vector3 targetSoundPosition;
    private float soundMemoryTimer;
    private bool isCharging;
    private bool isRecovering;
    private Vector3 idlePosition;
    private float idleTimer;
    private float lastCollisionAttackTime;
    private GameObject soundSource;
    private Dictionary<string, SoundTypePriority> soundPriorityLookup;

    // Override to return our normal speed instead of agent.speed
    protected override float normalSpeed => normalMoveSpeed;

    protected override void InitializeAI()
    {
        currentState = AIState.Patrolling;
        previousState = AIState.Patrolling;
        idlePosition = transform.position;
        currentHealth = maxHealth;
        currentPatrolIndex = 0;

        // Initialize sound priority lookup
        soundPriorityLookup = new Dictionary<string, SoundTypePriority>();
        foreach (var priority in soundPriorities)
        {
            soundPriorityLookup[priority.soundTag.ToLower()] = priority;
        }

        // Critical: Disable NavMeshAgent rotation - we handle it manually
        agent.speed = normalMoveSpeed;
        agent.angularSpeed = 0;
        agent.acceleration = 15f;
        agent.updateRotation = false;
        agent.autoBraking = true;

        if (HasPatrolPoints())
        {
            GoToNextPatrolPoint();
        }
        
        Debug.Log($"{gameObject.name}: Screamer initialized with hearing range {baseHearingRange}m");
    }

    protected override void UpdateState()
    {
        // Always increment state timer
        stateTimer += Time.deltaTime;

        // Handle sound memory
        if (soundMemoryTimer > 0)
        {
            soundMemoryTimer -= Time.deltaTime;
            if (soundMemoryTimer <= 0)
            {
                OnSoundForgotten();
            }
        }

        // Clean up old sounds periodically
        if (Time.time - lastSoundCleanupTime > soundCleanupInterval)
        {
            CleanupOldSounds();
            lastSoundCleanupTime = Time.time;
        }

        // Execute state-specific behavior
        switch (currentState)
        {
            case AIState.Patrolling:
                if (HasPatrolPoints())
                {
                    HandlePatrolling();
                }
                else
                {
                    HandleIdle();
                }
                break;
            case AIState.Investigating:
                HandleInvestigating();
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
            case AIState.Searching:
                HandleSearching();
                break;
        }
    }

    protected override void CheckForTargets()
    {
        if (isCharging && !isRecovering)
        {
            CheckDirectTargetDetection();
        }
    }

    #region Hearing System

    private void OnEnable()
    {
        // Subscribe to sound events from SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.OnSoundPlayed += OnSoundDetected;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from sound events
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.OnSoundPlayed -= OnSoundDetected;
        }
    }

    /// <summary>
    /// Main method that handles all sounds from the SoundManager
    /// </summary>
    private void OnSoundDetected(SoundInfo soundInfo)
    {
        // Don't react to our own sounds
        if (soundInfo.source == gameObject) return;
        
        // Calculate if we can hear this sound
        float hearableStrength = CalculateHearableStrength(soundInfo);
        
        if (hearableStrength >= minHearableStrength)
        {
            // Remember this sound
            RememberSound(soundInfo, hearableStrength);
            
            // React to it
            ReactToSound(soundInfo, hearableStrength);
        }
    }

    /// <summary>
    /// Calculate how well we can hear this specific sound
    /// </summary>
    private float CalculateHearableStrength(SoundInfo soundInfo)
    {
        // Get distance to sound
        float distance = Vector3.Distance(transform.position, soundInfo.position);
        
        // Get sound type priority
        float rangeMultiplier = 1f;
        float priority = 0.5f;
        
        if (soundPriorityLookup.TryGetValue(soundInfo.soundTag.ToLower(), out SoundTypePriority soundPriority))
        {
            rangeMultiplier = soundPriority.rangeMultiplier;
            priority = soundPriority.priority;
        }
        
        // Calculate effective hearing range for this sound
        float effectiveRange = baseHearingRange * rangeMultiplier;
        
        // Apply state boost
        if (currentState == AIState.Alerted || currentState == AIState.Chasing || currentState == AIState.Searching)
        {
            effectiveRange *= alertStateHearingBoost;
        }
        
        // Check if sound is within range
        if (distance > effectiveRange)
        {
            return 0f; // Too far to hear
        }
        
        // Check for obstructions
        float obstructionFactor = CheckSoundObstruction(soundInfo.position);
        if (obstructionFactor <= 0.05f)
        {
            return 0f; // Completely blocked
        }
        
        // Calculate falloff based on distance (closer = louder)
        float distanceRatio = distance / effectiveRange;
        float falloff = hearingFalloff.Evaluate(distanceRatio);
        
        // Base strength from sound info
        float baseStrength = soundInfo.strength * priority * falloff * obstructionFactor;
        
        // Boost player sounds
        if (soundInfo.isPlayerSound)
        {
            baseStrength *= playerSoundMultiplier;
        }
        
        return Mathf.Clamp01(baseStrength);
    }

    /// <summary>
    /// Check if there are walls/obstacles between us and the sound
    /// </summary>
    private float CheckSoundObstruction(Vector3 soundPosition)
    {
        Vector3 fromPosition = transform.position + Vector3.up * 1f; // Ear height
        Vector3 toPosition = soundPosition + Vector3.up * 1f;
        Vector3 direction = toPosition - fromPosition;
        float distance = direction.magnitude;
        
        RaycastHit hit;
        if (Physics.Raycast(fromPosition, direction.normalized, out hit, distance, soundObstructionMask))
        {
            // Something is blocking the sound
            // Return a value between 0.1 and 1 based on how much is blocked
            float obstructionDistance = hit.distance;
            float ratio = obstructionDistance / distance;
            
            // Sounds through walls are muffled but not completely silent
            return Mathf.Lerp(0.1f, 1f, ratio);
        }
        
        return 1f; // No obstruction
    }

    /// <summary>
    /// Store the sound in memory
    /// </summary>
    private void RememberSound(SoundInfo soundInfo, float hearableStrength)
    {
        RememberedSound memory = new RememberedSound
        {
            position = soundInfo.position,
            strength = hearableStrength,
            tag = soundInfo.soundTag,
            source = soundInfo.source,
            timeHeard = Time.time,
            priority = GetSoundPriority(soundInfo.soundTag),
            isPlayerSound = soundInfo.isPlayerSound
        };
        
        soundMemory.Add(memory);
        
        // Keep memory manageable
        if (soundMemory.Count > 20)
        {
            // Remove oldest sound
            soundMemory.RemoveAt(0);
        }
        
        // Debug log
        if (hearableStrength > 0.3f)
        {
            string sourceName = soundInfo.source ? soundInfo.source.name : "unknown";
            Debug.Log($"{gameObject.name}: Heard {soundInfo.soundTag} ({hearableStrength:F2}) from {sourceName} at {Vector3.Distance(transform.position, soundInfo.position):F1}m");
        }
    }

    /// <summary>
    /// Decide how to react to a sound
    /// </summary>
    private void ReactToSound(SoundInfo soundInfo, float hearableStrength)
    {
        // Don't react if we're recovering from a charge
        if (isRecovering) return;
        
        // Don't interrupt charging for weak sounds
        if (isCharging && hearableStrength < 0.7f) return;
        
        // Update our target sound position
        targetSoundPosition = soundInfo.position;
        soundMemoryTimer = Mathf.Lerp(2f, soundMemoryDuration, hearableStrength);
        
        if (soundInfo.source != null)
        {
            soundSource = soundInfo.source;
        }
        
        // Determine reaction based on sound strength and current state
        if (hearableStrength >= 0.7f)
        {
            // Loud sound - immediate investigation or charge
            if (!isCharging)
            {
                if (soundInfo.isPlayerSound)
                {
                    // Player sound - start charge immediately if we're facing roughly the right direction
                    Vector3 directionToSound = (targetSoundPosition - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToSound);
                    
                    if (angle < 60f) // Within 60 degree cone
                    {
                        StartCharge();
                    }
                    else
                    {
                        ChangeState(AIState.Investigating);
                        agent.ResetPath();
                    }
                }
                else
                {
                    ChangeState(AIState.Investigating);
                    agent.ResetPath();
                }
            }
        }
        else if (hearableStrength >= 0.3f && currentState == AIState.Patrolling)
        {
            // Moderate sound - investigate if we're just patrolling
            ChangeState(AIState.Investigating);
            agent.ResetPath();
        }
        // Weak sounds (< 0.3) are remembered but don't cause immediate state change
    }

    /// <summary>
    /// Get priority for a sound type
    /// </summary>
    private float GetSoundPriority(string soundTag)
    {
        if (soundPriorityLookup.TryGetValue(soundTag.ToLower(), out SoundTypePriority priority))
        {
            return priority.priority;
        }
        return 0.5f; // Default
    }

    /// <summary>
    /// Remove old sounds from memory
    /// </summary>
    private void CleanupOldSounds()
    {
        float currentTime = Time.time;
        
        for (int i = soundMemory.Count - 1; i >= 0; i--)
        {
            float age = currentTime - soundMemory[i].timeHeard;
            
            // Sounds fade from memory after a while
            if (age > soundMemoryDuration)
            {
                soundMemory.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Find the most recent/important sound in memory
    /// </summary>
    private RememberedSound GetMostImportantSound()
    {
        if (soundMemory.Count == 0) return null;
        
        RememberedSound bestSound = null;
        float bestScore = 0f;
        
        foreach (var sound in soundMemory)
        {
            float age = Time.time - sound.timeHeard;
            float ageFactor = Mathf.Clamp01(1f - (age / soundMemoryDuration));
            
            // Score = priority * strength * recency
            float score = sound.priority * sound.strength * ageFactor;
            
            if (sound.isPlayerSound)
            {
                score *= 2f; // Player sounds are more important
            }
            
            if (score > bestScore)
            {
                bestScore = score;
                bestSound = sound;
            }
        }
        
        return bestSound;
    }

    #endregion

    #region Movement & Rotation Core

    private void RotateTowardsMovement(float speedMultiplier = 1f)
    {
        if (!IsAgentReady() || !agent.hasPath) return;

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * speedMultiplier * Time.deltaTime
                );
            }
        }
    }

    private void RotateTowardsTarget(Vector3 targetPosition, float speedMultiplier = 1f)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * speedMultiplier * Time.deltaTime
            );
        }
    }

    private void RotateInPlace()
    {
        transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime);
    }

    #endregion

    #region State: Patrolling

    void HandlePatrolling()
    {
        if (!IsAgentReady()) return;

        // Ensure we're at normal speed
        if (agent.speed != normalMoveSpeed)
        {
            agent.speed = normalMoveSpeed;
        }

        // Rotate towards movement direction while moving
        RotateTowardsMovement();

        // Check if reached patrol point
        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            // Wait at patrol point and rotate in place
            if (stateTimer < patrolWaitTime)
            {
                RotateInPlace();
            }
            else
            {
                // Move to next patrol point
                GoToNextPatrolPoint();
                stateTimer = 0f;
            }
        }
    }

    #endregion

    #region State: Idle & Wandering

    void HandleIdle()
    {
        // Ensure we're at normal speed
        if (agent.speed != normalMoveSpeed)
        {
            agent.speed = normalMoveSpeed;
        }

        if (shouldWander && !HasPatrolPoints())
        {
            idleTimer += Time.deltaTime;

            if (agent.hasPath)
            {
                RotateTowardsMovement();

                if (HasReachedDestination(1.5f))
                {
                    agent.ResetPath();
                    idleTimer = idleMoveInterval - 2f;
                }
            }
            else
            {
                RotateInPlace();

                if (idleTimer >= idleMoveInterval)
                {
                    WanderToNewPosition();
                    idleTimer = 0f;
                }
            }
        }
        else
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            RotateInPlace();
        }
    }

    void WanderToNewPosition()
    {
        Vector3 randomPoint = GetRandomPointInRadius(idlePosition, wanderRadius);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    #endregion

    #region State: Investigating (Rotating to Sound)

    void HandleInvestigating()
    {
        if (!IsAgentReady()) return;

        // Stop any movement
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        // Rotate towards sound position
        RotateTowardsTarget(targetSoundPosition);

        // Check if facing the sound
        Vector3 directionToSound = (targetSoundPosition - transform.position).normalized;
        directionToSound.y = 0;
        float angleToTarget = Vector3.Angle(transform.forward, directionToSound);

        if (angleToTarget <= rotationThreshold)
        {
            StartCharge();
        }
        
        // If we've been investigating too long without hearing anything new
        if (stateTimer > 5f)
        {
            // Check if there are any recent sounds in memory
            RememberedSound recentSound = GetMostImportantSound();
            if (recentSound != null && (Time.time - recentSound.timeHeard) < 3f)
            {
                // Update to most recent sound
                targetSoundPosition = recentSound.position;
                soundMemoryTimer = 3f;
                stateTimer = 0f;
            }
            else
            {
                // Give up and search
                ChangeState(AIState.Searching);
            }
        }
    }

    void StartCharge()
    {
        isCharging = true;
        agent.speed = chargeSpeed;

        agent.SetDestination(targetSoundPosition);
        ChangeState(AIState.Chasing);

        if (soundSource != null)
        {
            currentTarget = soundSource;
            hasDetectedTarget = true;
            returnPoint = transform.position;
            lastKnownPosition = targetSoundPosition;

            if (canRaiseAlerts)
            {
                RaiseAlert(AlertLevel.Medium);
            }
        }

        Debug.Log($"{gameObject.name}: Starting charge towards sound!");
    }

    #endregion

    #region State: Chasing (Charging)

    void HandleChasing()
    {
        if (!IsAgentReady()) return;

        // Ensure we're at charge speed
        if (agent.speed != chargeSpeed)
        {
            agent.speed = chargeSpeed;
        }

        // Update destination if tracking moving target
        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.transform.position);
            targetSoundPosition = currentTarget.transform.position;
            lastKnownPosition = targetSoundPosition;
        }
        else if (hasDetectedTarget && soundSource != null)
        {
            agent.SetDestination(targetSoundPosition);
        }

        // Rotate towards movement direction (2x speed for aggressive charging)
        RotateTowardsMovement(2f);

        // Check for headbutt collisions
        CheckHeadbuttCollision();

        // Check if charge should end
        if (stateTimer >= chargeDuration || HasReachedDestination(chargeStopDistance))
        {
            StopCharge();
        }
    }

    void StopCharge()
    {
        isCharging = false;
        isRecovering = true;
        agent.ResetPath();
        agent.speed = normalMoveSpeed;

        Debug.Log($"{gameObject.name}: Charge ended, recovering...");

        Invoke(nameof(FinishRecovery), chargeRecoveryTime);

        if (currentTarget != null)
        {
            OnTargetLost();
        }
        else
        {
            ChangeState(AIState.Searching);
        }
    }

    void FinishRecovery()
    {
        isRecovering = false;
        Debug.Log($"{gameObject.name}: Recovery complete");
    }

    void CheckHeadbuttCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 0.5f, headbuttRange, attackLayers);

        foreach (Collider hit in hits)
        {
            if (Time.time - lastCollisionAttackTime < collisionAttackCooldown)
                continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(headbuttDamage);
                lastCollisionAttackTime = Time.time;
                Debug.Log($"{gameObject.name}: Headbutt hit {hit.name}!");

                if (!hasDetectedTarget)
                {
                    OnTargetDetected(hit.gameObject, DetectionType.Visual);
                }

                break;
            }
        }
    }

    #endregion

    #region State: Searching

    void HandleSearching()
    {
        // Ensure we're at normal speed
        if (agent.speed != normalMoveSpeed)
        {
            agent.speed = normalMoveSpeed;
        }

        // Stop any movement
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        // Rotate in place while searching
        if (!isRecovering)
        {
            RotateInPlace();
        }

        // Check memory for recent sounds
        RememberedSound recentSound = GetMostImportantSound();
        if (recentSound != null && (Time.time - recentSound.timeHeard) < 3f)
        {
            // Found something to investigate
            targetSoundPosition = recentSound.position;
            soundMemoryTimer = 3f;
            ChangeState(AIState.Investigating);
            return;
        }

        // Return to patrol after search timeout
        if (stateTimer >= 5f)
        {
            ChangeState(AIState.Patrolling);
            idlePosition = transform.position;
            stateTimer = 0f;
        }
    }

    #endregion

    void OnSoundForgotten()
    {
        soundSource = null;

        if (currentState == AIState.Investigating)
        {
            ChangeState(AIState.Searching);
        }
        else if (!hasDetectedTarget)
        {
            OnTargetLost();
        }
    }

    #region Alert Response

    protected override void OnAlertReceived(AlertRaisedEvent evt)
    {
        float distance = Vector3.Distance(transform.position, evt.Position);

        if (distance <= baseHearingRange && !isRecovering)
        {
            Debug.Log($"{gameObject.name}: Heard alert from {evt.Source.name}");
            targetSoundPosition = evt.Position;
            soundMemoryTimer = soundMemoryDuration;

            soundSource = evt.Source;

            if (currentState != AIState.Chasing)
            {
                ChangeState(AIState.Investigating);
                agent.ResetPath();
            }
        }
    }

    #endregion

    #region Target Management

    protected override void OnTargetAcquired(GameObject target, DetectionType detectionType)
    {
        currentTarget = target;
        targetSoundPosition = target.transform.position;
        soundSource = target;

        if (!isCharging && !isRecovering)
        {
            StartCharge();
        }
    }

    protected override void OnTargetLostBehavior()
    {
        if (currentState == AIState.Chasing && !isCharging)
        {
            ChangeState(AIState.Searching);
        }
    }

    #endregion

    #region Collision Detection

    void OnCollisionEnter(Collision collision)
    {
        HandleCollisionAttack(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollisionAttack(other.gameObject);
    }

    void HandleCollisionAttack(GameObject target)
    {
        if (((1 << target.layer) & attackLayers) != 0)
        {
            if (Time.time - lastCollisionAttackTime < collisionAttackCooldown)
                return;

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(headbuttDamage);
                lastCollisionAttackTime = Time.time;
                Debug.Log($"{gameObject.name}: Collision attack on {target.name}!");

                if (!hasDetectedTarget)
                {
                    OnTargetDetected(target, DetectionType.Visual);
                }
            }
        }
    }

    #endregion

    #region Health System

    public void TakeDamage(float damage, bool isWeakSpotHit = false)
    {
        float actualDamage = damage;

        if (isWeakSpotHit && weakSpot != null)
        {
            actualDamage *= weakSpotMultiplier;
            Debug.Log($"{gameObject.name}: Weak spot hit! {actualDamage} damage");
        }

        currentHealth -= actualDamage;
        Debug.Log($"{gameObject.name}: Took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");

        // Make a sound when hit (so other AI can hear)
        InvestigateSound(transform.position, 1f);

        if (currentState != AIState.Chasing && !isRecovering)
        {
            ChangeState(AIState.Searching);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void CheckDirectTargetDetection()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, headbuttRange * 1.5f, detectionMask))
        {
            if (!hasDetectedTarget || currentTarget != hit.collider.gameObject)
            {
                OnTargetDetected(hit.collider.gameObject, DetectionType.Visual);
            }
        }
    }

    protected override void Die()
    {
        Debug.Log($"{gameObject.name}: Destroyed!");

        Events.Publish(new AIDeathEvent
        {
            AI = gameObject,
            Position = transform.position
        });

        Destroy(gameObject);
    }

    #endregion

    #region Debug Visualization

    protected override void DrawCustomGizmos()
    {
        // Hearing range
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        Gizmos.DrawWireSphere(transform.position, baseHearingRange);
        
        // Current hearing state
        float currentRange = baseHearingRange;
        if (currentState == AIState.Alerted || currentState == AIState.Chasing || currentState == AIState.Searching)
        {
            currentRange *= alertStateHearingBoost;
            Gizmos.color = new Color(1, 0.5f, 0, 0.15f);
            Gizmos.DrawWireSphere(transform.position, currentRange);
        }

        // Headbutt range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, headbuttRange);

        // Sound memory
        foreach (var sound in soundMemory)
        {
            float alpha = Mathf.Clamp01(sound.strength * 0.7f);
            if (sound.isPlayerSound)
            {
                Gizmos.color = new Color(1, 0, 0, alpha);
            }
            else
            {
                Gizmos.color = new Color(1, 1, 0, alpha);
            }
            Gizmos.DrawWireSphere(sound.position, 0.3f);
            
            // Draw line to sound if it's recent
            if (Time.time - sound.timeHeard < 2f)
            {
                Gizmos.DrawLine(transform.position, sound.position);
            }
        }

        // Current target sound
        if (soundMemoryTimer > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSoundPosition, 0.5f);
            Gizmos.DrawLine(transform.position, targetSoundPosition);
        }

        // Charging indicator
        if (isCharging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
        }

        // Weak spot
        if (weakSpot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(weakSpot.position, 0.3f);
        }

        // Wander radius
        if (currentState == AIState.Patrolling && shouldWander && !HasPatrolPoints())
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(idlePosition, wanderRadius);
        }

        // Forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // Velocity direction
        if (agent != null && agent.hasPath && agent.velocity.sqrMagnitude > 0.1f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, agent.velocity.normalized * 2f);
        }
    }

    #endregion
}