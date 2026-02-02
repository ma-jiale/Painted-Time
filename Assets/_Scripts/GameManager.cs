using UnityEngine;

/// <summary>
/// Main game manager that controls the overall game state and flow.
/// This is a singleton that persists across scenes.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Current state of the game
    /// </summary>
    public enum GameState
    {
        MainMenu,      // Player is in the main menu (Home scene)
        Playing,       // Player is actively playing
        Pause,         // Game is paused
        Victory        // Player has completed the game
    }

    [Header("Game State")]
    public GameState currentState = GameState.MainMenu;

    [Header("Player Settings")]
    public GameObject player;
    public bool canPlayerMove = false;  // Controls whether player can move

    [Header("Respawn Settings")]
    [Tooltip("Tag used to identify respawn points in the scene")]
    public string respawnPointTag = "RespawnPoint";
    [Tooltip("Fallback respawn point if no tagged points found")]
    public Transform fallbackRespawnPoint;

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
        // Start in main menu state
        SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// Changes the current game state and updates related systems
    /// </summary>
    /// <param name="newState">The new game state to transition to</param>
    public void SetGameState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                canPlayerMove = false;
                Time.timeScale = 1f;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMainMenu();
                }
                break;

            case GameState.Playing:
                canPlayerMove = true;
                Time.timeScale = 1f;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideAllUI();
                }
                break;

            case GameState.Pause:
                Time.timeScale = 0f;  // Pause game time
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPauseMenu();
                }
                break;

            case GameState.Victory:
                canPlayerMove = false;
                Time.timeScale = 1f;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowVictoryScreen();
                }
                break;
        }

        Debug.Log($"Game state changed to: {newState}");
    }

    /// <summary>
    /// Called when the player clicks "Start Game" button
    /// </summary>
    public void StartGame()
    {
        // Play opening story before starting
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.PlayStory("GameStart");
        }
        
        SetGameState(GameState.Playing);
    }

    /// <summary>
    /// Pauses the game
    /// </summary>
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Pause);
        }
    }

    /// <summary>
    /// Resumes the game from pause
    /// </summary>
    public void ResumeGame()
    {
        if (currentState == GameState.Pause)
        {
            SetGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// Toggles between playing and paused states
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Pause)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Called when the player completes the game (picks up the orb)
    /// </summary>
    public void CompleteGame()
    {
        SetGameState(GameState.Victory);
    }

    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Respawns the player at the closest respawn point
    /// Finds all objects with respawnPointTag and teleports player to the nearest one
    /// </summary>
    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogError("Cannot respawn: Player reference is null!");
            return;
        }

        // Find all respawn points in the scene
        GameObject[] respawnPoints = GameObject.FindGameObjectsWithTag(respawnPointTag);

        if (respawnPoints.Length == 0)
        {
            // No tagged respawn points found, try fallback
            if (fallbackRespawnPoint != null)
            {
                TeleportPlayerTo(fallbackRespawnPoint);
                Debug.Log($"Player respawned at fallback point: {fallbackRespawnPoint.name}");
            }
            else
            {
                Debug.LogError($"No respawn points found with tag '{respawnPointTag}' and no fallback point assigned!");
            }
            return;
        }

        // Find the closest respawn point to the player's current position
        Transform closestPoint = FindClosestRespawnPoint(player.transform.position, respawnPoints);

        if (closestPoint != null)
        {
            TeleportPlayerTo(closestPoint);
            Debug.Log($"Player respawned at closest point: {closestPoint.name}");
        }
    }

    /// <summary>
    /// Finds the closest respawn point to a given position
    /// </summary>
    /// <param name="fromPosition">The position to measure distance from (usually player's current position)</param>
    /// <param name="respawnPoints">Array of all respawn point GameObjects</param>
    /// <returns>Transform of the closest respawn point</returns>
    private Transform FindClosestRespawnPoint(Vector3 fromPosition, GameObject[] respawnPoints)
    {
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject point in respawnPoints)
        {
            float distance = Vector3.Distance(fromPosition, point.transform.position);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// Teleports the player to a specific transform position and rotation
    /// Also resets player physics if a Rigidbody is present
    /// </summary>
    /// <param name="destination">The transform to teleport the player to</param>
    private void TeleportPlayerTo(Transform destination)
    {
        // Set position and rotation
        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;

        // Reset physics if player has a Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset CharacterController velocity if present
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            // CharacterController needs to be disabled and re-enabled to properly reset position
            cc.enabled = false;
            player.transform.position = destination.position;
            cc.enabled = true;
        }
    }
}
