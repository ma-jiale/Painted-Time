using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller that displays knot regions on the timeline.
/// Shows visual indicators for knot positions and highlights when timeline is in a knot.
/// Designed to work with TimableCage for the final level puzzle.
/// 
/// Purpose: Provides visual feedback showing where knot regions are on the timeline,
/// allowing players to see target areas they need to move the timeline to.
/// Attach to the Timeline Knot UI GameObject (separate from main TimelineUI).
/// </summary>
public class TimableKnotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("UI Image/RawImage with material containing Progress parameter")]
    private Graphic timelineGraphic;
    
    [SerializeField, Tooltip("Name of the progress property in shader")]
    private string progressPropertyName = "_Progress";
    
    [SerializeField, Tooltip("Text showing current time value")]
    private Text timeValueText;
    
    [SerializeField, Tooltip("Text showing instruction or status")]
    private Text statusText;
    
    [SerializeField, Tooltip("Container for knot region indicators")]
    private RectTransform knotContainer;
    
    [Header("Hold Progress UI")]
    [SerializeField, Tooltip("Image for hold progress bar (fill type)")]
    private Image holdProgressBar;
    
    [SerializeField, Tooltip("Text showing hold progress percentage")]
    private Text holdProgressText;
    
    [Header("Knot Visual Settings")]
    [SerializeField, Tooltip("Prefab for knot region indicator")]
    private GameObject knotIndicatorPrefab;
    
    [SerializeField, Tooltip("Color for inactive knot region")]
    private Color inactiveKnotColor = new Color(1f, 0.5f, 0f, 0.5f);
    
    [SerializeField, Tooltip("Color for active knot region (timeline is inside)")]
    private Color activeKnotColor = new Color(0f, 1f, 0.5f, 0.8f);
    
    [SerializeField, Tooltip("Color for completed knot region")]
    private Color completedKnotColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    [Header("Timeline Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color inKnotColor = new Color(0f, 1f, 0.5f);
    
    // Reference to the current cage being manipulated
    private TimableCage currentCage;
    
    // Created knot indicator (only one at a time now)
    private Image currentKnotIndicator;
    
    // Current state
    private float currentTimeValue = 0f;
    private bool isInKnot = false;
    private int displayedKnotIndex = 0;
    private float currentHoldProgress = 0f;
    
    // Cached material instance for timeline graphic
    private Material timelineMaterial;
    
    // Cached property ID for better performance
    private int progressPropertyId;
    
    private void Awake()
    {
        // Cache the material and property ID for timeline graphic
        if (timelineGraphic != null)
        {
            // Use material (not sharedMaterial) to create an instance for UI Graphic
            timelineMaterial = timelineGraphic.material;
            progressPropertyId = Shader.PropertyToID(progressPropertyName);
            
            // Initialize to 0.5 (middle) for time value 0
            if (timelineMaterial != null && timelineMaterial.HasProperty(progressPropertyId))
            {
                timelineMaterial.SetFloat(progressPropertyId, 0.5f);
            }
        }
        
        // Initialize progress bar
        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = 0f;
        }
        if (holdProgressText != null)
        {
            holdProgressText.text = "";
        }
        
        // Create default knot indicator prefab if not assigned
        if (knotIndicatorPrefab == null)
        {
            CreateDefaultKnotIndicator();
        }
    }
    
    /// <summary>
    /// Initialize the UI with a specific cage's knot regions
    /// </summary>
    public void Initialize(TimableCage cage)
    {
        currentCage = cage;
        
        // Clear existing indicator
        ClearKnotIndicator();
        
        if (cage == null || cage.KnotRegions == null) return;
        
        // Subscribe to cage events
        cage.OnKnotRegionChanged += OnKnotRegionChanged;
        cage.OnKnotActivated += OnKnotActivated;
        cage.OnTransitionToNextKnot += OnTransitionToNextKnot;
        cage.OnCageCompleted += OnCageCompleted;
        cage.OnHoldProgressChanged += OnHoldProgressChanged;
        
        // Start with the current active knot (in case of save/load)
        displayedKnotIndex = cage.CurrentActiveKnotIndex;
        
        // Create indicator for the current active knot only
        if (displayedKnotIndex < cage.KnotRegions.Length)
        {
            CreateKnotIndicator(cage.KnotRegions[displayedKnotIndex]);
        }
        
        UpdateStatusText();
    }
    
    /// <summary>
    /// Clean up when switching targets or disabling
    /// </summary>
    public void Cleanup()
    {
        if (currentCage != null)
        {
            currentCage.OnKnotRegionChanged -= OnKnotRegionChanged;
            currentCage.OnKnotActivated -= OnKnotActivated;
            currentCage.OnTransitionToNextKnot -= OnTransitionToNextKnot;
            currentCage.OnCageCompleted -= OnCageCompleted;
            currentCage.OnHoldProgressChanged -= OnHoldProgressChanged;
            currentCage = null;
        }
        
        // Reset progress bar
        currentHoldProgress = 0f;
        UpdateHoldProgressUI();
        
        ClearKnotIndicator();
    }
    
    /// <summary>
    /// Update the displayed time value
    /// </summary>
    public void UpdateTimeValue(float timeValue)
    {
        currentTimeValue = timeValue;
        
        // Update material progress (convert from -1~1 to 0~1)
        if (timelineMaterial != null && timelineMaterial.HasProperty(progressPropertyId))
        {
            // Map timeValue from [-1, 1] to [0, 1]
            float progressValue = (timeValue + 1f) * 0.5f;
            timelineMaterial.SetFloat(progressPropertyId, progressValue);
        }
        
        // Update text
        if (timeValueText != null)
        {
            timeValueText.text = $"Time: {timeValue:F2}";
        }
        
        // Update knot indicator color
        UpdateKnotIndicatorColor();
    }
    
    /// <summary>
    /// Create visual indicator for a single knot region
    /// </summary>
    private void CreateKnotIndicator(TimableCage.KnotRegion knot)
    {
        if (knotContainer == null || knotIndicatorPrefab == null || knot == null) return;
        
        float sliderWidth = knotContainer.rect.width;
        
        // Create indicator
        GameObject indicator = Instantiate(knotIndicatorPrefab, knotContainer);
        indicator.name = $"KnotIndicator_{displayedKnotIndex}";
        indicator.SetActive(true);
        
        RectTransform rect = indicator.GetComponent<RectTransform>();
        Image image = indicator.GetComponent<Image>();
        
        if (rect != null)
        {
            // Calculate position and size based on knot region
            // TimeValue -1 to 1 maps to 0 to sliderWidth
            float minPos = ((knot.minTimeValue + 1f) / 2f) * sliderWidth;
            float maxPos = ((knot.maxTimeValue + 1f) / 2f) * sliderWidth;
            float width = maxPos - minPos;
            float centerX = (minPos + maxPos) / 2f - sliderWidth / 2f;
            
            rect.anchoredPosition = new Vector2(centerX, 0);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }
        
        if (image != null)
        {
            image.color = inactiveKnotColor;
            currentKnotIndicator = image;
        }
    }
    
    /// <summary>
    /// Clear the current knot indicator
    /// </summary>
    private void ClearKnotIndicator()
    {
        if (knotContainer != null)
        {
            foreach (Transform child in knotContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        currentKnotIndicator = null;
    }
    
    /// <summary>
    /// Update knot indicator color based on current state
    /// </summary>
    private void UpdateKnotIndicatorColor()
    {
        if (currentCage == null || currentKnotIndicator == null) return;
        
        if (currentCage.IsInKnot)
        {
            currentKnotIndicator.color = activeKnotColor;
        }
        else
        {
            currentKnotIndicator.color = inactiveKnotColor;
        }
    }
    

    
    /// <summary>
    /// Update status text
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText == null || currentCage == null) return;
        
        int total = currentCage.KnotRegions.Length;
        int completed = currentCage.ActivatedKnotCount;
        
        if (currentCage.AreAllKnotsActivated())
        {
            statusText.text = "All knots activated!";
            statusText.color = Color.green;
        }
        else if (currentCage.IsInKnot)
        {
            // Show hold progress when in knot region
            float holdDuration = currentCage.KnotHoldDuration;
            float progress = currentHoldProgress;
            
            if (progress > 0f && progress < 1f)
            {
                float remainingTime = holdDuration * (1f - progress);
                statusText.text = $"Hold steady! {remainingTime:F1}s ({completed}/{total})";
            }
            else
            {
                statusText.text = $"In knot region! Hold for {holdDuration:F1}s ({completed}/{total})";
            }
            statusText.color = activeKnotColor;
        }
        else
        {
            statusText.text = $"Move to highlighted region ({completed}/{total})";
            statusText.color = Color.white;
        }
    }
    
    /// <summary>
    /// Called when entering/exiting a knot region
    /// </summary>
    private void OnKnotRegionChanged(bool inKnot, int knotIndex)
    {
        isInKnot = inKnot;
        UpdateKnotIndicatorColor();
        UpdateStatusText();
    }
    
    /// <summary>
    /// Called when a knot is activated
    /// </summary>
    private void OnKnotActivated(int knotIndex, TimableCage.KnotRegion knot)
    {
        // Mark as completed temporarily (will transition to next)
        if (currentKnotIndicator != null)
        {
            currentKnotIndicator.color = completedKnotColor;
        }
        UpdateStatusText();
    }
    
    /// <summary>
    /// Called when transitioning to the next knot
    /// </summary>
    private void OnTransitionToNextKnot(int nextKnotIndex)
    {
        if (currentCage == null) return;
        
        // Update displayed knot index
        displayedKnotIndex = nextKnotIndex;
        
        // Clear old indicator and create new one for next knot
        ClearKnotIndicator();
        
        if (nextKnotIndex < currentCage.KnotRegions.Length)
        {
            CreateKnotIndicator(currentCage.KnotRegions[nextKnotIndex]);
        }
        
        UpdateStatusText();
        
        Debug.Log($"TimableKnotUI: Now showing knot {nextKnotIndex}");
    }
    
    /// <summary>
    /// Called when all knots are completed
    /// </summary>
    private void OnCageCompleted()
    {
        // Clear the indicator
        ClearKnotIndicator();
        
        // Reset progress bar
        currentHoldProgress = 0f;
        UpdateHoldProgressUI();
        
        // Show completion message
        if (statusText != null)
        {
            statusText.text = "Cage Unlocked!";
            statusText.color = Color.green;
        }
        
        Debug.Log("TimableKnotUI: Cage completed!");
    }
    
    /// <summary>
    /// Called when hold progress changes
    /// </summary>
    private void OnHoldProgressChanged(float progress)
    {
        currentHoldProgress = progress;
        UpdateHoldProgressUI();
        UpdateStatusText();
    }
    
    /// <summary>
    /// Update the hold progress bar UI
    /// </summary>
    private void UpdateHoldProgressUI()
    {
        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = currentHoldProgress;
            
            // Change color based on progress
            if (currentHoldProgress >= 1f)
            {
                holdProgressBar.color = completedKnotColor;
            }
            else if (currentHoldProgress > 0f)
            {
                holdProgressBar.color = activeKnotColor;
            }
            else
            {
                holdProgressBar.color = inactiveKnotColor;
            }
        }
        
        if (holdProgressText != null)
        {
            if (currentHoldProgress > 0f && currentHoldProgress < 1f)
            {
                holdProgressText.text = $"{Mathf.RoundToInt(currentHoldProgress * 100)}%";
            }
            else
            {
                holdProgressText.text = "";
            }
        }
    }
    
    /// <summary>
    /// Create a default knot indicator prefab at runtime
    /// </summary>
    private void CreateDefaultKnotIndicator()
    {
        knotIndicatorPrefab = new GameObject("KnotIndicator");
        
        RectTransform rect = knotIndicatorPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50f, 20f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image image = knotIndicatorPrefab.AddComponent<Image>();
        image.color = inactiveKnotColor;
        
        knotIndicatorPrefab.SetActive(false);
    }
    
    private void OnDestroy()
    {
        Cleanup();
        
        // Clean up the material instance to prevent memory leaks
        if (timelineMaterial != null)
        {
            Destroy(timelineMaterial);
        }
        
        // Clean up runtime-created prefab
        if (knotIndicatorPrefab != null && knotIndicatorPrefab.scene.name == null)
        {
            Destroy(knotIndicatorPrefab);
        }
    }
}
