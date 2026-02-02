using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages story narrative and level guidance text display with fade-in/fade-out effects.
/// Uses Legacy Text component for Chinese character support.
/// 
/// Usage:
/// - StoryManager.Instance.PlayStory("GameStart") to play a story sequence
/// - Attach StoryTrigger components to trigger areas in the scene
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("Animation Settings")]
    [Tooltip("Duration for each line to fade in")]
    public float lineFadeInDuration = 0.8f;

    [Tooltip("Duration to display each line")]
    public float lineDisplayDuration = 2f;

    [Tooltip("Duration for each line to fade out")]
    public float lineFadeOutDuration = 0.8f;

    [Tooltip("Pause between lines")]
    public float linePauseDuration = 0.3f;

    [Header("Canvas Settings")]
    [Tooltip("Distance from camera for the text canvas")]
    public float canvasDistance = 0.5f;

    [Tooltip("Font size for story text")]
    public int fontSize = 36;

    [Tooltip("Optional: Custom font for Chinese text")]
    public Font customFont;

    // Story data - parsed from text.txt
    private readonly Dictionary<string, string[]> storyData = new Dictionary<string, string[]>
    {
        // Story 1: 开始游戏后
        ["GameStart"] = new string[]
        {
            "你是一名壁画修复师。",
            "在你面前的山脚，\n坐落着一座历经数百年的石窟。",
            "石窟之中，\n留存着一段被时间侵蚀的故事。",
            "色彩尚在，\n意义却已残缺。",
            "而你来到这里，\n是为了让它重新完整。"
        },

        // Story 2: 被传送到岛上
        ["EnterPainting"] = new string[]
        {
            "你踏入了壁画之中。",
            "在这里，\n你不再只是旁观者。",
            "你化身为——\n善事太子。",
            "昔日，他为拯救众生，\n踏上海上仙岛，\n寻求摩尼宝珠。",
            "如今，\n这段旅程再次展开。",
            "若要修复壁画，\n你必须完成他的选择。"
        },

        // Story 3: 拿到宝珠后（结束语之前）
        ["OrbObtained"] = new string[]
        {
            "善事太子取得了宝珠。",
            "他以此许愿，\n拯救众生，\n令苦难止息。",
            "壁画的故事，\n终于回到了应有的时间线上。",
            "而你，\n只是让它再次被看见。"
        },

        // Guidance 1: 靠近树
        ["NearTree"] = new string[]
        {
            "时间并不会创造事物，",
            "它只会决定——\n何时可以被使用。"
        },

        // Guidance 2: 进入密室房间A
        ["EnterRoomA"] = new string[]
        {
            "事物并未消失，\n只是偏离了它们的时间。",
            "让一切回到它们原本的位置。"
        },

        // Guidance 3: 靠近BOSS
        ["NearBoss"] = new string[]
        {
            "年轻人，",
            "我看得出，\n你渴望摩尼宝珠。",
            "然而，\n宝珠不回应欲望，\n只回应理解。",
            "解开缠绕其上的\n三个时间之结，",
            "让我看看——\n你是否配得上它。"
        },

        // Guidance 4: 开始解结
        ["StartPuzzle"] = new string[]
        {
            "守住时间，\n心中的结自然会回应你。"
        }
    };

    // Runtime references
    private Transform playerCamera;
    private CanvasGroup storyCanvasGroup;
    private Text storyText;
    private bool isPlaying = false;
    private Coroutine currentStoryCoroutine;

    // Track which stories have been played (for one-time triggers)
    private HashSet<string> playedStories = new HashSet<string>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        FindPlayerCamera();
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
            Debug.LogWarning("StoryManager: Could not find player camera!");
        }
    }

    /// <summary>
    /// Play a story sequence by ID
    /// </summary>
    /// <param name="storyId">The story identifier (e.g., "GameStart", "NearTree")</param>
    /// <param name="onComplete">Optional callback when story finishes</param>
    public void PlayStory(string storyId, System.Action onComplete = null)
    {
        if (!storyData.ContainsKey(storyId))
        {
            Debug.LogWarning($"StoryManager: Story '{storyId}' not found!");
            onComplete?.Invoke();
            return;
        }

        if (isPlaying)
        {
            Debug.Log($"StoryManager: Already playing a story, queuing '{storyId}'.");
            // Optionally queue stories or skip
            return;
        }

        currentStoryCoroutine = StartCoroutine(PlayStoryCoroutine(storyId, onComplete));
    }

    /// <summary>
    /// Play a story sequence and wait for it to complete (for use in other coroutines)
    /// </summary>
    public IEnumerator PlayStoryAndWait(string storyId)
    {
        bool completed = false;
        PlayStory(storyId, () => completed = true);
        
        while (!completed)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Check if a story has already been played
    /// </summary>
    public bool HasPlayed(string storyId)
    {
        return playedStories.Contains(storyId);
    }

    /// <summary>
    /// Mark a story as played (for external tracking)
    /// </summary>
    public void MarkAsPlayed(string storyId)
    {
        playedStories.Add(storyId);
    }

    /// <summary>
    /// Reset played stories (e.g., when starting a new game)
    /// </summary>
    public void ResetPlayedStories()
    {
        playedStories.Clear();
    }

    /// <summary>
    /// Check if currently playing a story
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// Stop the current story immediately
    /// </summary>
    public void StopCurrentStory()
    {
        if (currentStoryCoroutine != null)
        {
            StopCoroutine(currentStoryCoroutine);
            currentStoryCoroutine = null;
        }
        isPlaying = false;
        
        if (storyCanvasGroup != null)
        {
            storyCanvasGroup.alpha = 0f;
            storyCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Main coroutine for playing story sequences
    /// </summary>
    private IEnumerator PlayStoryCoroutine(string storyId, System.Action onComplete)
    {
        isPlaying = true;
        playedStories.Add(storyId);

        Debug.Log($"StoryManager: Playing story '{storyId}'");

        // Ensure we have camera reference
        if (playerCamera == null)
        {
            FindPlayerCamera();
        }

        // Create canvas if needed
        EnsureCanvasExists();

        if (storyCanvasGroup == null || storyText == null)
        {
            Debug.LogError("StoryManager: Failed to create story canvas!");
            isPlaying = false;
            onComplete?.Invoke();
            yield break;
        }

        // Show canvas
        storyCanvasGroup.gameObject.SetActive(true);
        storyCanvasGroup.alpha = 1f;

        string[] lines = storyData[storyId];

        // Display each line sequentially
        foreach (string line in lines)
        {
            // Set text content
            storyText.text = line;

            // Fade in
            float elapsed = 0f;
            while (elapsed < lineFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lineFadeInDuration;
                Color color = storyText.color;
                color.a = Mathf.Lerp(0f, 1f, t);
                storyText.color = color;
                yield return null;
            }
            storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, 1f);

            // Display for duration
            yield return new WaitForSeconds(lineDisplayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < lineFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lineFadeOutDuration;
                Color color = storyText.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                storyText.color = color;
                yield return null;
            }
            storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, 0f);

            // Pause between lines
            yield return new WaitForSeconds(linePauseDuration);
        }

        // Hide canvas
        storyCanvasGroup.alpha = 0f;
        storyCanvasGroup.gameObject.SetActive(false);

        isPlaying = false;
        currentStoryCoroutine = null;

        Debug.Log($"StoryManager: Finished story '{storyId}'");
        onComplete?.Invoke();
    }

    /// <summary>
    /// Ensure the story canvas exists
    /// </summary>
    private void EnsureCanvasExists()
    {
        if (storyCanvasGroup != null && storyText != null)
        {
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("StoryManager: Cannot create canvas - no player camera found!");
            return;
        }

        Debug.Log("StoryManager: Creating story canvas at runtime.");

        // Create canvas GameObject
        GameObject canvasObj = new GameObject("StoryCanvas_Runtime");
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
        storyCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        storyCanvasGroup.alpha = 0f;

        // Create text object using Legacy Text for Chinese support
        GameObject textObj = new GameObject("StoryText");
        textObj.transform.SetParent(canvasObj.transform, false);

        storyText = textObj.AddComponent<Text>();
        storyText.text = "";
        storyText.fontSize = fontSize;
        storyText.alignment = TextAnchor.MiddleCenter;
        storyText.color = new Color(1f, 1f, 1f, 0f); // Start invisible
        storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyText.verticalOverflow = VerticalWrapMode.Overflow;

        // Set font
        if (customFont != null)
        {
            storyText.font = customFont;
        }
        else
        {
            // Try to use Arial or system default
            storyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (storyText.font == null)
            {
                storyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        // Stretch text to fill canvas
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        canvasObj.SetActive(false);
    }

    /// <summary>
    /// Editor test method
    /// </summary>
    [ContextMenu("Test GameStart Story")]
    public void TestGameStartStory()
    {
        PlayStory("GameStart");
    }

    [ContextMenu("Test NearTree Story")]
    public void TestNearTreeStory()
    {
        PlayStory("NearTree");
    }

    [ContextMenu("Test NearBoss Story")]
    public void TestNearBossStory()
    {
        PlayStory("NearBoss");
    }
}
