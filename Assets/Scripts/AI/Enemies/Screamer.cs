using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;
using UnityEngine.AI;


public class Screamer : BaseAI
{
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 90f; 
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float rotationThreshold = 5f; 

    [Header("Sound Detection")]
    [SerializeField] private float hearingRange = 25f;
    [SerializeField] private float soundMemoryDuration = 3f;

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

    private float currentHealth;
    private Vector3 targetSoundPosition;
    private float soundMemoryTimer;
    private bool isCharging;
    private bool isRecovering;
    private Vector3 idlePosition;
    private float idleTimer;
    private float lastCollisionAttackTime;
    private GameObject soundSource;

    protected override void InitializeAI()
    {
        currentState = AIState.Patrolling; 
        previousState = AIState.Patrolling;
        idlePosition = transform.position;
        currentHealth = maxHealth;
        currentPatrolIndex = 0;

        agent.speed = normalSpeed;
        agent.angularSpeed = 0; 
        agent.acceleration = 15f;
        agent.updateRotation = false; 
        agent.autoBraking = true;

        if (HasPatrolPoints())
        {
            GoToNextPatrolPoint();
        }
    }

    protected override void UpdateState()
    {
        switch (currentState)
        {
            case AIState.Patrolling: 
                if (HasPatrolPoints())
                {
                    HandlePatrollingWithRotation();
                }
                else
                {
                    HandleIdle();
                }
                break;
            case AIState.Investigating: 
                HandleRotatingToSound();
                break;
            case AIState.Chasing: 
                HandleCharging();
                break;
            case AIState.Searching: 
                HandleSearching();
                break;
        }

        if (soundMemoryTimer > 0)
        {
            soundMemoryTimer -= Time.deltaTime;
            if (soundMemoryTimer <= 0)
            {
                OnSoundForgotten();
            }
        }

        stateTimer += Time.deltaTime;
    }

    protected override void CheckForTargets()
    {
        if (isCharging && !isRecovering)
        {
            CheckDirectTargetDetection();
        }
    }

    #region Patrol with Rotation (Screamer-specific)

    void HandlePatrollingWithRotation()
    {
        if (!IsAgentReady()) return;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0; 

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            stateTimer += Time.deltaTime;

            if (stateTimer >= patrolWaitTime)
            {
                transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime);

                if (stateTimer >= patrolWaitTime)
                {
                    GoToNextPatrolPoint();
                    stateTimer = 0f;
                }
            }
        }
    }

    #endregion

    #region Idle State (When no patrol points)

    void HandleIdle()
    {
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        if (idleTimer < 0.5f)
        {
            transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime);
        }

        if (shouldWander && !HasPatrolPoints())
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleMoveInterval)
            {
                WanderToNewPosition();
                idleTimer = 0f;
            }

            if (agent.hasPath && HasReachedDestination(1.5f))
            {
                agent.ResetPath();
                idleTimer = idleMoveInterval - 2f; 
            }
        }
    }

    void WanderToNewPosition()
    {
        Vector3 randomPoint = GetRandomPointInRadius(idlePosition, wanderRadius);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log($"{gameObject.name}: Wandering to new position");
        }
    }

    #endregion

    #region Sound Response

    void HandleRotatingToSound()
    {
        if (!IsAgentReady()) return;

        agent.ResetPath();

        Vector3 directionToSound = (targetSoundPosition - transform.position).normalized;
        directionToSound.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(directionToSound);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        float angleToTarget = Vector3.Angle(transform.forward, directionToSound);

        if (angleToTarget <= rotationThreshold)
        {
            StartCharge();
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

    #region Charging State

    void HandleCharging()
    {
        if (!IsAgentReady()) return;

        stateTimer += Time.deltaTime;

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
                    rotationSpeed * 2f * Time.deltaTime 
                );
            }
        }

        if (stateTimer >= chargeDuration || HasReachedDestination(chargeStopDistance))
        {
            StopCharge();
            return;
        }

        CheckHeadbuttCollision();

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
    }

    void StopCharge()
    {
        isCharging = false;
        isRecovering = true;
        agent.ResetPath();
        agent.speed = normalSpeed;

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

    #region Searching State

    void HandleSearching()
    {
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        if (!isRecovering)
        {
            transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime);
        }

        if (stateTimer >= 5f)
        {
            ChangeState(AIState.Patrolling);
            idlePosition = transform.position;
            stateTimer = 0f;
        }
    }

    #endregion

    #region Sound Detection

    protected override void OnSoundHeard(SoundHeardEvent evt)
    {
        base.OnSoundHeard(evt);

        float distance = Vector3.Distance(transform.position, evt.SoundPosition);

        if (distance <= hearingRange)
        {
            Debug.Log($"{gameObject.name}: Heard sound at {distance:F1}m!");

            targetSoundPosition = evt.SoundPosition;
            soundMemoryTimer = soundMemoryDuration;

            if (evt.AI != null && evt.AI != gameObject)
            {
                soundSource = evt.AI;
            }

            if (currentState != AIState.Chasing && !isRecovering)
            {
                ChangeState(AIState.Investigating);
                agent.ResetPath(); 
            }
        }
    }

    void OnSoundForgotten()
    {
        Debug.Log($"{gameObject.name}: Forgot sound location");
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

    #endregion

    #region Alert Response

    protected override void OnAlertReceived(AlertRaisedEvent evt)
    {
        float distance = Vector3.Distance(transform.position, evt.Position);

        if (distance <= hearingRange && !isRecovering)
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, headbuttRange);

        if (soundMemoryTimer > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSoundPosition, 1f);
            Gizmos.DrawLine(transform.position, targetSoundPosition);
        }

        if (isCharging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
        }

        if (weakSpot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(weakSpot.position, 0.3f);
        }

        if (currentState == AIState.Patrolling && shouldWander && !HasPatrolPoints())
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(idlePosition, wanderRadius);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.1f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, agent.velocity.normalized * 2f);
        }
    }

    #endregion
}