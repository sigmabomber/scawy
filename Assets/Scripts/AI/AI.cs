
using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;
using UnityEngine.AI;

public class AI : EventListener
{
    [Header("AI Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform eyePosition;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float pointReachedDistance = 0.5f;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask detectionMask;
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private float soundDetectionRange = 20f;
    [SerializeField] private float detectionCheckInterval = 0.2f; 

    [Header("Investigation Settings")]
    [SerializeField] private float investigationTime = 5f;
    [SerializeField] private float investigationRadius = 3f;

    [Header("Alert Settings")]
    [SerializeField] private bool canRaiseAlerts = true;
    [SerializeField] private bool respondsToAlerts = true;
    [SerializeField] private float alertRadius = 30f;

    // State management
    private AIState currentState;
    private AIState previousState;
    private int currentPatrolIndex;
    private float stateTimer;
    private Vector3 lastKnownPosition;
    private Vector3 returnPoint;
    private GameObject currentTarget;
    private float detectionTimer;
    private bool hasDetectedTarget; 

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (eyePosition == null) eyePosition = transform;

        // Subscribe to events
        Listen<SoundHeardEvent>(OnSoundHeard);
        Listen<AlertRaisedEvent>(OnAlertRaised);

        currentState = AIState.Patrolling;
        previousState = AIState.Patrolling;
        currentPatrolIndex = 0;

        if (patrolPoints.Length > 0)
        {
            GoToNextPatrolPoint();
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolling();
                break;
            case AIState.Investigating:
                HandleInvestigating();
                break;
            case AIState.Searching:
                HandleSearching();
                break;
            case AIState.Returning:
                HandleReturning();
                break;
            case AIState.Alerted:
                HandleAlerted();
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
        }

        // Throttled detection checks
        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionCheckInterval)
        {
            CheckVisionDetection();
            detectionTimer = 0f;
        }
    }

    #region Patrol State

    void HandlePatrolling()
    {
        // Check if agent is active and on NavMesh
        if (!agent.isOnNavMesh || !agent.enabled)
            return;

        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            stateTimer += Time.deltaTime;

            if (stateTimer >= patrolWaitTime)
            {
                GoToNextPatrolPoint();
                stateTimer = 0f;
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    #endregion

    #region Investigation State

    void HandleInvestigating()
    {
        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            stateTimer += Time.deltaTime;

            // Look around while investigating
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);

            if (stateTimer >= investigationTime)
            {
                CompleteInvestigation(false);
            }
        }
    }

    void CompleteInvestigation(bool foundTarget)
    {
        Events.Publish(new InvestigationCompleteEvent
        {
            AI = gameObject,
            InvestigatedPosition = lastKnownPosition,
            FoundAnything = foundTarget
        });

        if (!foundTarget)
        {
            ChangeState(AIState.Searching);
        }
    }

    #endregion

    #region Searching State

    void HandleSearching()
    {
        stateTimer += Time.deltaTime;

        // Search around the investigation point
        if (stateTimer >= 2f)
        {
            Vector3 searchPoint = GetRandomPointInRadius(lastKnownPosition, investigationRadius);

            if (NavMesh.SamplePosition(searchPoint, out NavMeshHit hit, investigationRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                stateTimer = 0f;
            }
        }

        // Give up search after investigation time
        if (stateTimer >= investigationTime)
        {
            ChangeState(AIState.Returning);
        }
    }

    #endregion

    #region Returning State

    void HandleReturning()
    {
        agent.SetDestination(returnPoint);

        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            ChangeState(AIState.Patrolling);
            GoToNextPatrolPoint();
        }
    }

    #endregion

    #region Alerted State

    void HandleAlerted()
    {
        // Move to alert position
        if (!agent.pathPending && agent.remainingDistance <= pointReachedDistance)
        {
            stateTimer += Time.deltaTime;

            // Look around
            transform.Rotate(Vector3.up, 60f * Time.deltaTime);

            if (stateTimer >= investigationTime * 0.5f)
            {
                ChangeState(AIState.Searching);
            }
        }
    }

    #endregion

    #region Chasing State

    void HandleChasing()
    {
        if (currentTarget == null)
        {
            OnTargetLost();
            return;
        }

        // Update destination to target's position
        agent.SetDestination(currentTarget.transform.position);
        lastKnownPosition = currentTarget.transform.position;

        // Vision detection will automatically handle losing sight
    }

    #endregion

    #region Vision Detection

    void CheckVisionDetection()
    {
        Collider[] targets = Physics.OverlapSphere(eyePosition.position, visionRange, detectionMask);

        bool foundTarget = false;

        foreach (Collider target in targets)
        {
            Vector3 directionToTarget = (target.transform.position - eyePosition.position).normalized;
            float angleToTarget = Vector3.Angle(eyePosition.forward, directionToTarget);

            if (angleToTarget <= visionAngle / 2f)
            {
                float distanceToTarget = Vector3.Distance(eyePosition.position, target.transform.position);

                if (Physics.Raycast(eyePosition.position, directionToTarget, out RaycastHit hit, distanceToTarget))
                {
                    if (hit.collider == target)
                    {
                        foundTarget = true;

                        // Only trigger detection if we haven't already detected this target
                        if (!hasDetectedTarget || currentTarget != target.gameObject)
                        {
                            OnTargetDetected(target.gameObject, DetectionType.Visual);
                        }
                        break; // Found a target, no need to check others
                    }
                }
            }
        }

        // If we previously had a target but can't see it anymore, lose it
        if (!foundTarget && hasDetectedTarget && currentState == AIState.Chasing)
        {
            hasDetectedTarget = false;
            OnTargetLost();
        }
    }

