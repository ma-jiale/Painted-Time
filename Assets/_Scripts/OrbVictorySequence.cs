using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Handles the victory sequence when the cage puzzle is completed.
/// The orb flies in circles, then flies to the player, fades out,
/// shows credits, and returns to the home scene.
/// 
/// Attach this to the treasure orb GameObject in the scene.
/// 
/// Note: This script automatically creates FadeCanvas and CreditsCanvas at runtime
/// under the player's camera, so you don't need to set them up manually if the
/// XR Origin is in a different scene.
/// </summary>
public class OrbVictorySequence : MonoBehaviour
{
    [Header("Cage Reference")]
    [Tooltip("The cage that triggers the victory sequence when completed")]
    public TimableCage targetCage;

    [Header("Orbit Animation Settings")]
    [Tooltip("Duration of the orbit animation in seconds")]
    public float orbitDuration = 3f;

    [Tooltip("Number of complete loops during orbit")]
    public int orbitLoops = 3;

    [Tooltip("Radius of the orbit circle")]
    public float orbitRadius = 1f;

    [Tooltip("Height above the cage for orbit center")]
    public float orbitHeight = 2f;

    [Header("Fly To Player Settings")]
    [Tooltip("Duration to fly from orbit to player")]
    public float flyToPlayerDuration = 2f;

    [Tooltip("Distance in front of the player's face")]
    public float distanceFromPlayer = 0.5f;

    [Header("Fade Settings")]
    [Tooltip("Duration for the orb to fade out")]
    public float orbFadeOutDuration = 2f;

    [Tooltip("Duration for the screen to fade to black")]
    public float screenFadeDuration = 2f;

    [Header("Credits Settings")]
    [Tooltip("Duration to display the credits")]
    public float creditsDisplayTime = 8f;

    [Tooltip("Duration for credits text to fade in")]
    public float creditsFadeInDuration = 1f;

    [Header("UI References (Optional - Will be created at runtime if not set)")]
    [Tooltip("Canvas for screen fade effect. If not set, will be created automatically under player camera.")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("Canvas for credits display. If not set, will be created automatically under player camera.")]
    public CanvasGroup creditsCanvasGroup;

    [Tooltip("Optional: Text component for credits (if not set, will search in creditsCanvasGroup)")]
    public TMP_Text creditsText;

    [Header("Canvas Settings (Used when creating at runtime)")]
    [Tooltip("Distance from camera for the fade/credits canvas")]
    public float canvasDistance = 0.5f;

    [Tooltip("Size of the canvas in world units")]
    public float canvasSize = 2f;

    [Tooltip("Font size for credits text")]
    public int creditsFontSize = 36;

    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when the victory sequence starts")]
    public AudioClip victorySound;

    [Tooltip("Background music during credits")]
    public AudioClip creditsMusic;

    // Credits text content - displayed sequentially
    private readonly string[] creditsLines = new string[]
    {
        "Thank You for Playing",
        "This game is still in development\nand does not represent the final product",
        "An SJTU Design Production",
        "Created by\nMa Jiale    Zhang Qi",
        "Special Thanks to\nProfessor Zhang Andong"
    };

    [Header("Credits Animation")]
    [Tooltip("Duration for each credit line to fade in")]
    public float lineFadeInDuration = 0.8f;

    [Tooltip("Duration to display each credit line")]
    public float lineDisplayDuration = 2f;

    [Tooltip("Duration for each credit line to fade out")]
    public float lineFadeOutDuration = 0.8f;

    // Internal state
    private bool isSequenceStarted = false;
    private AudioSource audioSource;
    private Renderer orbRenderer;
    private Material orbMaterial;
    private Vector3 initialPosition;
    private Transform playerCamera;
    private bool canvasesCreatedAtRuntime = false;

    private void Awake()
    {
        // Cache components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        orbRenderer = GetComponent<Renderer>();
        if (orbRenderer != null)
        {
            // Create instance of material to avoid modifying shared material
            orbMaterial = orbRenderer.material;
        }

        initialPosition = transform.position;
    }

