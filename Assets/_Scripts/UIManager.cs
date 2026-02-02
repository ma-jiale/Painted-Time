using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all UI elements in the game including main menu and victory screen.
/// This is a singleton that persists across scenes.
/// </summary>
public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("The main menu panel shown at game start")]
    public GameObject mainMenuPanel;
    
    [Tooltip("The victory screen panel shown when game is completed")]
    public GameObject victoryPanel;

    [Tooltip("The pause menu panel shown at game pause")]
    public GameObject pauseMenuPanel;

    [Header("Main Menu Buttons")]
    [Tooltip("Button to start the game")]
    public Button startGameButton;
    
    [Tooltip("Button to quit the game")]
    public Button quitButton;

    [Header("Pause Menu Buttons")]
    public Button resumeButton;
    public Button quitFromPauseButton;

    [Header("Victory Screen Buttons")]
    [Tooltip("Button to quit from victory screen")]
    public Button quitFromVictoryButton;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Keep this object when loading new scenes
        }
        else
        {
            Destroy(gameObject);  // Destroy duplicate instances
            return;
        }
    }

    private void Start()
    {
        // Set up button listeners
        SetupButtonListeners();
        
        // Show main menu at start
        ShowMainMenu();
    }

    /// <summary>
    /// Connects all UI buttons to their respective functions
    /// </summary>
    private void SetupButtonListeners()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        if (quitFromPauseButton != null)
        {
            quitFromPauseButton.onClick.AddListener(OnQuitClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (quitFromVictoryButton != null)
        {
            quitFromVictoryButton.onClick.AddListener(OnQuitClicked);
        }
    }

    /// <summary>
    /// Shows the main menu UI panel
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the pause menu UI panel
    /// </summary>
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the victory screen UI panel
    /// </summary>
    public void ShowVictoryScreen()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Hides all UI panels (used during gameplay)
    /// </summary>
    public void HideAllUI()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    // === Button Click Handlers ===

    /// <summary>
    /// Called when the Start Game button is clicked
    /// </summary>
    private void OnStartGameClicked()
    {
        Debug.Log("Start Game button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    /// <summary>
    /// Called when the Resume button is clicked
    /// </summary>
    private void OnResumeClicked()
    {
        Debug.Log("Resume Game button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    /// <summary>
    /// Called when the Quit button is clicked
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("Quit button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
}
