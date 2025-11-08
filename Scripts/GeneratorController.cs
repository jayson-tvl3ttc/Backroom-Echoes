using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GeneratorController : MonoBehaviour
{
    [Header("Generator Settings")]
    public bool isActivated = false;
    public float activationTime = 16f; // Time needed to complete startup

    [Header("Audio Settings")]
    public AudioSource generatorAudio;
    public AudioClip startupClipWithRunning; // Startup sound + running sound
    public AudioClip runningClipLoop;        // Pure running sound (for looping)
    public AudioClip faultSound;             // Fault sound (loops when not activated)

    [Header("UI Settings")]
    public Canvas progressCanvas;
    public Image progressFill; // Progress bar fill part
    public GameObject progressBarUI; // Entire progress bar UI

    [Header("Visual Feedback")]
    public Light generatorLight; // Optional: generator light
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;

    // Private variables
    private float currentProgress = 0f; // Current progress (0-1)
    private bool isBeingActivated = false;
    //private bool hasStartedAudio = false;



    private XRSimpleInteractable interactable;
    void Awake()
    {
        // Get XR Simple Interactable on the same object
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        InitializeGenerator();
    }

    void InitializeGenerator()
    {
        // Hide progress bar
        if (progressBarUI) progressBarUI.SetActive(false);

        // Set initial audio - play fault sound
        if (generatorAudio && faultSound)
        {
            generatorAudio.clip = faultSound;
            generatorAudio.loop = true;  // Fault sound loops
            generatorAudio.Play();
        }

        // Set lighting
        if (generatorLight)
        {
            generatorLight.color = inactiveColor;

        }

        // Set progress bar
        UpdateProgressBar();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {

        if (!isActivated)
        {

            StartActivation();
        }
        else
        {
            Debug.Log("Generator already activated, skipping");
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (!isActivated)
        {
            StopActivation();
        }
    }

    void StartActivation()
    {
        isBeingActivated = true;
        //hasStartedAudio = false;

        // Show progress bar
        if (progressBarUI) progressBarUI.SetActive(true);

        // Stop fault sound, start playing startup audio
        if (generatorAudio)
        {
            generatorAudio.Stop(); // Stop fault sound

            if (startupClipWithRunning)
            {
                generatorAudio.clip = startupClipWithRunning;
                generatorAudio.loop = false; // Startup audio doesn't loop
                generatorAudio.Play();
                //hasStartedAudio = true;
            }
        }
        //NotifyEnemiesOfSound();// Notify enemies of generator activation (louder sound)

        Debug.Log("Starting generator activation");
    }

    void StopActivation()
    {
        if (isActivated) return; // If already fully activated, don't stop

        isBeingActivated = false;

        // Stop audio
        // Stop startup audio, restore fault sound
        if (generatorAudio)
        {
            generatorAudio.Stop();

            if (faultSound)
            {
                generatorAudio.clip = faultSound;
                generatorAudio.loop = true; // Fault sound loops
                generatorAudio.Play();
            }
        }

        // Stop light flickering
        generatorLight.color = inactiveColor;
        generatorLight.intensity = 0.06f;

        // Hide progress bar
        if (progressBarUI) progressBarUI.SetActive(false);

        // Reset progress

        currentProgress = 0f;
        UpdateProgressBar();
        //hasStartedAudio = false;

        Debug.Log("Stopping generator activation");
    }

    void Update()
    {

        if (isBeingActivated && !isActivated)
        {


            // Increase progress
            currentProgress += Time.deltaTime / activationTime;
            currentProgress = Mathf.Clamp01(currentProgress);

            float flickerSpeed = 5f + currentProgress * 10f; // Higher progress means faster flickering
            float flicker = Mathf.Sin(Time.time * flickerSpeed) * 0.5f + 0.5f;
            generatorLight.intensity = flicker * 1f * currentProgress;

            // Color gradient from red to yellow
            generatorLight.color = Color.Lerp(inactiveColor, Color.yellow, currentProgress);

            // Update progress bar
            UpdateProgressBar();

            // Check if activation is complete
            if (currentProgress >= 1f)
            {
                CompleteActivation();
            }
        }
    }

    void UpdateProgressBar()
    {
        if (progressFill)
        {
            progressFill.fillAmount = currentProgress;
        }
    }

    void CompleteActivation()
    {
        isActivated = true;
        isBeingActivated = false;
        currentProgress = 1f;

        // Hide progress bar
        if (progressBarUI) progressBarUI.SetActive(false);

        // Enable lighting
        if (generatorLight)
        {

            generatorLight.color = activeColor;
            generatorLight.intensity = 0.06f; // Stable brightness
        }

        // Switch to looping running audio
        if (generatorAudio && runningClipLoop)
        {
            generatorAudio.Stop(); // Stop startup audio
            generatorAudio.clip = runningClipLoop; // Switch to running audio
            generatorAudio.loop = true; // Set to loop
            generatorAudio.Play(); // Play running audio
        }

        // Notify enemies of generator completion (louder sound)
        NotifyEnemiesOfSound();

        Debug.Log("Generator activation complete!");

        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGeneratorActivated(this);
        }
        else
        {
            Debug.LogWarning("GameManager not found, cannot notify generator activation status");
        }
    }
    void NotifyEnemiesOfSound()
    {
        // Find all enemies in the scene
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();

        Debug.Log($"=== NotifyEnemiesOfSound ===");
        Debug.Log($"Found {enemies.Length} enemies");
        Debug.Log($"Generator position: {transform.position}");

        foreach (EnemyAI enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // Determine sound intensity based on distance
            float soundRange = isActivated ? 15f : 10f; // Sound is louder when activation is complete

            Debug.Log($"Enemy distance: {distance:F2}m, Sound range: {soundRange}m");

            enemy.InvestigateSound(transform.position);

        }

        if (enemies.Length > 0)
            Debug.Log($"=== Notification complete, processed {enemies.Length} enemies ===");
    }

}