    void OnTargetDetected(GameObject target, DetectionType detectionType)
    {
        // Skip if already detected
        if (hasDetectedTarget && currentTarget == target)
            return;

        currentTarget = target;
        hasDetectedTarget = true;
        returnPoint = transform.position;
        lastKnownPosition = target.transform.position;

        // Publish detection event ONCE
        Events.Publish(new TargetDetectedEvent
        {
            AI = gameObject,
            Target = target,
            Position = target.transform.position,
            Type = detectionType
        });

        // Raise alert to nearby AI
        if (canRaiseAlerts)
        {
            RaiseAlert(AlertLevel.High);
        }

        ChangeState(AIState.Chasing);

        Debug.Log($"{gameObject.name}: Target detected - {target.name}");
    }

    void OnTargetLost()
    {
        if (!hasDetectedTarget)
            return;

        Events.Publish(new TargetLostEvent
        {
            AI = gameObject,
            LastTarget = currentTarget,
            LastKnownPosition = lastKnownPosition
        });

        hasDetectedTarget = false;
        GameObject lostTarget = currentTarget;
        currentTarget = null;

        ChangeState(AIState.Investigating);
        agent.SetDestination(lastKnownPosition);

        Debug.Log($"{gameObject.name}: Lost target - {lostTarget.name}");
    }

    #endregion

    #region Event Handlers

    void OnSoundHeard(SoundHeardEvent evt)
    {
        // Ignore our own sounds
        if (evt.AI == gameObject) return;

        float distance = Vector3.Distance(transform.position, evt.SoundPosition);

        if (distance <= soundDetectionRange)
        {
            // Only react if not already in high priority state
            if (currentState == AIState.Patrolling || currentState == AIState.Returning)
            {
                Debug.Log($"{gameObject.name}: Heard sound at distance {distance:F1}m");
                returnPoint = transform.position;
                lastKnownPosition = evt.SoundPosition;
                ChangeState(AIState.Investigating);
                agent.SetDestination(evt.SoundPosition);
            }
        }
    }

    void OnAlertRaised(AlertRaisedEvent evt)
    {
        if (!respondsToAlerts) return;
        if (evt.Source == gameObject) return;

        float distance = Vector3.Distance(transform.position, evt.Position);

        if (distance <= alertRadius)
        {
            // React based on alert level and current state
            if (evt.Level == AlertLevel.High && currentState != AIState.Chasing)
            {
                Debug.Log($"{gameObject.name}: Responding to HIGH alert from {evt.Source.name}");
                returnPoint = transform.position;
                lastKnownPosition = evt.Position;
                ChangeState(AIState.Alerted);
                agent.SetDestination(evt.Position);
            }
            else if (evt.Level == AlertLevel.Medium && currentState == AIState.Patrolling)
            {
                Debug.Log($"{gameObject.name}: Responding to MEDIUM alert");
                returnPoint = transform.position;
                lastKnownPosition = evt.Position;
                ChangeState(AIState.Investigating);
                agent.SetDestination(evt.Position);
            }
        }
    }

    void RaiseAlert(AlertLevel level)
    {
        Events.Publish(new AlertRaisedEvent
        {
            Source = gameObject,
            Position = lastKnownPosition,
            Level = level
        });

        Debug.Log($"{gameObject.name}: Raised {level} alert at {lastKnownPosition}");
    }

    #endregion

    #region Public API

    public void InvestigateSound(Vector3 soundPosition, float intensity = 1f)
    {
        Events.Publish(new SoundHeardEvent
        {
            AI = gameObject,
            SoundPosition = soundPosition,
            SoundIntensity = intensity
        });
    }

    public AIState GetCurrentState() => currentState;

    public Vector3 GetLastKnownPosition() => lastKnownPosition;

    #endregion

    #region Utility

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        Events.Publish(new AIStateChangedEvent
        {
            AI = gameObject,
            PreviousState = previousState,
            NewState = newState
        });

        Debug.Log($"{gameObject.name}: State changed from {previousState} to {newState}");
    }

    Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return center + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    #endregion

    #region Debug Visualization

    void OnDrawGizmosSelected()
    {
        if (eyePosition == null) eyePosition = transform;

        // Vision cone
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * eyePosition.forward * visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * eyePosition.forward * visionRange;

        Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftBoundary);
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightBoundary);

        // Sound detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, soundDetectionRange);

        // Alert range
        if (canRaiseAlerts)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, alertRadius);
        }

        // Patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }

        // Investigation area
        if (currentState == AIState.Investigating || currentState == AIState.Searching)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPosition, investigationRadius);
        }

        // Target line
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePosition.position, currentTarget.transform.position);
        }
    }

    #endregion
}