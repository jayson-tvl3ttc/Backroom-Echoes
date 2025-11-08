using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Objects")]
    public Transform elevatorTrigger;           // Elevator trigger zone
    public Transform player;                    // Player object

    [Header("Generator Settings")]
    public GeneratorController[] generators;    // Manually assigned generators
    public bool autoFindGenerators = true;      // Automatically find generators

    [Header("Elevator Settings")]
    public float elevatorDetectionRadius = 2f;  // Radius to detect elevator entry

    [Header("Victory Settings")]
    public float successDelay = 3f;             // Delay before restarting after success
    public AudioSource victoryAudioSource;      // Audio source for victory sound
    public AudioClip victorySound;              // Victory sound clip

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    // Private variables
    private int totalGenerators;
    private int activatedGenerators;
    private bool allGeneratorsActivated = false;
    private bool gameCompleted = false;
    private bool playerInElevator = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        // Find player
        if (player == null)
        {
            FindPlayer();
        }

        // Find generators
        if (autoFindGenerators)
        {
            FindAllGenerators();
        }

        // Initialize generator count
        totalGenerators = generators.Length;
        activatedGenerators = 0;

        // Setup victory sound
        SetupVictoryAudio();

        // Initialize UI display
        if (ScreenUIManager.Instance != null)
        {
            ScreenUIManager.Instance.UpdateGeneratorProgress(activatedGenerators, totalGenerators);
        }
        else
        {
            Debug.LogWarning("SimpleUIManager not found. UI functions will be unavailable.");
        }

        if (enableDebugLogs)
        {
            Debug.Log("GameManager initialized.");
            Debug.Log($"Total generators: {totalGenerators}");
            Debug.Log($"Player: {(player ? player.name : "Not found")}");
            Debug.Log($"Elevator location: {(elevatorTrigger ? elevatorTrigger.position.ToString() : "Not set")}");
        }
    }

    void FindPlayer()
    {
        // Find player using different methods
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            playerObject = GameObject.Find("XR Origin (XR Rig)");
        }

        if (playerObject == null && Camera.main != null)
        {
            playerObject = Camera.main.gameObject;
        }

        if (playerObject != null)
        {
            player = playerObject.transform;
            if (enableDebugLogs)
                Debug.Log($"Player found: {player.name}");
        }
        else
        {
            Debug.LogWarning("Player object not found!");
        }
    }

    void FindAllGenerators()
    {
        GeneratorController[] foundGenerators = FindObjectsOfType<GeneratorController>();
        generators = foundGenerators;

        if (enableDebugLogs)
            Debug.Log($"Automatically found {generators.Length} generators.");
    }

    void SetupVictoryAudio()
    {
        if (victoryAudioSource == null)
        {
            victoryAudioSource = gameObject.AddComponent<AudioSource>();
        }

        victoryAudioSource.clip = victorySound;
        victoryAudioSource.spatialBlend = 0f; // 2D audio
        victoryAudioSource.playOnAwake = false;
    }

    void Update()
    {
        if (gameCompleted) return;

        // Check if player is in elevator zone
        CheckPlayerInElevator();

        // If all generators are activated and player is in the elevator, game is won
        if (allGeneratorsActivated && playerInElevator)
        {
            GameSuccess();
        }
    }

    void CheckPlayerInElevator()
    {
        if (player == null || elevatorTrigger == null) return;

        float distance = Vector3.Distance(player.position, elevatorTrigger.position);
        bool wasInElevator = playerInElevator;
        playerInElevator = distance <= elevatorDetectionRadius;

        // Log on status change
        if (playerInElevator != wasInElevator && enableDebugLogs)
        {
            Debug.Log($"Player elevator state changed: {wasInElevator} -> {playerInElevator}");
            if (playerInElevator)
            {
                Debug.Log("Player entered elevator area.");
                if (allGeneratorsActivated)
                {
                    Debug.Log("All generators activated. Preparing for game success!");
                }
                else
                {
                    Debug.Log($"Remaining generators to activate: {totalGenerators - activatedGenerators}");
                }
            }
        }
    }

    // Called by generator
    public void OnGeneratorActivated(GeneratorController generator)
    {
        activatedGenerators++;

        // Update UI
        if (ScreenUIManager.Instance != null)
        {
            ScreenUIManager.Instance.UpdateGeneratorProgress(activatedGenerators, totalGenerators);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Generator {generator.name} activated!");
            Debug.Log($"Progress: {activatedGenerators}/{totalGenerators}");
        }

        // Check if all generators are activated
        if (activatedGenerators >= totalGenerators)
        {
            AllGeneratorsActivated();
        }
    }

    void AllGeneratorsActivated()
    {
        allGeneratorsActivated = true;

        if (enableDebugLogs)
            Debug.Log("🎉 All generators activated! Head to the elevator to escape!");

        // Add UI hint or sound here
        ShowCompletionHint();
    }

    void ShowCompletionHint()
    {
        // Display UI hint
        if (ScreenUIManager.Instance != null)
        {
            ScreenUIManager.Instance.ShowMessage("All generators activated! \nHead to the elevator to escape!", 5f);
        }

        if (enableDebugLogs)
            Debug.Log("Hint: All generators activated. Go to the elevator!");

        // Play hint sound
        if (victoryAudioSource && victorySound)
        {
            victoryAudioSource.pitch = 1.5f;
            victoryAudioSource.volume = 0.5f;
            victoryAudioSource.PlayOneShot(victorySound);
        }
    }

    void GameSuccess()
    {
        if (gameCompleted) return;

        gameCompleted = true;

        // Show victory UI
        if (ScreenUIManager.Instance != null)
        {
            ScreenUIManager.Instance.ShowVictory();
        }

        if (enableDebugLogs)
            Debug.Log("Game complete! Player escaped successfully!");

        // Play victory sound
        PlayVictorySound();

        // Stop enemies
        StopAllEnemies();

        // Restart game after delay
        StartCoroutine(RestartGameAfterDelay());
    }

    void PlayVictorySound()
    {
        if (victoryAudioSource && victorySound)
        {
            victoryAudioSource.pitch = 1f;
            victoryAudioSource.volume = 1f;
            victoryAudioSource.Play();

            if (enableDebugLogs)
                Debug.Log("Playing victory sound");
        }
    }

    void StopAllEnemies()
    {
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in enemies)
        {
            // Stop enemy movement
            if (enemy.GetComponent<UnityEngine.AI.NavMeshAgent>())
            {
                enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = true;
            }

            // Other logic to stop enemy
            enemy.enabled = false;
        }

        if (enableDebugLogs)
            Debug.Log($"Stopped {enemies.Length} enemies.");
    }

    IEnumerator RestartGameAfterDelay()
    {
        if (enableDebugLogs)
            Debug.Log($"Restarting game in {successDelay} seconds...");

        yield return new WaitForSeconds(successDelay);

        if (enableDebugLogs)
            Debug.Log("Reloading scene...");

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [System.Obsolete("For debugging only")]
    public void DebugCompleteGame()
    {
        if (Application.isEditor)
        {
            activatedGenerators = totalGenerators;
            AllGeneratorsActivated();
            Debug.Log("Debug: Game manually completed.");
        }
    }

    // Visualize elevator detection range in editor
    void OnDrawGizmos()
    {
        if (elevatorTrigger != null)
        {
            Gizmos.color = playerInElevator ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(elevatorTrigger.position, elevatorDetectionRadius);

            if (player != null && Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(player.position, elevatorTrigger.position);
            }
        }
    }
}
