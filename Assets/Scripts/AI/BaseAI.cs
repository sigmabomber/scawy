using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Patrolling,
    Investigating,
    Searching,
    Returning,
    Alerted,
    Chasing
}

public enum DetectionType
{
    Visual,
    Sound,
    Alert
}

public enum AlertLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// Base AI controller handles common AI functionality including animation
/// This is your original AI script refactored to be extendable
/// Extend this class to create specific enemy types
/// </summary>
public abstract class BaseAI : EventListener
{
    [Header("AI Components")]
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Transform eyePosition;
    [SerializeField] protected Animator animator;

    [Header("Patrol Settings")]
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolWaitTime = 2f;
    [SerializeField] protected float pointReachedDistance = 0.5f;
    protected int currentPatrolIndex;

    [Header("Detection Settings")]
    [SerializeField] protected LayerMask detectionMask;
    [SerializeField] protected float detectionCheckInterval = 0.2f;

    [Header("Alert Settings")]
    [SerializeField] protected bool canRaiseAlerts = true;
    [SerializeField] protected bool respondsToAlerts = true;
    [SerializeField] protected float alertRadius = 30f;

    [Header("Animation Settings")]
    [SerializeField] protected bool useAnimations = true;
    [SerializeField] protected float animationSmoothTime = 0.1f;
    [SerializeField] protected float speedThreshold = 0.1f;
    [SerializeField] protected string speedParameter = "Speed";
    [SerializeField] protected string stateParameter = "AIState";
    [SerializeField] protected string attackTrigger = "Attack";
    [SerializeField] protected string damageTrigger = "TakeDamage";
    [SerializeField] protected string deathTrigger = "Death";
    [SerializeField] protected string alertTrigger = "Alert";

    // State management
    protected AIState currentState;
    protected AIState previousState;
    protected float stateTimer;
    protected Vector3 lastKnownPosition;
    protected Vector3 returnPoint;
    protected GameObject currentTarget;
    protected float detectionTimer;
    protected bool hasDetectedTarget;

    // Animation
    protected float currentSpeed;
    protected Vector3 lastPosition;
    protected bool isAnimatorInitialized;

    protected virtual void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (eyePosition == null) eyePosition = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Initialize animation
        if (useAnimations && animator != null)
        {
            InitializeAnimator();
        }

        // Subscribe to events
        Listen<SoundHeardEvent>(OnSoundHeard);
        Listen<AlertRaisedEvent>(OnAlertRaised);

