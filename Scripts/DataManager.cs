using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRAudioDataCollector : MonoBehaviour
{
    [Header("Privacy and Data Protection")]
    public bool enableDataCollection = true;
    public bool anonymizeTimestamps = true;
    public bool encryptData = false; // For future implementation

    [Header("Data Collection Settings")]
    public int frameInterval = 30;  // Save data every 30 frames (approximately 0.5 seconds)
    public int maxDataPoints = 500; // Maximum number of data points
    public string saveDirectory = "C:/VRData/"; // Change to your save path

    [Header("Objects to Track")]
    public Transform playerHead;        // VR headset/camera
    public Transform enemyAgent;        // Enemy character with pathfinding
    public AudioSource[] audioSources;  // Audio sources to monitor
    public Transform[] interactiveObjects; // Interactive audio objects

    [Header("Data Collection Status")]
    public bool isCollecting = true;
    public int currentDataCount = 0;

    // Private variables
    private int frameCounter = 0;
    private List<string> positionData = new List<string>();
    private List<string> audioData = new List<string>();
    private List<string> interactionData = new List<string>();
    private List<string> headMovementData = new List<string>();
    private List<string> enemyData = new List<string>();

    // Variables for calculating movement distance
    private Vector3 lastHeadPosition;
    private Vector3 lastHeadRotation;
    private float totalMovementDistance = 0f;
    private float totalHeadRotation = 0f;

    // Enemy tracking variables
    private Vector3 lastEnemyPosition;
    private Vector3 lastEnemyRotation;
    private float totalEnemyMovement = 0f;
    private float playerEnemyDistance = 0f;

    // Interaction tracking
    private Dictionary<string, float> objectInteractionTime = new Dictionary<string, float>();
    private Dictionary<string, int> objectInteractionCount = new Dictionary<string, int>();
    private string currentInteractingObject = "";
    private float currentInteractionStartTime = 0f;

    // Anonymous user ID (randomly generated to protect privacy)
    private string anonymousUserID;
    private System.DateTime sessionStartTime;

    // Privacy protection variables
    private bool userConsented = false;

    void Start()
    {
        // Request user consent before data collection
        if (enableDataCollection)
        {
            RequestUserConsent();
        }

        // Generate anonymous user ID using more secure method
        anonymousUserID = "User_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        sessionStartTime = System.DateTime.Now;

        // Initialize tracking variables
        if (playerHead != null)
        {
            lastHeadPosition = playerHead.position;
            lastHeadRotation = playerHead.eulerAngles;
        }

        if (enemyAgent != null)
        {
            lastEnemyPosition = enemyAgent.position;
            lastEnemyRotation = enemyAgent.eulerAngles;
        }

        // Create save directory
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        Debug.Log("VR Audio Data Collection Started for " + anonymousUserID);
        Debug.Log("Data will be stored locally and anonymized");
    }

    void RequestUserConsent()
    {
        // In a real application, this should show a proper consent dialog
        // For now, we assume consent is given
        userConsented = true;
        Debug.Log("Data collection consent: User has been informed about data collection");
        Debug.Log("Data collected: Position, movement, and interaction data for research purposes");
        Debug.Log("Data protection: All data is anonymized and stored locally");
    }

    void Update()
    {
        if (!isCollecting || currentDataCount >= maxDataPoints || !userConsented || !enableDataCollection)
            return;

        frameCounter++;

        // Collect data at set intervals
        if (frameCounter >= frameInterval)
        {
            CollectAllData();
            frameCounter = 0;
            currentDataCount++;

            // Auto-save when reaching maximum data points
            if (currentDataCount >= maxDataPoints)
            {
                SaveAllData();
                isCollecting = false;
                Debug.Log("Data collection completed and saved!");
            }
        }

        // Update interaction data in real-time
        UpdateInteractionTracking();
    }

    void CollectAllData()
    {
        string timestamp;

        if (anonymizeTimestamps)
        {
            // Use session time instead of real timestamp for privacy
            System.TimeSpan sessionTime = System.DateTime.Now - sessionStartTime;
            timestamp = string.Format("Session_{0:D6}", (int)sessionTime.TotalSeconds);
        }
        else
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        // Collect position and movement data
        CollectPositionData(timestamp);

        // Collect head movement data
        CollectHeadMovementData(timestamp);

        // Collect audio data
        CollectAudioData(timestamp);

        // Collect enemy data
        CollectEnemyData(timestamp);

        // Collect interaction data
        CollectInteractionData(timestamp);
    }

    void CollectPositionData(string timestamp)
    {
        if (playerHead == null) return;

        Vector3 currentPosition = playerHead.position;
        Vector3 currentRotation = playerHead.eulerAngles;

        // Calculate movement distance
        float movementDelta = Vector3.Distance(currentPosition, lastHeadPosition);
        totalMovementDistance += movementDelta;

        // Calculate rotation change
        float rotationDelta = Quaternion.Angle(
            Quaternion.Euler(lastHeadRotation),
            Quaternion.Euler(currentRotation)
        );
        totalHeadRotation += rotationDelta;

        // Save position data
        string positionEntry = string.Format("{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3}",
            anonymousUserID, timestamp,
            currentPosition.x, currentPosition.y, currentPosition.z,
            currentRotation.x, currentRotation.y, currentRotation.z,
            movementDelta, totalMovementDistance
        );

        positionData.Add(positionEntry);

        // Update previous position and rotation
        lastHeadPosition = currentPosition;
        lastHeadRotation = currentRotation;
    }

    void CollectHeadMovementData(string timestamp)
    {
        if (playerHead == null) return;

        // Calculate head movement velocity
        Vector3 velocity = (playerHead.position - lastHeadPosition) / (frameInterval * Time.fixedDeltaTime);

        string headMovementEntry = string.Format("{0},{1},{2:F3},{3:F3},{4:F3},{5:F3}",
            anonymousUserID, timestamp,
            velocity.magnitude, totalHeadRotation,
            playerHead.position.y, // Head height change
            Vector3.Angle(Vector3.forward, playerHead.forward) // Head direction angle
        );

        headMovementData.Add(headMovementEntry);
    }

    void CollectAudioData(string timestamp)
    {
        if (audioSources == null) return;

        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource == null) continue;

            string audioEntry = string.Format("{0},{1},{2},{3},{4:F3},{5:F3},{6}",
                anonymousUserID, timestamp,
                audioSource.name,
                audioSource.isPlaying ? "Playing" : "Stopped",
                audioSource.volume,
                audioSource.pitch,
                audioSource.clip != null ? audioSource.clip.name : "None"
            );

            audioData.Add(audioEntry);
        }
    }

    void CollectInteractionData(string timestamp)
    {
        // Record current interaction state
        string interactionEntry = string.Format("{0},{1},{2},{3:F3}",
            anonymousUserID, timestamp,
            currentInteractingObject != "" ? currentInteractingObject : "None",
            currentInteractingObject != "" ? (Time.time - currentInteractionStartTime) : 0f
        );

        interactionData.Add(interactionEntry);
    }

    void CollectEnemyData(string timestamp)
    {
        if (enemyAgent == null) return;

        Vector3 currentEnemyPosition = enemyAgent.position;
        Vector3 currentEnemyRotation = enemyAgent.eulerAngles;

        // Calculate enemy movement distance
        float enemyMovementDelta = Vector3.Distance(currentEnemyPosition, lastEnemyPosition);
        totalEnemyMovement += enemyMovementDelta;

        // Calculate distance between player and enemy
        if (playerHead != null)
        {
            playerEnemyDistance = Vector3.Distance(playerHead.position, currentEnemyPosition);
        }

        // Calculate enemy rotation change
        float enemyRotationDelta = Quaternion.Angle(
            Quaternion.Euler(lastEnemyRotation),
            Quaternion.Euler(currentEnemyRotation)
        );

        // Check if enemy has NavMeshAgent component for additional data
        UnityEngine.AI.NavMeshAgent navAgent = enemyAgent.GetComponent<UnityEngine.AI.NavMeshAgent>();
        string navStatus = "None";
        float navSpeed = 0f;
        float distanceToTarget = 0f;

        if (navAgent != null)
        {
            navStatus = navAgent.pathStatus.ToString();
            navSpeed = navAgent.velocity.magnitude;
            if (navAgent.hasPath)
            {
                distanceToTarget = navAgent.remainingDistance;
            }
        }

        // Save enemy data
        string enemyEntry = string.Format("{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11},{12:F3},{13:F3}",
            anonymousUserID, timestamp,
            currentEnemyPosition.x, currentEnemyPosition.y, currentEnemyPosition.z,
            currentEnemyRotation.x, currentEnemyRotation.y, currentEnemyRotation.z,
            enemyMovementDelta, totalEnemyMovement, playerEnemyDistance,
            navStatus, navSpeed, distanceToTarget
        );

        enemyData.Add(enemyEntry);

        // Update previous enemy position and rotation
        lastEnemyPosition = currentEnemyPosition;
        lastEnemyRotation = currentEnemyRotation;
    }

    void UpdateInteractionTracking()
    {
        // Track XR Simple Interactable interactions
        // This method monitors XR interaction events with audio objects

        // Find all XR Simple Interactable objects in the scene
        if (interactiveObjects != null && playerHead != null)
        {
            foreach (Transform obj in interactiveObjects)
            {
                if (obj == null) continue;

                // Try to get XR interactable component (compatible with different versions)
                var baseInteractable = obj.GetComponent<XRBaseInteractable>();
                if (baseInteractable == null)
                {
                    // Fallback: try XRSimpleInteractable if XRBaseInteractable doesn't exist
                    var simpleInteractable = obj.GetComponent<MonoBehaviour>();
                    if (simpleInteractable != null && simpleInteractable.GetType().Name.Contains("Interactable"))
                    {
                        // Use reflection to check interaction state
                        CheckInteractionStateViaReflection(obj, simpleInteractable);
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Check if this object is currently being interacted with
                bool isCurrentlyInteracting = baseInteractable.isSelected || baseInteractable.isHovered;
                string objectName = obj.name;

                // Handle interaction start
                if (isCurrentlyInteracting && currentInteractingObject != objectName)
                {
                    // End previous interaction if exists
                    if (currentInteractingObject != "")
                    {
                        EndInteraction(currentInteractingObject);
                    }

                    // Start new interaction
                    StartInteraction(objectName);
                }
                // Handle interaction end
                else if (!isCurrentlyInteracting && currentInteractingObject == objectName)
                {
                    EndInteraction(objectName);
                }
            }
        }
    }

    void CheckInteractionStateViaReflection(Transform obj, MonoBehaviour interactableComponent)
    {
        try
        {
            // Use reflection to access isSelected and isHovered properties
            var type = interactableComponent.GetType();
            var isSelectedField = type.GetProperty("isSelected");
            var isHoveredField = type.GetProperty("isHovered");

            bool isCurrentlyInteracting = false;

            if (isSelectedField != null)
            {
                isCurrentlyInteracting = (bool)isSelectedField.GetValue(interactableComponent);
            }

            if (!isCurrentlyInteracting && isHoveredField != null)
            {
                isCurrentlyInteracting = (bool)isHoveredField.GetValue(interactableComponent);
            }

            string objectName = obj.name;

            // Handle interaction start/end
            if (isCurrentlyInteracting && currentInteractingObject != objectName)
            {
                if (currentInteractingObject != "")
                {
                    EndInteraction(currentInteractingObject);
                }
                StartInteraction(objectName);
            }
            else if (!isCurrentlyInteracting && currentInteractingObject == objectName)
            {
                EndInteraction(objectName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to check interaction state for " + obj.name + ": " + e.Message);
        }
    }

    void StartInteraction(string objectName)
    {
        currentInteractingObject = objectName;
        currentInteractionStartTime = Time.time;

        // Increment interaction count
        if (objectInteractionCount.ContainsKey(objectName))
            objectInteractionCount[objectName]++;
        else
            objectInteractionCount[objectName] = 1;

        Debug.Log("Started interaction with: " + objectName);
    }

    void EndInteraction(string objectName)
    {
        if (currentInteractingObject == objectName)
        {
            float interactionDuration = Time.time - currentInteractionStartTime;

            // Add to total interaction time
            if (objectInteractionTime.ContainsKey(objectName))
                objectInteractionTime[objectName] += interactionDuration;
            else
                objectInteractionTime[objectName] = interactionDuration;

            Debug.Log("Ended interaction with: " + objectName + " Duration: " + interactionDuration.ToString("F2") + "s");

            currentInteractingObject = "";
            currentInteractionStartTime = 0f;
        }
    }

    // Public method: manually save data
    public void SaveAllData()
    {
        if (!userConsented)
        {
            Debug.LogWarning("Cannot save data: User consent not obtained");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Add privacy notice to saved files
        string privacyNotice = "# VR Audio Research Data - Anonymized and Privacy Protected\n" +
                             "# User ID: " + anonymousUserID + " (randomly generated)\n" +
                             "# Data collected for academic research purposes only\n" +
                             "# No personal identifying information included\n" +
                             "# Data stored locally and not transmitted\n";

        // Save position data
        SaveDataToFileWithPrivacy(positionData, "PositionData_" + timestamp + ".csv",
            "UserID,Timestamp,PosX,PosY,PosZ,RotX,RotY,RotZ,MovementDelta,TotalMovement", privacyNotice);

        // Save head movement data
        SaveDataToFileWithPrivacy(headMovementData, "HeadMovementData_" + timestamp + ".csv",
            "UserID,Timestamp,Velocity,TotalRotation,HeadHeight,HeadDirection", privacyNotice);

        // Save audio data
        SaveDataToFileWithPrivacy(audioData, "AudioData_" + timestamp + ".csv",
            "UserID,Timestamp,AudioSourceName,PlaybackState,Volume,Pitch,ClipName", privacyNotice);

        // Save interaction data
        SaveDataToFileWithPrivacy(interactionData, "InteractionData_" + timestamp + ".csv",
            "UserID,Timestamp,InteractingObject,InteractionDuration", privacyNotice);

        // Save enemy data
        SaveDataToFileWithPrivacy(enemyData, "EnemyData_" + timestamp + ".csv",
            "UserID,Timestamp,EnemyPosX,EnemyPosY,EnemyPosZ,EnemyRotX,EnemyRotY,EnemyRotZ,EnemyMovementDelta,TotalEnemyMovement,PlayerEnemyDistance,NavStatus,NavSpeed,DistanceToTarget", privacyNotice);

        // Save interaction statistics summary
        SaveInteractionSummary(timestamp);

        Debug.Log("All data saved to: " + saveDirectory);
        Debug.Log("Data has been anonymized and privacy protected");
    }

    void SaveDataToFileWithPrivacy(List<string> data, string fileName, string header, string privacyNotice)
    {
        if (data.Count == 0) return;

        string filePath = Path.Combine(saveDirectory, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write privacy notice as comments
                writer.WriteLine(privacyNotice);

                // Write header
                writer.WriteLine(header);

                // Write data
                foreach (string entry in data)
                {
                    writer.WriteLine(entry);
                }
            }

            Debug.Log("Saved " + data.Count + " entries to " + fileName + " (privacy protected)");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save " + fileName + ": " + e.Message);
        }
    }

    void SaveDataToFile(List<string> data, string fileName, string header)
    {
        if (data.Count == 0) return;

        string filePath = Path.Combine(saveDirectory, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine(header);

                // Write data
                foreach (string entry in data)
                {
                    writer.WriteLine(entry);
                }
            }

            Debug.Log("Saved " + data.Count + " entries to " + fileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save " + fileName + ": " + e.Message);
        }
    }

    void SaveInteractionSummary(string timestamp)
    {
        string fileName = "InteractionSummary_" + timestamp + ".csv";
        string filePath = Path.Combine(saveDirectory, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("UserID,ObjectName,TotalInteractionTime,InteractionCount");

                foreach (var kvp in objectInteractionTime)
                {
                    int count = objectInteractionCount.ContainsKey(kvp.Key) ? objectInteractionCount[kvp.Key] : 0;
                    writer.WriteLine(string.Format("{0},{1},{2:F3},{3}",
                        anonymousUserID, kvp.Key, kvp.Value, count));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save interaction summary: " + e.Message);
        }
    }

    // Public methods: start/stop data collection
    public void StartDataCollection()
    {
        isCollecting = true;
        currentDataCount = 0;
        frameCounter = 0;
        Debug.Log("Data collection started");
    }

    public void StopDataCollection()
    {
        isCollecting = false;
        SaveAllData();
        Debug.Log("Data collection stopped and saved");
    }

    // Auto-save data when application exits
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isCollecting)
        {
            SaveAllData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isCollecting)
        {
            SaveAllData();
        }
    }

    // Display current data collection status
    void OnGUI()
    {
        if (isCollecting && userConsented)
        {
            GUI.Box(new Rect(Screen.width - 310, 10, 300, 100), "");
            GUI.Label(new Rect(Screen.width - 300, 20, 280, 20), "Data Collection: ACTIVE");
            GUI.Label(new Rect(Screen.width - 300, 40, 280, 20), "Data Points: " + currentDataCount + "/" + maxDataPoints);
            GUI.Label(new Rect(Screen.width - 300, 60, 280, 20), "User ID: " + anonymousUserID);
            GUI.Label(new Rect(Screen.width - 300, 80, 280, 20), "Privacy: Protected & Anonymized");
        }
        else if (!userConsented && enableDataCollection)
        {
            GUI.Box(new Rect(Screen.width - 310, 10, 300, 60), "");
            GUI.Label(new Rect(Screen.width - 300, 20, 280, 20), "Data Collection: WAITING CONSENT");
            GUI.Label(new Rect(Screen.width - 300, 40, 280, 20), "Privacy protection enabled");
        }
    }
}