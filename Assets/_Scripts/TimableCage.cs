using UnityEngine;

/// <summary>
/// A cage with bars that lower when the timeline is within specific knot regions.
/// Each knot region represents a time range where the cage responds.
/// When the timeline enters a knot, the bars lower by a specified distance.
/// 
/// Purpose: Final level puzzle - player must manipulate time to specific knot regions
/// to progressively lower cage bars and free the target.
/// Attach to the cage GameObject with bars as child objects.
/// </summary>
public class TimableCage : TimableObject
{
    [System.Serializable]
    public class KnotRegion
    {
        [Tooltip("Minimum time value for this knot (-1 to 1)")]
        [Range(-1f, 1f)]
        public float minTimeValue = -0.8f;
        
        [Tooltip("Maximum time value for this knot (-1 to 1)")]
        [Range(-1f, 1f)]
        public float maxTimeValue = -0.4f;
        
        [Tooltip("Has this knot been activated (timeline entered this region)?")]
        [HideInInspector]
        public bool isActivated = false;
        
        /// <summary>
        /// Check if a time value is within this knot region
        /// </summary>
        public bool ContainsTimeValue(float timeValue)
        {
            return timeValue >= minTimeValue && timeValue <= maxTimeValue;
        }
    }
    
    [Header("Cage Settings")]
    [SerializeField, Tooltip("The bars transform that will move down")]
    private Transform barsTransform;
    
    [SerializeField, Tooltip("Distance each activated knot lowers the bars")]
    private float lowerDistancePerKnot = 0.5f;
    
    [SerializeField, Tooltip("Speed at which bars lower")]
    private float lowerSpeed = 2f;
    
    [SerializeField, Tooltip("Local axis to move bars (typically down)")]
    private Vector3 lowerDirection = Vector3.down;
    
    [Header("Knot Regions")]
    [SerializeField, Tooltip("List of knot regions that must be activated in sequence")]
    private KnotRegion[] knotRegions;
    
    [Header("Hold Settings")]
    [SerializeField, Tooltip("Time in seconds player must hold timeline in knot region to activate")]
    private float knotHoldDuration = 3.0f;
    
    [Header("Transition Settings")]
    [SerializeField, Tooltip("Delay before transitioning to next knot UI (seconds)")]
    private float knotTransitionDelay = 1.0f;
    
    [Header("Audio Feedback")]
    [SerializeField, Tooltip("Sound when a knot is activated")]
    private AudioClip knotActivatedSound;
    
    [SerializeField, Tooltip("Sound when all knots completed")]
    private AudioClip allKnotsCompletedSound;
    
    private AudioSource audioSource;
    
    // Current target position for bars
    private Vector3 barsInitialLocalPosition;
    private Vector3 barsTargetLocalPosition;
    private int activatedKnotCount = 0;
    private bool isLowering = false;
    
    // Sequential knot tracking
    private int currentActiveKnotIndex = 0; // The knot player is currently trying to solve
    private bool isInKnot = false;
    private bool isCageCompleted = false; // When true, cage cannot be targeted anymore
    
    // Hold timer for knot activation
    private float currentHoldTime = 0f;
    
    // Transition state
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    
    /// <summary>
    /// Event fired when a knot is activated (knotIndex, knot)
    /// </summary>
    public event System.Action<int, KnotRegion> OnKnotActivated;
    
    /// <summary>
    /// Event fired when entering/exiting a knot region
    /// </summary>
    public event System.Action<bool, int> OnKnotRegionChanged;
    
    /// <summary>
    /// Event fired when transitioning to next knot (nextKnotIndex)
    /// UI should update to show only the next knot
    /// </summary>
    public event System.Action<int> OnTransitionToNextKnot;
    
    /// <summary>
    /// Event fired when hold progress changes (progress 0-1)
    /// UI can use this to show a progress bar
    /// </summary>
    public event System.Action<float> OnHoldProgressChanged;
    
    /// <summary>
    /// Event fired when all knots are completed and cage is fully opened
    /// </summary>
    public event System.Action OnCageCompleted;
    
    /// <summary>
    /// Returns the knot regions for UI display
    /// </summary>
    public KnotRegion[] KnotRegions => knotRegions;
    
    /// <summary>
    /// Returns whether currently in a knot region
    /// </summary>
    public bool IsInKnot => isInKnot;
    
