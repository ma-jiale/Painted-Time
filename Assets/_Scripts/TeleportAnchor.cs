using UnityEngine;
using System.Collections;

/// <summary>
/// Teleport anchor that transports the player to a different location or scene.

/// Place this on GameObjects where you want teleportation to occur.
/// The player will be teleported when entering the trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TeleportAnchor : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Type of teleportation to perform")]
    public TeleportType teleportType = TeleportType.SameScene;

    public enum TeleportType
    {
        SameScene,      // Teleport within the same scene
        DifferentScene  // Load a different scene and teleport
    }

    [Header("Same Scene Teleport")]
    [Tooltip("The target transform to teleport the player to (for same scene teleport)")]
    public Transform targetTransform;

    [Header("Different Scene Teleport")]
    [Tooltip("Name of the scene to load (for different scene teleport)")]
    public string targetSceneName;
    
    [Tooltip("Name of the spawn point GameObject in the target scene")]
    public string spawnPointName;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Particle effect to play when player enters teleport zone")]
    public ParticleSystem teleportEffect;
    
    [Tooltip("Sound to play when teleporting")]
    public AudioClip teleportSound;

    private AudioSource audioSource;
    private bool hasBeenUsed = false;  // Prevents multiple triggers

    private void Start()
    {
        // Ensure the collider is set as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"TeleportAnchor on {gameObject.name} requires a Collider component!");
        }

        // Set up audio source if a sound is assigned
        if (teleportSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = teleportSound;
            audioSource.playOnAwake = false;
        }

        // Validate settings
        ValidateSettings();
    }

    /// <summary>
    /// Validates that all required settings are properly configured
    /// </summary>
    private void ValidateSettings()
    {
        if (teleportType == TeleportType.SameScene && targetTransform == null)
        {
            Debug.LogWarning($"TeleportAnchor on {gameObject.name}: Target Transform is not set for same scene teleport!");
        }

        if (teleportType == TeleportType.DifferentScene)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"TeleportAnchor on {gameObject.name}: Target Scene Name is not set!");
            }
            if (string.IsNullOrEmpty(spawnPointName))
            {
                Debug.LogWarning($"TeleportAnchor on {gameObject.name}: Spawn Point Name is not set!");
            }
        }
    }



    /// <summary>
    /// Called when another collider enters the trigger zone
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player
        if (other.CompareTag("Player") && !hasBeenUsed)
        {
            // Check if player is allowed to move (game must be in playing state)
            if (GameManager.Instance != null && !GameManager.Instance.canPlayerMove)
            {
                return;  // Don't teleport if player can't move
            }

            hasBeenUsed = true;  // Prevent multiple triggers
            Debug.Log($"Player entered teleport anchor: {gameObject.name}");
            
            // Start the teleport sequence with fade
            StartCoroutine(TeleportSequence(other.gameObject));
        }
    }

    /// <summary>
    /// Coroutine that handles the teleport sequence with visual fade
    /// </summary>
    private IEnumerator TeleportSequence(GameObject player)
    {
        // Play sound immediately
        PlayEffects();

        if (teleportType == TeleportType.SameScene)
        {
            // 1. Fade Out
            if (SceneTransitionManager.Instance != null)
            {
                yield return StartCoroutine(SceneTransitionManager.Instance.FadeOut());
            }

            // 2. Teleport
            TeleportSameScene(player);

            // Wait a moment for stability
            yield return new WaitForSeconds(0.1f);

            // 3. Fade In
            if (SceneTransitionManager.Instance != null)
            {
                yield return StartCoroutine(SceneTransitionManager.Instance.FadeIn());
            }
        }
        else if (teleportType == TeleportType.DifferentScene)
        {
            // For different scene, SceneTransitionManager handles the fade sequence internally
            TeleportDifferentScene();
            
            // Note: We don't wait here because scene loading starts immediately
        }
    }


    /// <summary>
    /// Teleports the player to a location within the same scene
    /// </summary>
    private void TeleportSameScene(GameObject player)
    {
        if (targetTransform == null)
        {
            Debug.LogError($"Cannot teleport: Target Transform is not set on {gameObject.name}");
            hasBeenUsed = false;
            return;
        }

        // Get CharacterController if it exists
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
        {
            // Disable CharacterController temporarily to allow position change
            cc.enabled = false;
            player.transform.position = targetTransform.position;
            player.transform.rotation = targetTransform.rotation;
            cc.enabled = true;
        }
        else
        {
            // Direct teleport if no CharacterController
            player.transform.position = targetTransform.position;
            player.transform.rotation = targetTransform.rotation;
        }

        Debug.Log($"Player teleported to {targetTransform.name}");
        
        // Reset the used flag after a short delay
        Invoke(nameof(ResetUsedFlag), 2f);
    }

    /// <summary>
    /// Loads a different scene and teleports the player to the spawn point
    /// </summary>
    private void TeleportDifferentScene()
    {
        if (string.IsNullOrEmpty(targetSceneName) || string.IsNullOrEmpty(spawnPointName))
        {
            Debug.LogError($"Cannot teleport: Scene or spawn point name is not set on {gameObject.name}");
            hasBeenUsed = false;
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"Loading scene: {targetSceneName}, spawn point: {spawnPointName}");
            SceneTransitionManager.Instance.LoadSceneWithSpawnPoint(targetSceneName, spawnPointName);
        }
        else
        {
            Debug.LogError("SceneTransitionManager instance not found!");
            hasBeenUsed = false;
        }
    }

    /// <summary>
    /// Plays visual and audio effects for the teleportation
    /// </summary>
    private void PlayEffects()
    {
        // Play particle effect
        if (teleportEffect != null)
        {
            teleportEffect.Play();
        }

        // Play sound effect
        if (audioSource != null && teleportSound != null)
        {
            audioSource.Play();
        }
    }

    /// <summary>
    /// Resets the used flag to allow the teleporter to be used again
    /// </summary>
    private void ResetUsedFlag()
    {
        hasBeenUsed = false;
    }

    /// <summary>
    /// Visualizes the teleport anchor in the Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw a wire sphere to show the teleport zone
        Gizmos.color = Color.cyan;
        Collider col = GetComponent<Collider>();
        
        if (col != null)
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        // Draw a line to the target if teleporting in same scene
        if (teleportType == TeleportType.SameScene && targetTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetTransform.position);
        }
    }
}