    private void Start()
    {
        // Subscribe to cage completion event
        if (targetCage != null)
        {
            targetCage.OnCageCompleted += OnCageCompleted;
        }
        else
        {
            Debug.LogWarning("OrbVictorySequence: No target cage assigned!");
        }

        // Find player camera
        FindPlayerCamera();

        // If canvases are already assigned, ensure they are hidden
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.alpha = 0f;
            creditsCanvasGroup.gameObject.SetActive(false);

            // Set credits text content
            if (creditsText == null)
            {
                creditsText = creditsCanvasGroup.GetComponentInChildren<TMP_Text>();
            }
            if (creditsText != null)
            {
                creditsText.text = ""; // Will be set dynamically during sequence
                creditsText.alpha = 0f;

            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from cage completion event
        if (targetCage != null)
        {
            targetCage.OnCageCompleted -= OnCageCompleted;
        }

        // Clean up runtime-created canvases
        if (canvasesCreatedAtRuntime)
        {
            if (fadeCanvasGroup != null)
            {
                Destroy(fadeCanvasGroup.gameObject);
            }
            if (creditsCanvasGroup != null)
            {
                Destroy(creditsCanvasGroup.gameObject);
            }
        }
    }

    /// <summary>
    /// Find the player's camera for positioning
    /// </summary>
    private void FindPlayerCamera()
    {
        // Try to find from GameManager
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            playerCamera = GameManager.Instance.player.GetComponentInChildren<Camera>()?.transform;
        }

        // Fallback: find main camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("OrbVictorySequence: Could not find player camera!");
        }
    }

    /// <summary>
    /// Create fade and credits canvases at runtime under the player camera
    /// Called when the victory sequence starts if canvases are not assigned
    /// </summary>
    private void CreateCanvasesAtRuntime()
    {
        if (playerCamera == null)
        {
            Debug.LogError("OrbVictorySequence: Cannot create canvases - no player camera found!");
            return;
        }

        Debug.Log("OrbVictorySequence: Creating canvases at runtime under player camera.");
        canvasesCreatedAtRuntime = true;

        // Create FadeCanvas if not assigned
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = CreateFadeCanvas();
        }

        // Create CreditsCanvas if not assigned
        if (creditsCanvasGroup == null)
        {
            creditsCanvasGroup = CreateCreditsCanvas();
        }
    }

    /// <summary>
    /// Create the fade canvas with a black image
    /// </summary>
    private CanvasGroup CreateFadeCanvas()
    {
        // Create canvas GameObject
        GameObject canvasObj = new GameObject("FadeCanvas_Runtime");
        canvasObj.transform.SetParent(playerCamera, false);
        canvasObj.transform.localPosition = new Vector3(0, 0, canvasDistance + 0.1f); // Slightly behind credits
        canvasObj.transform.localRotation = Quaternion.identity;

        // Add Canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Set canvas size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(canvasSize * 1000, canvasSize * 1000); // Large enough to cover view
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Add CanvasGroup
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Create black image
        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;

        // Stretch image to fill canvas
        RectTransform imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;

        canvasObj.SetActive(false);

        return canvasGroup;
    }

    /// <summary>
    /// Create the credits canvas with text
    /// </summary>
    private CanvasGroup CreateCreditsCanvas()
    {
        // Create canvas GameObject
        GameObject canvasObj = new GameObject("CreditsCanvas_Runtime");
        canvasObj.transform.SetParent(playerCamera, false);
        canvasObj.transform.localPosition = new Vector3(0, 0, canvasDistance);
        canvasObj.transform.localRotation = Quaternion.identity;

        // Add Canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Set canvas size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 900);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Add CanvasGroup
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Create text object
        GameObject textObj = new GameObject("CreditsText");
        textObj.transform.SetParent(canvasObj.transform, false);

        // Try to add TextMeshPro, fallback to legacy Text if TMP is not available
        try
        {
            creditsText = textObj.AddComponent<TextMeshProUGUI>();
            creditsText.text = ""; // Will be set dynamically
            creditsText.fontSize = creditsFontSize;
            creditsText.alignment = TextAlignmentOptions.Center;
            creditsText.color = Color.white;
            creditsText.alpha = 0f; // Start invisible
        }
        catch
        {
            // Fallback to legacy Text
            Text legacyText = textObj.AddComponent<Text>();
            legacyText.text = "";
            legacyText.fontSize = creditsFontSize;
            legacyText.alignment = TextAnchor.MiddleCenter;
            legacyText.color = new Color(1f, 1f, 1f, 0f); // Start invisible
            legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }


        // Stretch text to fill canvas
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        canvasObj.SetActive(false);

        return canvasGroup;
    }


    /// <summary>
    /// Called when the cage completes all knots
    /// </summary>
    private void OnCageCompleted()
    {
        if (isSequenceStarted) return;
        isSequenceStarted = true;

        Debug.Log("OrbVictorySequence: Cage completed! Starting victory sequence.");
        StartCoroutine(VictorySequenceCoroutine());
    }

    /// <summary>
    /// Main victory sequence coroutine
    /// </summary>
    private IEnumerator VictorySequenceCoroutine()
    {
        // Play victory sound
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // Ensure we have the player camera reference
        if (playerCamera == null)
        {
            FindPlayerCamera();
        }

        // Create canvases at runtime if not assigned (for cross-scene setup)
        if (fadeCanvasGroup == null || creditsCanvasGroup == null)
        {
            CreateCanvasesAtRuntime();
        }

        // Phase 1: Orbit animation
        yield return StartCoroutine(OrbitAnimationCoroutine());

        // Phase 2: Fly to player
        yield return StartCoroutine(FlyToPlayerCoroutine());

        // Phase 3: Fade out orb
        yield return StartCoroutine(FadeOutOrbCoroutine());

        // Phase 4: Fade screen to black
        yield return StartCoroutine(FadeScreenToBlackCoroutine());

        // Phase 4.5: Show orb obtained story before credits
        if (StoryManager.Instance != null)
        {
            yield return StartCoroutine(StoryManager.Instance.PlayStoryAndWait("OrbObtained"));
        }

        // Phase 5: Show credits
        yield return StartCoroutine(ShowCreditsCoroutine());

        // Phase 6: Return to home scene
        ReturnToHomeScene();
    }


    /// <summary>
    /// Phase 1: Orb orbits around a center point above the cage
    /// </summary>
    private IEnumerator OrbitAnimationCoroutine()
    {
        Debug.Log("OrbVictorySequence: Starting orbit animation.");

        // Calculate orbit center (above the cage)
        Vector3 orbitCenter = targetCage != null 
            ? targetCage.transform.position + Vector3.up * orbitHeight 
            : initialPosition + Vector3.up * orbitHeight;

        float elapsed = 0f;
        float totalAngle = orbitLoops * 360f;

        while (elapsed < orbitDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / orbitDuration;

            // Calculate angle for current position
            float angle = Mathf.Lerp(0f, totalAngle, t) * Mathf.Deg2Rad;

            // Calculate position on circle
            float x = Mathf.Cos(angle) * orbitRadius;
            float z = Mathf.Sin(angle) * orbitRadius;

            // Gradually rise during orbit
            float y = Mathf.Lerp(0f, orbitHeight * 0.5f, t);

            transform.position = orbitCenter + new Vector3(x, y, z);

            yield return null;
        }
    }

    /// <summary>
    /// Phase 2: Orb flies towards the player's face
    /// </summary>
    private IEnumerator FlyToPlayerCoroutine()
    {
        Debug.Log("OrbVictorySequence: Flying to player.");

        if (playerCamera == null)
        {
            Debug.LogWarning("OrbVictorySequence: No player camera found, skipping fly to player.");
            yield break;
        }

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < flyToPlayerDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyToPlayerDuration;

            // Use smooth step for more natural movement
            float smoothT = t * t * (3f - 2f * t);

            // Calculate target position in front of player's face
            Vector3 targetPosition = playerCamera.position + playerCamera.forward * distanceFromPlayer;

            // Interpolate position
            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

            // Face the player
            transform.LookAt(playerCamera);

            yield return null;
        }
    }

    /// <summary>
    /// Phase 3: Orb gradually fades out (becomes transparent)
    /// </summary>
    private IEnumerator FadeOutOrbCoroutine()
    {
        Debug.Log("OrbVictorySequence: Fading out orb.");

        if (orbMaterial == null)
        {
            Debug.LogWarning("OrbVictorySequence: No orb material found, skipping fade.");
            yield break;
        }

        // Get initial color
        Color initialColor = orbMaterial.color;
        float elapsed = 0f;

        while (elapsed < orbFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / orbFadeOutDuration;

            // Fade alpha
            Color newColor = initialColor;
            newColor.a = Mathf.Lerp(initialColor.a, 0f, t);
            orbMaterial.color = newColor;

            // Also try to set _Color property for standard shaders
            if (orbMaterial.HasProperty("_Color"))
            {
                orbMaterial.SetColor("_Color", newColor);
            }

            // Keep following player
            if (playerCamera != null)
            {
                Vector3 targetPosition = playerCamera.position + playerCamera.forward * distanceFromPlayer;
                transform.position = targetPosition;
            }

            yield return null;
        }

        // Hide the orb visually but DON'T disable the GameObject
        // (disabling would stop the coroutine!)
        if (orbRenderer != null)
        {
            orbRenderer.enabled = false;
        }
        
        // Also disable any child renderers and lights
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
        foreach (var light in GetComponentsInChildren<Light>())
        {
            light.enabled = false;
        }
        
        Debug.Log("OrbVictorySequence: Orb faded out, continuing to screen fade.");
    }


    /// <summary>
    /// Phase 4: Screen fades to black
    /// </summary>
    private IEnumerator FadeScreenToBlackCoroutine()
    {
        Debug.Log("OrbVictorySequence: Fading screen to black.");

        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("OrbVictorySequence: No fade canvas assigned, skipping screen fade.");
            yield break;
        }

        // Show the fade canvas
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < screenFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / screenFadeDuration;

            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Phase 5: Show credits on black screen - display each line sequentially
    /// </summary>
    private IEnumerator ShowCreditsCoroutine()
    {
        Debug.Log("OrbVictorySequence: Showing credits.");

        // Play credits music if available
        if (audioSource != null && creditsMusic != null)
        {
            audioSource.clip = creditsMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (creditsCanvasGroup == null)
        {
            Debug.LogWarning("OrbVictorySequence: No credits canvas assigned, skipping credits display.");
            yield return new WaitForSeconds(creditsDisplayTime);
            yield break;
        }

        // Show the credits canvas
        creditsCanvasGroup.gameObject.SetActive(true);
        creditsCanvasGroup.alpha = 1f; // Keep canvas visible, we'll fade the text

        // Display each line sequentially
        foreach (string line in creditsLines)
        {
            // Set the text content
            if (creditsText != null)
            {
                creditsText.text = line;
            }

            // Fade in
            float elapsed = 0f;
            while (elapsed < lineFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lineFadeInDuration;
                if (creditsText != null)
                {
                    creditsText.alpha = Mathf.Lerp(0f, 1f, t);
                }
                yield return null;
            }
            if (creditsText != null)
            {
                creditsText.alpha = 1f;
            }

            // Display for duration
            yield return new WaitForSeconds(lineDisplayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < lineFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lineFadeOutDuration;
                if (creditsText != null)
                {
                    creditsText.alpha = Mathf.Lerp(1f, 0f, t);
                }
                yield return null;
            }
            if (creditsText != null)
            {
                creditsText.alpha = 0f;
            }

            // Small pause between lines
            yield return new WaitForSeconds(0.3f);
        }

        // Hide credits canvas
        creditsCanvasGroup.alpha = 0f;
    }


    /// <summary>
    /// Phase 6: Return to home scene and main menu
    /// </summary>
    private void ReturnToHomeScene()
    {
        Debug.Log("OrbVictorySequence: Returning to home scene.");

        // Stop any playing audio
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Use SceneTransitionManager to load home scene
        if (SceneTransitionManager.Instance != null)
        {
            // Set game state to main menu before loading
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainMenu);
            }

            SceneTransitionManager.Instance.LoadSceneWithSpawnPoint(
                SceneTransitionManager.Instance.homeSceneName, 
                "StartGamePoint"
            );

        }
        else
        {
            Debug.LogError("OrbVictorySequence: SceneTransitionManager not found!");
            
            // Fallback: just set the game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainMenu);
            }
        }
    }

    /// <summary>
    /// Public method to manually trigger the victory sequence (for testing)
    /// </summary>
    [ContextMenu("Trigger Victory Sequence")]
    public void TriggerVictorySequence()
    {
        OnCageCompleted();
    }
}