    /// <summary>
    /// Returns the index of the current active knot (the one player is solving)
    /// </summary>
    public int CurrentActiveKnotIndex => currentActiveKnotIndex;
    
    /// <summary>
    /// Returns whether the cage is completed (all knots solved, cannot be targeted)
    /// </summary>
    public bool IsCageCompleted => isCageCompleted;
    
    /// <summary>
    /// Returns the number of activated knots
    /// </summary>
    public int ActivatedKnotCount => activatedKnotCount;
    
    /// <summary>
    /// Returns the current hold progress (0 to 1)
    /// </summary>
    public float HoldProgress => knotHoldDuration > 0 ? Mathf.Clamp01(currentHoldTime / knotHoldDuration) : 0f;
    
    /// <summary>
    /// Returns the required hold duration in seconds
    /// </summary>
    public float KnotHoldDuration => knotHoldDuration;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Cache initial bar position
        if (barsTransform != null)
        {
            barsInitialLocalPosition = barsTransform.localPosition;
            barsTargetLocalPosition = barsInitialLocalPosition;
        }
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Initialize knot regions
        if (knotRegions == null || knotRegions.Length == 0)
        {
            // Create a default knot region for testing
            knotRegions = new KnotRegion[]
            {
                new KnotRegion { minTimeValue = -0.8f, maxTimeValue = -0.4f }
            };
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Smoothly move bars to target position
        if (barsTransform != null && isLowering)
        {
            barsTransform.localPosition = Vector3.MoveTowards(
                barsTransform.localPosition,
                barsTargetLocalPosition,
                lowerSpeed * Time.deltaTime
            );
            
            // Check if reached target
            if (Vector3.Distance(barsTransform.localPosition, barsTargetLocalPosition) < 0.01f)
            {
                barsTransform.localPosition = barsTargetLocalPosition;
                isLowering = false;
            }
        }
        
        // Handle knot transition timer
        if (isTransitioning)
        {
            transitionTimer -= Time.deltaTime;
            if (transitionTimer <= 0f)
            {
                isTransitioning = false;
                CompleteKnotTransition();
            }
        }
        
        // Handle hold timer for knot activation (runs every frame, not just on time value change)
        UpdateHoldTimer();
    }
    
