using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScreenUIManager : MonoBehaviour
{
    public static ScreenUIManager Instance;

    [Header("Manually Assigned UI Elements")]
    public TextMeshProUGUI progressText;        // Progress text
    public Slider progressSlider;               // Progress bar
    public GameObject messagePanel;             // Message panel
    public TextMeshProUGUI messageText;         // Message text
    public GameObject victoryPanel;             // Victory panel
    public GameObject gameOverPanel;            // Game over panel

    [Header("Message Display Duration")]
    public float messageDisplayTime = 3f;

    // Private variables
    private Coroutine messageCoroutine;

    void Awake()
    {
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
        // Initialize UI state
        InitializeUI();
    }

    void InitializeUI()
    {
        // Hide message panel
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // Hide victory panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Initialize progress display
        UpdateGeneratorProgress(0, 3);
    }

    // Update generator progress display
    public void UpdateGeneratorProgress(int activated, int total)
    {
        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"Generators: {activated}/{total}";

            // Change color based on progress
            if (activated == total)
            {
                progressText.color = Color.green; // Green when complete
            }
            else if (activated >= total / 2)
            {
                progressText.color = Color.yellow; // Yellow when over halfway
            }
            else
            {
                progressText.color = Color.white; // Default white
            }
        }

        // Update progress bar
        if (progressSlider != null)
        {
            float progress = total > 0 ? (float)activated / total : 0f;
            progressSlider.value = progress;
        }

        Debug.Log($"UI progress updated: {activated}/{total}");
    }

    // Show message on screen
    public void ShowMessage(string message, float duration = 0f)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (messagePanel != null)
        {
            // Stop any previous message coroutine
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }

            // Show message panel
            messagePanel.SetActive(true);

            // Auto-hide if duration specified
            float displayTime = duration > 0 ? duration : messageDisplayTime;
            messageCoroutine = StartCoroutine(HideMessageAfterDelay(displayTime));
        }

        Debug.Log($"Showing message: {message}");
    }

    // Hide message after delay
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    // Show victory screen
    public void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        Debug.Log("Victory screen displayed.");
    }

    // Show game over screen
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log("Game over screen displayed.");
    }

    // Hide all UI panels
    public void HideAllPanels()
    {
        if (messagePanel != null) messagePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // Manually hide message
    public void HideMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }
}