        InitializeAI();
    }

    protected virtual void Update()
    {
        UpdateState();
        UpdateDetection();

        // Update animations if enabled
        if (useAnimations && animator != null)
        {
            UpdateAnimations();
        }
    }

    /// <summary>
    /// Override this to set initial state and configuration
    /// </summary>
    protected abstract void InitializeAI();

    /// <summary>
    /// Override this to handle state-specific logic
    /// </summary>
    protected abstract void UpdateState();

    /// <summary>
    /// Override this to implement custom detection logic (vision, hearing, etc.)
    /// Called every detectionCheckInterval seconds
    /// </summary>
    protected abstract void CheckForTargets();

    #region Animation System

    /// <summary>
    /// Initializes the animator controller
    /// </summary>
    protected virtual void InitializeAnimator()
    {
        if (animator == null) return;

        // Set initial animation parameters
        animator.SetFloat(speedParameter, 0f);
        animator.SetInteger(stateParameter, (int)currentState);
        isAnimatorInitialized = true;
    }

    /// <summary>
    /// Updates animation parameters based on AI state and movement
    /// Call this in Update() if using animations
    /// </summary>
    protected virtual void UpdateAnimations()
    {
        if (!isAnimatorInitialized) return;

        // Calculate current speed for movement animations
        CalculateMovementSpeed();

        // Update speed parameter
        animator.SetFloat(speedParameter, currentSpeed, animationSmoothTime, Time.deltaTime);

        // Update state parameter
     
        animator.SetInteger(stateParameter, (int)currentState);

        // Additional state-specific animation updates
        UpdateStateAnimations();
    }

    /// <summary>
    /// Calculates the current movement speed based on position change
    /// </summary>
    protected virtual void CalculateMovementSpeed()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            // Use agent velocity for more accurate speed calculation
            currentSpeed = agent.velocity.magnitude / agent.speed;
        }
        else
        {
            // Fallback to position-based calculation
            float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
            currentSpeed = Mathf.Clamp01(speed / normalSpeed);
            lastPosition = transform.position;
        }
    }

    /// <summary>
    /// Override this to add state-specific animation updates
    /// </summary>
    protected virtual void UpdateStateAnimations()
    {
        // Base class does nothing - override in subclasses
    }

    /// <summary>
    /// Triggers an attack animation
    /// </summary>
    protected virtual void TriggerAttackAnimation()
    {
        if (useAnimations && animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }
    }

    /// <summary>
    /// Triggers a damage reaction animation
    /// </summary>
    protected virtual void TriggerDamageAnimation()
    {
        if (useAnimations && animator != null && !string.IsNullOrEmpty(damageTrigger))
        {
            animator.SetTrigger(damageTrigger);
        }
    }

    /// <summary>
    /// Triggers a death animation
    /// </summary>
    protected virtual void TriggerDeathAnimation()
    {
        if (useAnimations && animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.SetTrigger(deathTrigger);
        }
    }

    /// <summary>
    /// Triggers an alert animation
    /// </summary>
    protected virtual void TriggerAlertAnimation()
    {
        if (useAnimations && animator != null && !string.IsNullOrEmpty(alertTrigger))
        {
            animator.SetTrigger(alertTrigger);
        }
    }

    /// <summary>
    /// Sets a boolean animation parameter
    /// </summary>
    protected virtual void SetAnimationBool(string parameterName, bool value)
    {
        if (useAnimations && animator != null)
        {
            animator.SetBool(parameterName, value);
        }
    }

    /// <summary>
    /// Sets a float animation parameter
    /// </summary>
    protected virtual void SetAnimationFloat(string parameterName, float value)
    {
        if (useAnimations && animator != null)
        {
            animator.SetFloat(parameterName, value);
        }
    }

    /// <summary>
    /// Sets an integer animation parameter
    /// </summary>
    protected virtual void SetAnimationInt(string parameterName, int value)
    {
        if (useAnimations && animator != null)
        {
            animator.SetInteger(parameterName, value);
        }
    }

    #endregion

    /// <summary>
    /// Handles the detection timer and calls CheckForTargets
    /// </summary>
    protected virtual void UpdateDetection()
    {
        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionCheckInterval)
        {
            CheckForTargets();
            detectionTimer = 0f;
        }
    }

    #region Patrol Methods (Now in BaseAI)

    /// <summary>
    /// Goes to the next patrol point in sequence
    /// </summary>
    protected virtual void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    /// <summary>
    /// Goes to a specific patrol point by index
    /// </summary>
    protected virtual void GoToPatrolPoint(int index)
    {
        if (patrolPoints.Length == 0 || index < 0 || index >= patrolPoints.Length) return;

        currentPatrolIndex = index;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    /// <summary>
    /// Gets a random patrol point
    /// </summary>
    protected virtual void GoToRandomPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        currentPatrolIndex = Random.Range(0, patrolPoints.Length);
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    /// <summary>
    /// Handles patrol state logic - call this in UpdateState when in Patrolling state
    /// </summary>
    protected virtual void HandlePatrolling()
    {
        if (!IsAgentReady()) return;

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

    /// <summary>
    /// Checks if AI has patrol points assigned
    /// </summary>
    protected virtual bool HasPatrolPoints()
    {
        return patrolPoints != null && patrolPoints.Length > 0;
    }

    #endregion

    #region Target Management

    protected virtual void OnTargetDetected(GameObject target, DetectionType detectionType)
    {
        // Skip if already detected
        if (hasDetectedTarget && currentTarget == target)
            return;

        currentTarget = target;
        hasDetectedTarget = true;
        returnPoint = transform.position;
        lastKnownPosition = target.transform.position;

        // Trigger alert animation
        TriggerAlertAnimation();

        // Publish detection event
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

        OnTargetAcquired(target, detectionType);

        Debug.Log($"{gameObject.name}: Target detected - {target.name}");
    }

    protected virtual void OnTargetLost()
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

        OnTargetLostBehavior();

        Debug.Log($"{gameObject.name}: Lost target - {lostTarget?.name}");
    }

    /// <summary>
    /// Override to define behavior when target is first acquired
    /// </summary>
    protected abstract void OnTargetAcquired(GameObject target, DetectionType detectionType);

    /// <summary>
    /// Override to define behavior when target is lost
    /// </summary>
    protected abstract void OnTargetLostBehavior();

    #endregion

    #region Event Handlers

    protected virtual void OnSoundHeard(SoundHeardEvent evt)
    {
        if (evt.AI == gameObject) return;
        // Base implementation - override in derived classes
    }

    protected virtual void OnAlertRaised(AlertRaisedEvent evt)
    {
        if (!respondsToAlerts) return;
        if (evt.Source == gameObject) return;

        float distance = Vector3.Distance(transform.position, evt.Position);

        if (distance <= alertRadius)
        {
            OnAlertReceived(evt);
        }
    }

    /// <summary>
    /// Override to define how AI reacts to alerts
    /// </summary>
    protected virtual void OnAlertReceived(AlertRaisedEvent evt) { }

    protected void RaiseAlert(AlertLevel level)
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

    #region Health & Damage System

    /// <summary>
    /// Called when AI takes damage
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        // Trigger damage animation
        TriggerDamageAnimation();

        // Override in subclass to implement health system
        Debug.Log($"{gameObject.name}: Took {damage} damage");
    }

    /// <summary>
    /// Called when AI dies
    /// </summary>
    protected virtual void Die()
    {
        // Trigger death animation
        TriggerDeathAnimation();

        // Publish death event
        Events.Publish(new AIDeathEvent
        {
            AI = gameObject,
            Position = transform.position
        });

        Debug.Log($"{gameObject.name}: Destroyed!");
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
    public GameObject GetCurrentTarget() => currentTarget;

    /// <summary>
    /// Sets new patrol points for this AI
    /// </summary>
    public void SetPatrolPoints(Transform[] newPatrolPoints)
    {
        patrolPoints = newPatrolPoints;
        currentPatrolIndex = 0;

        if (currentState == AIState.Patrolling && patrolPoints.Length > 0)
        {
            GoToNextPatrolPoint();
        }
    }

    /// <summary>
    /// Adds a patrol point to the AI's patrol route
    /// </summary>
    public void AddPatrolPoint(Transform newPoint)
    {
        if (patrolPoints == null)
        {
            patrolPoints = new Transform[] { newPoint };
        }
        else
        {
            System.Array.Resize(ref patrolPoints, patrolPoints.Length + 1);
            patrolPoints[patrolPoints.Length - 1] = newPoint;
        }
    }

    #endregion

    #region Utility

    protected void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // Update animation state parameter
        if (useAnimations && animator != null)
        {
            animator.SetInteger(stateParameter, (int)newState);
        }

        Events.Publish(new AIStateChangedEvent
        {
            AI = gameObject,
            PreviousState = previousState,
            NewState = newState
        });

        Debug.Log($"{gameObject.name}: State changed from {previousState} to {newState}");
    }

    protected Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return center + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    protected bool IsAgentReady()
    {
        return agent != null && agent.isOnNavMesh && agent.enabled;
    }

    protected bool HasReachedDestination(float threshold = 0.5f)
    {
        return IsAgentReady() && !agent.pathPending && agent.remainingDistance <= threshold;
    }

    /// <summary>
    /// Gets the agent's normal movement speed (for animation calculations)
    /// </summary>
    protected virtual float normalSpeed
    {
        get { return agent != null ? agent.speed : 3.5f; }
    }

    #endregion

    #region Debug Visualization

    protected virtual void OnDrawGizmosSelected()
    {
        if (eyePosition == null) eyePosition = transform;

        // Alert range
        if (canRaiseAlerts)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, alertRadius);
        }

        // Target line
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePosition.position, currentTarget.transform.position);
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

        DrawCustomGizmos();
    }


    protected virtual void DrawCustomGizmos() { }

    #endregion
}

// Event classes
public class TargetDetectedEvent
{
    public GameObject AI;
    public GameObject Target;
    public Vector3 Position;
    public DetectionType Type;
}

public class TargetLostEvent
{
    public GameObject AI;
    public GameObject LastTarget;
    public Vector3 LastKnownPosition;
}

public class AlertRaisedEvent
{
    public GameObject Source;
    public Vector3 Position;
    public AlertLevel Level;
}

public class SoundHeardEvent
{
    public GameObject AI;
    public Vector3 SoundPosition;
    public float SoundIntensity;
}

public class AIStateChangedEvent
{
    public GameObject AI;
    public AIState PreviousState;
    public AIState NewState;
}

public class InvestigationCompleteEvent
{
    public GameObject AI;
    public Vector3 InvestigatedPosition;
    public bool FoundAnything;
}

public class AIDeathEvent
{
    public GameObject AI;
    public Vector3 Position;
}

public interface IDamageable
{
    void TakeDamage(float damage);
}