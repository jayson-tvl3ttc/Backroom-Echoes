using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;        // Patrol points array
    public float waitTime = 2f;             // Wait time at each point
    public float patrolSpeed = 1.5f;        // Patrol speed

    [Header("Chase Settings")]
    public float chaseSpeed = 3.5f;         // Chase speed
    public float searchSpeed = 2.0f;        // Search speed
    public float chaseTimeout = 15f;        // Chase timeout duration
    public float searchTimeout = 8f;        // Search timeout duration
    public float lostPlayerDelay = 2f;      // Delay before starting search after losing player

    [Header("Investigation Settings")]
    public float investigationTimeout = 10f;    // Investigation timeout duration
    public float investigationSpeed = 2.5f;     // Investigation speed

    [Header("Audio Settings")]
    public AudioSource footstepsAudio;      // Footsteps sound
    public AudioSource breathingAudio;      // Breathing sound

    [Header("Player Detection Settings")]
    public float detectionDistance = 4f;    // Visual detection distance
    public float hearingDistance = 2f;      // Hearing detection distance (from behind)
    public float captureDistance = 1.2f;    // Capture distance
    public float viewAngle = 60f;           // Field of view angle
    public LayerMask obstacleLayerMask = -1; // Obstacle layer mask
    public float restartDelay = 1f;         // Restart delay
    public bool enablePlayerDetection = true;
    public bool enableLineOfSight = true;   // Enable line of sight detection
    public bool enableHearing = true;       // Enable hearing detection

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool useSimpleDetection = false; // Simplified detection for debugging

    // Enemy states
    public enum EnemyState
    {
        Patrolling,
        Waiting,
        Chasing,        // Chasing player
        Searching,      // Searching for player
        Investigating,  // Investigating sound
        Alert
    }

    public EnemyState currentState = EnemyState.Patrolling;

    // Private variables
    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private bool isMoving = false;
    private Transform playerTransform;           // Player Transform
    private bool gameEnded = false;     // Whether the game has ended
    private Vector3 lastKnownPlayerPosition; // Player's last known position
    private float stateTimer = 0f;      // State timer
    private bool canSeePlayer = false;  // Whether player is currently visible
    private Coroutine currentStateCoroutine; // Current state coroutine
    private Vector3 investigationTarget; // Investigation target position
    private bool hasInvestigationTarget = false; // Whether there is an investigation target

    void Start()
    {
        InitializeEnemy();
    }

    void InitializeEnemy()
    {
        // Get NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Enemy requires NavMeshAgent component!");
            return;
        }

        // Find player
        FindPlayer();

        // Set NavMeshAgent parameters
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = true;

        // Auto-find patrol points (if not manually set)
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            FindPatrolPoints();
        }

        // Setup audio
        SetupAudio();

        // Start patrolling
        if (patrolPoints.Length > 0)
        {
            StartPatrolling();
        }
        else
        {
            Debug.LogWarning("No patrol points found!");
        }
    }

    void FindPlayer()
    {
        // Find player - multiple methods
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            // If no Player tag, look for XR Origin
            playerObject = GameObject.Find("XR Origin (XR Rig)");
        }

        if (playerObject == null)
        {
            // Find main camera as player position
            if (Camera.main != null)
            {
                playerObject = Camera.main.gameObject;
            }
        }

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            if (enableDebugLogs)
                Debug.Log($"Player found: {playerTransform.name}");
        }
        else
        {
            Debug.LogWarning("Player object not found!");
        }
    }

    void FindPatrolPoints()
    {
        // Auto-find patrol points in the scene
        GameObject[] patrolObjects = GameObject.FindGameObjectsWithTag("PatrolPoint");
        patrolPoints = new Transform[patrolObjects.Length];

        for (int i = 0; i < patrolObjects.Length; i++)
        {
            patrolPoints[i] = patrolObjects[i].transform;
        }

        if (enableDebugLogs)
            Debug.Log($"Found {patrolPoints.Length} patrol points");
    }

    void SetupAudio()
    {
        // Setup breathing sound (continuous play)
        if (breathingAudio)
        {
            breathingAudio.Play();
        }

        // Footsteps sound default not playing (only during movement)
        if (footstepsAudio)
        {
            footstepsAudio.Stop();
        }
    }

    void StartPatrolling()
    {
        if (patrolPoints.Length == 0) return;

        currentState = EnemyState.Patrolling;
        GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        if (isWaiting) return;

        // Set target point
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        agent.SetDestination(targetPoint.position);

        // Start movement sound effects
        StartMoving();

        if (enableDebugLogs)
            Debug.Log($"Going to patrol point {currentPatrolIndex}: {targetPoint.name}");
    }

    void StartMoving()
    {
        isMoving = true;
        UpdateAudioForState();
    }

    void StopMoving()
    {
        isMoving = false;

        // Stop footsteps sound
        if (footstepsAudio && footstepsAudio.isPlaying)
        {
            footstepsAudio.Stop();
        }
    }

    void Update()
    {
        if (gameEnded) return; // Stop all logic after game ends

        // Update state timer
        stateTimer += Time.deltaTime;

        // Check player visibility
        if (enablePlayerDetection)
        {
            CheckPlayerVisibility();
        }

        // Execute logic based on current state
        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;
            case EnemyState.Waiting:
                // Wait state is handled in coroutine
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Searching:
                HandleSearching();
                break;
            case EnemyState.Investigating:
                HandleInvestigating();
                break;
        }

        // Check player distance (for game over detection)
        CheckPlayerCaptured();
    }

    void CheckPlayerVisibility()
    {
        if (playerTransform == null || gameEnded) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (enableDebugLogs)
            Debug.Log($"Player distance: {distance:F2}m, Visual distance: {detectionDistance}m, Hearing distance: {hearingDistance}m");

        bool playerDetected = false;
        string detectionType = "";

        // Simplified detection mode (for debugging)
        if (useSimpleDetection)
        {
            // Simple mode: can detect within visual or hearing range
            if (distance <= detectionDistance || distance <= hearingDistance)
            {
                playerDetected = true;
                detectionType = "Simplified mode";
            }
        }
        else
        {
            // 1. Visual detection: within front field of view
            if (distance <= detectionDistance)
            {
                if (CanSeePlayer()) // Must also satisfy line of sight detection
                {
                    playerDetected = true;
                    detectionType = "Visual";
                    lastKnownPlayerPosition = playerTransform.position;
                    if (enableDebugLogs)
                        Debug.Log("Visual detection successful: Player in view and line of sight clear");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log("Visual detection failed: Player within distance but doesn't meet line of sight conditions");
                }
            }

            // 2. Hearing detection: close distance from behind
            if (!playerDetected && enableHearing && distance <= hearingDistance)
            {
                if (CanHearPlayer())
                {
                    playerDetected = true;
                    detectionType = "Hearing";
                    lastKnownPlayerPosition = playerTransform.position;
                    if (enableDebugLogs)
                        Debug.Log("Hearing detection successful: Player within range behind");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log("Hearing detection failed: Player not behind");
                }
            }
        }

        canSeePlayer = playerDetected;

        if (enableDebugLogs && playerDetected)
            Debug.Log($"Player detected by {detectionType}!");

        // Decide behavior based on detection state and current state
        if (canSeePlayer)
        {
            if (enableDebugLogs)
                Debug.Log($"Current state: {currentState}, Player visible: {canSeePlayer}");

            // When player is found, highest priority, immediately chase
            if (currentState != EnemyState.Chasing)
            {
                if (enableDebugLogs)
                    Debug.Log("Player found, start chasing (interrupt other activities)");
                StartChasing();
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Player not visible, current state: {currentState}");

            if (currentState == EnemyState.Chasing)
            {
                // Delay starting search to avoid frequent switching
                if (currentStateCoroutine == null)
                {
                    if (enableDebugLogs)
                        Debug.Log("Starting delayed search coroutine");
                    currentStateCoroutine = StartCoroutine(DelayedStartSearching());
                }
            }
        }
    }

    bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 enemyForward = transform.forward;

        // Check if within field of view angle
        float angle = Vector3.Angle(enemyForward, directionToPlayer);
        if (enableDebugLogs)
            Debug.Log($"Visual detection - Player angle: {angle:F1}бу, View range: б└{viewAngle / 2f:F1}бу");

        if (angle > viewAngle / 2f)
        {
            if (enableDebugLogs)
                Debug.Log($"Visual detection failed: Player not within view angle ({angle:F1}бу > {viewAngle / 2f:F1}бу)");
            return false; // Player not within field of view
        }

        // If line of sight detection is disabled, return true if angle is satisfied
        if (!enableLineOfSight)
        {
            if (enableDebugLogs)
                Debug.Log("Visual detection successful: Angle satisfied and obstacle detection not enabled");
            return true;
        }

        // Check if line of sight is blocked
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 rayTarget = playerTransform.position + Vector3.up * 1f;
        Vector3 rayDirection = (rayTarget - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, rayTarget);

        if (enableDebugLogs)
            Debug.Log($"Line of sight detection - Ray distance: {rayDistance:F2}m");

        // Cast ray to detect obstacles
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, obstacleLayerMask))
        {
            // Ray hit an obstacle, check if it hit the player
            if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
            {
                if (enableDebugLogs)
                    Debug.Log("Visual detection successful: Ray directly hit player");
                return true; // Hit player, can see
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Visual detection failed: Line of sight blocked by {hit.transform.name}");
                return false; // Hit other object, line of sight blocked
            }
        }

        // Didn't hit anything, can see player
        if (enableDebugLogs)
            Debug.Log("Visual detection successful: No obstacles in line of sight");
        return true;
    }

    bool CanHearPlayer()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 enemyForward = transform.forward;

        // Check if player is behind (angle greater than 90 degrees)
        float angle = Vector3.Angle(enemyForward, directionToPlayer);

        if (enableDebugLogs)
            Debug.Log($"Hearing detection - Angle: {angle:F1}бу, Behind: {angle > 90f}");

        // Behind hearing detection: no line of sight needed, but must be behind
        if (angle > 90f) // Player is behind
        {
            if (enableDebugLogs)
                Debug.Log("Hearing detection: Player behind, can hear!");
            return true;
        }

        if (enableDebugLogs)
            Debug.Log("Hearing detection: Player in front, cannot hear");
        return false;
    }

    void HandlePatrolling()
    {
        // If there's an investigation target, prioritize investigation
        if (hasInvestigationTarget)
        {
            if (enableDebugLogs)
                Debug.Log("Investigation target detected, preparing to switch to investigation state");
            StartInvestigating();
            return;
        }

        CheckPatrolStatus();
    }

    void HandleChasing()
    {
        if (canSeePlayer)
        {
            // Can see player, chase directly
            agent.SetDestination(playerTransform.position);
            stateTimer = 0f; // Reset timer
        }
        else
        {
            // Chase timeout, start searching
            if (stateTimer >= chaseTimeout)
            {
                StartSearching();
            }
        }
    }

    void HandleSearching()
    {
        // Search timeout, return to patrol
        if (stateTimer >= searchTimeout)
        {
            if (enableDebugLogs)
                Debug.Log("Search timeout, returning to patrol");
            ReturnToPatrol();
            return;
        }

        // Check if reached search target point
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            // Look for new search points around the search area
            SearchAroundLastPosition();
        }
    }

    void HandleInvestigating()
    {
        // Check if reached investigation target point
        if (!agent.pathPending && agent.remainingDistance < 2f)
        {
            if (enableDebugLogs)
                Debug.Log("Reached investigation point, start searching for player");

            // After reaching investigation point, start searching for player
            TransitionToSearchFromInvestigation();
        }
    }

    void CheckPatrolStatus()
    {
        // Check if reached target point
        if (!agent.pathPending && agent.remainingDistance < 0.5f && !isWaiting)
        {
            // Reached patrol point, start waiting
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    void CheckPlayerCaptured()
    {
        if (playerTransform == null || gameEnded) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (enableDebugLogs)
            Debug.Log($"Capture detection - Distance: {distance:F2}m, Capture distance: {captureDistance:F2}m");

        // Use independent capture distance
        if (distance <= captureDistance)
        {
            if (enableDebugLogs)
                Debug.Log("Distance condition met, checking line of sight...");

            // If line of sight detection is enabled, need to be able to see player
            if (!enableLineOfSight || CanSeePlayer() || useSimpleDetection)
            {
                if (enableDebugLogs)
                    Debug.Log("Line of sight condition also met, player captured!");
                PlayerCaught();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("Line of sight condition not met");
            }
        }
    }

    void StartChasing()
    {
        if (currentState == EnemyState.Chasing) return;

        currentState = EnemyState.Chasing;
        stateTimer = 0f;

        // Stop current state coroutine
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        // Set chase speed and target
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);

        // Play chase sound effects
        UpdateAudioForState();

        if (enableDebugLogs)
            Debug.Log("Start chasing player!");
    }

    void StartInvestigating()
    {
        if (enableDebugLogs)
            Debug.Log($"=== StartInvestigating called ===");
        Debug.Log($"Current state: {currentState}");
        Debug.Log($"Investigation target: {investigationTarget}");

        if (currentState == EnemyState.Investigating)
        {
            if (enableDebugLogs)
                Debug.Log("Already in investigation state, skipping");
            return;
        }

        currentState = EnemyState.Investigating;
        stateTimer = 0f;

        // Stop current state coroutine
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
            if (enableDebugLogs)
                Debug.Log("Stopped current state coroutine");
        }

        // Set investigation speed and target
        agent.speed = investigationSpeed;
        agent.isStopped = false;
        agent.SetDestination(investigationTarget);

        if (enableDebugLogs)
        {
            Debug.Log($"Set NavMeshAgent target: {investigationTarget}");
            Debug.Log($"agent.speed set to: {investigationSpeed}");
            Debug.Log($"agent.isStopped set to: false");
        }

        // Start moving
        StartMoving();

        // Play investigation sound effects
        UpdateAudioForState();

        if (enableDebugLogs)
            Debug.Log($"*** Successfully switched to investigation state, going to: {investigationTarget} ***");
    }

    IEnumerator DelayedStartSearching()
    {
        yield return new WaitForSeconds(lostPlayerDelay);

        if (currentState == EnemyState.Chasing && !canSeePlayer)
        {
            StartSearching();
        }

        currentStateCoroutine = null; // Coroutine ended, clear reference
    }

    void StartSearching()
    {
        currentState = EnemyState.Searching;
        stateTimer = 0f;

        // Set search speed
        agent.speed = searchSpeed;

        // Go to player's last known position
        agent.SetDestination(lastKnownPlayerPosition);

        // Play search sound effects
        UpdateAudioForState();

        if (enableDebugLogs)
            Debug.Log("Start searching for player, last position: " + lastKnownPlayerPosition);
    }

    void SearchAroundLastPosition()
    {
        // Randomly select search points around the search center position
        Vector3 randomDirection = Random.insideUnitSphere * 5f;
        randomDirection += lastKnownPlayerPosition;
        randomDirection.y = lastKnownPlayerPosition.y; // Maintain Y-axis height

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);

            if (enableDebugLogs)
                Debug.Log($"Searching new position: {hit.position}, Search center: {lastKnownPlayerPosition}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("Cannot find valid search point, continue searching at current position");
        }
    }

    void ReturnToPatrol()
    {
        currentState = EnemyState.Patrolling;
        stateTimer = 0f;
        hasInvestigationTarget = false; // Clear investigation target

        // Restore patrol speed
        agent.speed = patrolSpeed;

        // Find nearest patrol point
        FindNearestPatrolPoint();

        // Restore patrol sound effects
        UpdateAudioForState();

        if (enableDebugLogs)
            Debug.Log("Return to patrol state");
    }

    void FindNearestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        currentPatrolIndex = nearestIndex;
        GoToNextPatrolPoint();
    }

    void TransitionToSearchFromInvestigation()
    {
        currentState = EnemyState.Searching;
        stateTimer = 0f;
        hasInvestigationTarget = false; // Clear investigation target

        // Set search speed
        agent.speed = searchSpeed;

        // Start searching centered at investigation point
        lastKnownPlayerPosition = investigationTarget;
        SearchAroundLastPosition();

        // Play search sound effects
        UpdateAudioForState();

        if (enableDebugLogs)
            Debug.Log("Transition from investigation state to search state, start searching around investigation point");
    }

    void UpdateAudioForState()
    {
        if (footstepsAudio == null) return;

        switch (currentState)
        {
            case EnemyState.Patrolling:
            case EnemyState.Waiting:
                // Normal footsteps sound
                if (isMoving && !footstepsAudio.isPlaying)
                {
                    footstepsAudio.pitch = 1f;
                    footstepsAudio.Play();
                }
                break;

            case EnemyState.Chasing:
                // Fast footsteps sound
                footstepsAudio.pitch = 1.5f;
                if (!footstepsAudio.isPlaying)
                {
                    footstepsAudio.Play();
                }
                break;

            case EnemyState.Searching:
                // Medium speed footsteps sound
                footstepsAudio.pitch = 1.2f;
                if (!footstepsAudio.isPlaying)
                {
                    footstepsAudio.Play();
                }
                break;

            case EnemyState.Investigating:
                // Investigation footsteps sound (slightly slower than searching)
                footstepsAudio.pitch = 1.1f;
                if (!footstepsAudio.isPlaying)
                {
                    footstepsAudio.Play();
                }
                break;
        }
    }

    void PlayerCaught()
    {
        if (gameEnded) return;

        gameEnded = true;

        if (enableDebugLogs)
            Debug.Log("Player discovered! Game restarting...");
        if (ScreenUIManager.Instance != null)
        {
            ScreenUIManager.Instance.ShowGameOver();
        }

        // Stop enemy movement and audio
        if (agent) agent.isStopped = true;
        if (footstepsAudio) footstepsAudio.Stop();
        if (breathingAudio) breathingAudio.Stop();

        // Delay game restart
        StartCoroutine(RestartGameAfterDelay());
    }

    IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        currentState = EnemyState.Waiting;
        currentStateCoroutine = null; // Clear other coroutine references

        // Stop moving
        StopMoving();

        if (enableDebugLogs)
            Debug.Log($"Waiting at patrol point {currentPatrolIndex} for {waitTime} seconds");

        // Wait for specified time
        yield return new WaitForSeconds(waitTime);

        // Move to next patrol point
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
        currentState = EnemyState.Patrolling;

        // Go to next point
        GoToNextPatrolPoint();
    }

    // Public method: external call to make enemy investigate a location
    public void InvestigateSound(Vector3 soundPosition)
    {
        if (enableDebugLogs)
            Debug.Log($"=== Investigation request received ===");
        Debug.Log($"Current state: {currentState}");
        Debug.Log($"Sound position: {soundPosition}");

        // Only ignore investigation requests when in chase state
        if (currentState == EnemyState.Chasing || gameEnded)
        {
            if (enableDebugLogs)
                Debug.Log("Enemy is chasing player or game ended, ignoring investigation request");
            return;
        }

        investigationTarget = soundPosition;
        hasInvestigationTarget = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Set investigation target: {soundPosition}");
            Debug.Log("Force start investigation (regardless of current state)");
        }

        // Immediately start investigation regardless of current state (except chase state)
        StartInvestigating();
    }

    // Debug: Display patrol paths and detection ranges in Scene view
    void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length < 2) return;

        // Draw patrol path
        Gizmos.color = Color.blue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            // Draw patrol points
            Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);

            // Draw path lines
            Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];
            if (nextPoint != null)
            {
                Gizmos.DrawLine(patrolPoints[i].position, nextPoint.position);
            }
        }

        // Highlight current target point
        if (Application.isPlaying && currentPatrolIndex < patrolPoints.Length && patrolPoints[currentPatrolIndex] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(patrolPoints[currentPatrolIndex].position, 0.7f);
        }

        // Draw investigation target
        if (Application.isPlaying && hasInvestigationTarget)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(investigationTarget, 0.8f);
            Gizmos.DrawLine(transform.position, investigationTarget);
        }

        // Draw player detection range
        if (enablePlayerDetection)
        {
            // Draw visual detection range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionDistance);

            // Draw hearing detection range
            if (enableHearing)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange
                Gizmos.DrawWireSphere(transform.position, hearingDistance);
            }

            // Draw field of view range
            if (enableLineOfSight)
            {
                Vector3 forward = transform.forward;
                Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
                Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;

                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, leftBoundary * detectionDistance);
                Gizmos.DrawRay(transform.position, rightBoundary * detectionDistance);

                // Draw field of view sector edges
                Vector3 lastDirection = leftBoundary;
                for (int i = 1; i <= 20; i++)
                {
                    float angle = (-viewAngle / 2f) + (viewAngle / 20f) * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
                    Gizmos.DrawLine(transform.position + lastDirection * detectionDistance,
                                   transform.position + direction * detectionDistance);
                    lastDirection = direction;
                }
            }

            // Draw back hearing area
            if (enableHearing)
            {
                Vector3 backward = -transform.forward;
                Vector3 backLeftBoundary = Quaternion.Euler(0, -90f, 0) * backward;
                Vector3 backRightBoundary = Quaternion.Euler(0, 90f, 0) * backward;

                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, backLeftBoundary * hearingDistance);
                Gizmos.DrawRay(transform.position, backRightBoundary * hearingDistance);
                Gizmos.DrawRay(transform.position, backward * hearingDistance);
            }

            // Draw detection ray to player
            if (Application.isPlaying && playerTransform != null)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
                Vector3 rayTarget = playerTransform.position + Vector3.up * 1f;

                if (canSeePlayer)
                {
                    Gizmos.color = Color.green; // Can detect player
                }
                else
                {
                    Gizmos.color = Color.red;   // Cannot detect player
                }
                Gizmos.DrawLine(rayOrigin, rayTarget);
            }
        }

        // Draw current state information
        if (Application.isPlaying && enableDebugLogs)
        {
            Vector3 textPosition = transform.position + Vector3.up * 3f;
#if UNITY_EDITOR
            Handles.Label(textPosition, $"State: {currentState}\nTimer: {stateTimer:F1}s\nCan See: {canSeePlayer}");
#endif
        }
    }
}