    /// <summary>
    /// Update hold timer - called every frame to accumulate time while in knot region
    /// </summary>
    private void UpdateHoldTimer()
    {
        // Skip if cage is completed or transitioning between knots
        if (isCageCompleted || isTransitioning) return;
        
        // Skip if not in a knot region
        if (!isInKnot) return;
        
        // Only check the current active knot
        if (currentActiveKnotIndex >= knotRegions.Length) return;
        
        KnotRegion activeKnot = knotRegions[currentActiveKnotIndex];
        
        // Accumulate hold time while in knot region
        if (!activeKnot.isActivated)
        {
            currentHoldTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentHoldTime / knotHoldDuration);
            OnHoldProgressChanged?.Invoke(progress);
            
            // Activate knot when hold duration is met
            if (currentHoldTime >= knotHoldDuration)
            {
                ActivateKnot(currentActiveKnotIndex);
            }
        }
    }
    
    /// <summary>
    /// Called when the time value changes - check only the current active knot
    /// </summary>
    protected override void ApplyTimeValue(float timeValue)
    {
        // Skip if cage is completed or transitioning between knots
        if (isCageCompleted || isTransitioning) return;
        
        // Only check the current active knot (sequential progression)
        if (currentActiveKnotIndex >= knotRegions.Length) return;
        
        KnotRegion activeKnot = knotRegions[currentActiveKnotIndex];
        bool wasInKnot = isInKnot;
        isInKnot = activeKnot.ContainsTimeValue(timeValue);
        
        // Fire event when entering/exiting the active knot region
        if (isInKnot != wasInKnot)
        {
            OnKnotRegionChanged?.Invoke(isInKnot, currentActiveKnotIndex);
            
            // Play StartPuzzle story when first entering the first knot
            if (isInKnot && currentActiveKnotIndex == 0 && StoryManager.Instance != null)
            {
                if (!StoryManager.Instance.HasPlayed("StartPuzzle"))
                {
                    StoryManager.Instance.PlayStory("StartPuzzle");
                }
            }
            
            // Reset hold timer when leaving the knot region
            if (!isInKnot)
            {
                currentHoldTime = 0f;
                OnHoldProgressChanged?.Invoke(0f);
            }
        }
        
        // Note: Hold timer accumulation is now handled in UpdateHoldTimer() called from Update()
    }
    
    /// <summary>
    /// Activate a knot region and lower the bars
    /// </summary>
    private void ActivateKnot(int knotIndex)
    {
        if (knotIndex < 0 || knotIndex >= knotRegions.Length) return;
        
        KnotRegion knot = knotRegions[knotIndex];
        if (knot.isActivated) return;
        
        // Mark as activated
        knot.isActivated = true;
        activatedKnotCount++;
        
        // Calculate new target position
        barsTargetLocalPosition = barsInitialLocalPosition + 
            lowerDirection.normalized * lowerDistancePerKnot * activatedKnotCount;
        
        isLowering = true;
        
        // Play sound
        if (audioSource != null && knotActivatedSound != null)
        {
            audioSource.PlayOneShot(knotActivatedSound);
        }
        
        // Fire event
        OnKnotActivated?.Invoke(knotIndex, knot);
        
        Debug.Log($"TimableCage: Knot {knotIndex} activated! Bars lowering to position {activatedKnotCount}");
        
        // Start transition to next knot or complete cage
        StartKnotTransition();
    }
    
    /// <summary>
    /// Start transition to the next knot
    /// </summary>
    private void StartKnotTransition()
    {
        isTransitioning = true;
        transitionTimer = knotTransitionDelay;
    }
    
    /// <summary>
    /// Complete the transition to next knot or finish the puzzle
    /// </summary>
    private void CompleteKnotTransition()
    {
        // Move to next knot
        currentActiveKnotIndex++;
        isInKnot = false;
        
        // Check if all knots are completed
        if (currentActiveKnotIndex >= knotRegions.Length)
        {
            CompleteCage();
        }
        else
        {
            // Reset timeline to center for next knot
            if (timeAnchor != null)
            {
                timeAnchor.SetTimeValue(0f);
            }
            
            // Reset hold timer for next knot
            currentHoldTime = 0f;
            OnHoldProgressChanged?.Invoke(0f);
            
            // Notify UI to update for next knot
            OnTransitionToNextKnot?.Invoke(currentActiveKnotIndex);
            
            Debug.Log($"TimableCage: Transitioning to knot {currentActiveKnotIndex}");
        }
    }
    
    /// <summary>
    /// Called when all knots are completed
    /// </summary>
    private void CompleteCage()
    {
        isCageCompleted = true;
        
        // Disable the Timable tag so cage cannot be selected anymore
        gameObject.tag = "Untagged";
        
        // Play completion sound
        if (audioSource != null && allKnotsCompletedSound != null)
        {
            audioSource.PlayOneShot(allKnotsCompletedSound);
        }
        
        // Fire completion event
        OnCageCompleted?.Invoke();
        
        Debug.Log("TimableCage: All knots completed! Cage is now fully open.");
    }
    
    /// <summary>
    /// Reset all knots (for testing or level restart)
    /// </summary>
    public void ResetKnots()
    {
        foreach (var knot in knotRegions)
        {
            knot.isActivated = false;
        }
        
        activatedKnotCount = 0;
        currentActiveKnotIndex = 0;
        barsTargetLocalPosition = barsInitialLocalPosition;
        
        if (barsTransform != null)
        {
            barsTransform.localPosition = barsInitialLocalPosition;
        }
        
        isInKnot = false;
        isCageCompleted = false;
        isTransitioning = false;
        currentHoldTime = 0f;
        
        // Restore Timable tag
        gameObject.tag = timableTag;
    }
    
    /// <summary>
    /// Check if all knots have been activated (cage fully opened)
    /// </summary>
    public bool AreAllKnotsActivated()
    {
        foreach (var knot in knotRegions)
        {
            if (!knot.isActivated) return false;
        }
        return true;
    }
    
    #region Editor Helpers
    private void OnDrawGizmosSelected()
    {
        if (barsTransform == null) return;
        
        // Draw current position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(barsTransform.position, Vector3.one * 0.3f);
        
        // Draw lowered positions for each knot
        if (knotRegions != null)
        {
            for (int i = 0; i < knotRegions.Length; i++)
            {
                Vector3 pos = transform.TransformPoint(
                    barsInitialLocalPosition + lowerDirection.normalized * lowerDistancePerKnot * (i + 1)
                );
                
                Gizmos.color = knotRegions[i].isActivated ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.2f);
            }
        }
    }
    #endregion
}
