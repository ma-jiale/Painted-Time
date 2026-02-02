using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Manages all scene transitions and player spawn positions.
/// This is a singleton that persists across scenes.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // Singleton instance
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    [Tooltip("Name of the Home scene")]
    public string homeSceneName = "Final_HomeScene";
    
    [Tooltip("Name of the Island scene")]
    public string islandSceneName = "Final_IslandScene";
    
    [Tooltip("Name of the Chamber scene (contains both chambers)")]
    public string chamberSceneName = "Final_ChamberScene";

    [Header("Fade Settings")]
    [Tooltip("Duration of the fade to black")]
    public float fadeDuration = 1.0f;
    
    [Tooltip("Color of the fade screen")]
    public Color fadeColor = Color.black;

    // Runtime Fade UI
    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning = false;

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

    /// <summary>
    /// loads a scene by name with fade transition
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }
    }

    /// <summary>
    /// Loads a scene and teleports player to a specific spawn point with fade transition
    /// </summary>
    public void LoadSceneWithSpawnPoint(string sceneName, string spawnPointName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneWithSpawnCoroutine(sceneName, spawnPointName));
        }
    }

    /// <summary>
    /// Performs a fade out to black (coroutine)
    /// </summary>
    public IEnumerator FadeOut(float duration = -1f)
    {
        float dur = duration > 0 ? duration : fadeDuration;
        EnsureFadeCanvas();
        
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / dur);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Performs a fade in from black (coroutine)
    /// </summary>
    public IEnumerator FadeIn(float duration = -1f)
    {
        float dur = duration > 0 ? duration : fadeDuration;
        EnsureFadeCanvas();

        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / dur);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// Helper to create/find the fade canvas at runtime
    /// </summary>
    private void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        // Check if player camera exists to attach to
        Transform playerCamera = Camera.main?.transform;
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            Camera cam = GameManager.Instance.player.GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("SceneTransitionManager: No player camera found for fade canvas.");
            return;
        }

        // Create canvas object
        GameObject canvasObj = new GameObject("TransitionFadeCanvas");
        canvasObj.transform.SetParent(playerCamera, false);
        canvasObj.transform.localPosition = new Vector3(0, 0, 0.6f); // Behind story canvas (0.5m)
        canvasObj.transform.localRotation = Quaternion.identity;

        // Add components
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Setup scaling - larger size to cover view at greater distance
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        rect.sizeDelta = new Vector2(4000, 4000); // Larger to cover view at 0.6m distance

        // Add CanvasGroup
        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f; // Start invisible
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;

        // Add Image
        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image image = imageObj.AddComponent<Image>();
        image.color = fadeColor;
        
        RectTransform imgRect = imageObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.sizeDelta = Vector2.zero;
        imgRect.anchoredPosition = Vector2.zero;
        
        canvasObj.SetActive(false);
    }

    /// <summary>
    /// Coroutine that handles scene loading with fade
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isTransitioning = true;

        // 1. Fade Out
        yield return StartCoroutine(FadeOut());

        // 2. Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;

        // Wait a bit to ensure stability
        yield return null;

        // 3. Fade In
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    /// <summary>
    /// Coroutine that handles scene loading, teleport, and fade
    /// </summary>
    private IEnumerator LoadSceneWithSpawnCoroutine(string sceneName, string spawnPointName)
    {
        isTransitioning = true;

        // 1. Fade Out
        yield return StartCoroutine(FadeOut());

        // 1.5. Play story during black screen if transitioning to island
        if (sceneName == islandSceneName && StoryManager.Instance != null)
        {
            yield return StartCoroutine(StoryManager.Instance.PlayStoryAndWait("EnterPainting"));
        }

        // 2. Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;

        // Wait one frame
        yield return null;

        // 3. Find Spawn Point & Teleport
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        
        if (spawnPoint != null)
        {
            GameObject player = GetPlayerReference();

            if (player != null)
            {
                TeleportPlayer(player, spawnPoint.transform);
                Debug.Log($"Player teleported to spawn point: {spawnPointName}");
            }
            else
            {
                Debug.LogWarning("Player not found! Cannot teleport.");
            }
        }
        else
        {
            Debug.LogWarning($"Spawn point '{spawnPointName}' not found in scene '{sceneName}'");
        }

        // 4. Fade In
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    private GameObject GetPlayerReference()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
            return GameManager.Instance.player;
            
        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag("Player"); } catch {}
        
        if (player == null) player = GameObject.Find("XR Origin") ?? GameObject.Find("XR Origin (XR Rig)");
        
        return player;
    }

    private void TeleportPlayer(GameObject player, Transform target)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        player.transform.position = target.position;
        player.transform.rotation = target.rotation;
        
        if (cc != null) cc.enabled = true;
    }
}

