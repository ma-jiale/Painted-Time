using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss controller that interferes with the timeline system.
/// The Boss doesn't attack players directly, but disrupts their ability to
/// maintain the timeline in knot regions to prevent stable unlocking.
/// 
/// Interference behaviors:
/// 1. Periodic Reverse Pull - Gradually pulls TimeValue away from the current region
/// 2. Noise Jitter - Creates random shaking/noise that destabilizes the timeline
/// 3. Pulse Push - Periodic strong pushes that knock the timeline off target
/// 
/// Purpose: Final level challenge - makes the cage puzzle more difficult.
/// Attach to a Boss GameObject in the scene.
/// </summary>
public class BossController : MonoBehaviour
{
    public static BossController Instance { get; private set; }
    
    #region Interference Types
    [System.Serializable]
    public class ReversePullSettings
    {
        [Tooltip("Enable periodic reverse pull behavior")]
        public bool enabled = true;
        
        [Tooltip("Strength of the pull force")]
        [Range(0.1f, 2f)]
        public float pullStrength = 0.5f;
        
        [Tooltip("Direction to pull (-1 = past, 1 = future, 0 = away from current)")]
        [Range(-1f, 1f)]
        public float pullDirection = 0f;
        
        [Tooltip("If true, always pulls away from the knot region center")]
        public bool pullAwayFromKnot = true;
        
        [Tooltip("Duration of each pull cycle (seconds)")]
        public float pullDuration = 2f;
        
        [Tooltip("Cooldown between pulls (seconds)")]
        public float pullCooldown = 3f;
    }
    
    [System.Serializable]
    public class NoiseJitterSettings
    {
        [Tooltip("Enable noise jitter behavior")]
        public bool enabled = true;
        
        [Tooltip("Maximum amplitude of random noise")]
        [Range(0.01f, 0.3f)]
        public float noiseAmplitude = 0.1f;
        
        [Tooltip("Frequency of noise changes per second")]
        [Range(1f, 20f)]
        public float noiseFrequency = 8f;
        
        [Tooltip("Duration of each jitter episode (seconds)")]
        public float jitterDuration = 3f;
        
        [Tooltip("Cooldown between jitter episodes (seconds)")]
        public float jitterCooldown = 5f;
    }
    
    [System.Serializable]
    public class PulsePushSettings
    {
        [Tooltip("Enable pulse push behavior")]
        public bool enabled = true;
        
        [Tooltip("Strength of each pulse push")]
        [Range(0.1f, 0.5f)]
        public float pushStrength = 0.3f;
        
        [Tooltip("Interval between pulses (seconds)")]
        public float pushInterval = 4f;
        
        [Tooltip("If true, push direction is random; if false, alternates")]
        public bool randomDirection = true;
    }
    #endregion
    
    #region Settings
    [Header("Target")]
    [SerializeField, Tooltip("The cage to interfere with (auto-finds if null)")]
    private TimableCage targetCage;
    
    [Header("Boss Activation")]
    [SerializeField, Tooltip("Should boss start active?")]
    private bool startActive = true;
    
    [SerializeField, Tooltip("Delay before boss starts interfering (seconds)")]
    private float activationDelay = 2f;
    
    [Header("Interference Behaviors")]
    [SerializeField]
    private ReversePullSettings reversePull = new ReversePullSettings();
    
    [SerializeField]
    private NoiseJitterSettings noiseJitter = new NoiseJitterSettings();
    
    [SerializeField]
    private PulsePushSettings pulsePush = new PulsePushSettings();
    
    [Header("Difficulty Scaling")]
    [SerializeField, Tooltip("Increase difficulty as more knots are solved")]
    private bool scaleDifficulty = true;
    
    [SerializeField, Tooltip("Difficulty multiplier per solved knot")]
    [Range(1f, 2f)]
    private float difficultyMultiplierPerKnot = 1.2f;
    
    [Header("Visual Feedback")]
    [SerializeField, Tooltip("Visual indicator when boss is actively interfering")]
    private GameObject interferenceIndicator;
    
    [Header("Audio")]
    [SerializeField] private AudioClip reversePullSound;
    [SerializeField] private AudioClip jitterSound;
    [SerializeField] private AudioClip pulseSound;
    #endregion
    
    #region State
    private bool isActive = false;
    private TimeAnchor currentTimeAnchor;
    private AudioSource audioSource;
    
    // Behavior state
    private bool isReversePulling = false;
    private bool isJittering = false;
    private float nextPulsePushTime = 0f;
    private float currentDifficultyMultiplier = 1f;
    
    // Noise generation
    private float noisePhase = 0f;
    private int lastPushDirection = 1;
    #endregion
    
    #region Events
    /// <summary>
    /// Fired when boss starts a specific interference type
    /// </summary>
    public event System.Action<string> OnInterferenceStarted;
    
    /// <summary>
    /// Fired when boss stops a specific interference type
    /// </summary>
    public event System.Action<string> OnInterferenceStopped;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound for boss
        }
        
        // Hide indicator initially
        if (interferenceIndicator != null)
        {
            interferenceIndicator.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Auto-find cage if not assigned
        if (targetCage == null)
        {
            targetCage = FindAnyObjectByType<TimableCage>();
        }
        
        // Subscribe to cage events
        if (targetCage != null)
        {
            targetCage.OnKnotActivated += OnKnotActivated;
            targetCage.OnCageCompleted += OnCageCompleted;
            
            // Get time anchor reference
            currentTimeAnchor = targetCage.TimeAnchor;
        }
        
        // Start boss if configured
        if (startActive)
        {
            StartCoroutine(DelayedActivation());
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (targetCage != null)
        {
            targetCage.OnKnotActivated -= OnKnotActivated;
            targetCage.OnCageCompleted -= OnCageCompleted;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void Update()
    {
        if (!isActive || currentTimeAnchor == null) return;
        
        // Handle continuous behaviors
        if (noiseJitter.enabled && isJittering)
        {
            ApplyNoiseJitter();
        }
        
        // Handle pulse push timing
        if (pulsePush.enabled && Time.time >= nextPulsePushTime)
        {
            ApplyPulsePush();
            nextPulsePushTime = Time.time + pulsePush.pushInterval / currentDifficultyMultiplier;
        }
    }
    #endregion
    
    #region Activation
    private IEnumerator DelayedActivation()
    {
        yield return new WaitForSeconds(activationDelay);
        ActivateBoss();
    }
    
    /// <summary>
    /// Activate the boss interference system
    /// </summary>
    public void ActivateBoss()
    {
        if (isActive) return;
        
        isActive = true;
        currentDifficultyMultiplier = 1f;
        
        Debug.Log("BossController: Boss activated! Beginning interference...");
        
        // Start interference behaviors
        if (reversePull.enabled)
        {
            StartCoroutine(ReversePullRoutine());
        }
        
        if (noiseJitter.enabled)
        {
            StartCoroutine(NoiseJitterRoutine());
        }
        
        if (pulsePush.enabled)
        {
            nextPulsePushTime = Time.time + pulsePush.pushInterval;
        }
        
        // Show indicator
        if (interferenceIndicator != null)
        {
            interferenceIndicator.SetActive(true);
        }
    }
    
    /// <summary>
    /// Deactivate the boss interference system
    /// </summary>
    public void DeactivateBoss()
    {
        if (!isActive) return;
        
        isActive = false;
        isReversePulling = false;
        isJittering = false;
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Disable interference on time anchor
        if (currentTimeAnchor != null)
        {
            currentTimeAnchor.SetBossInterference(false);
        }
        
        // Hide indicator
        if (interferenceIndicator != null)
        {
            interferenceIndicator.SetActive(false);
        }
        
        Debug.Log("BossController: Boss deactivated.");
    }
    #endregion
    
    #region Reverse Pull Behavior
    /// <summary>
    /// Coroutine that periodically applies reverse pull force
    /// </summary>
    private IEnumerator ReversePullRoutine()
    {
        while (isActive && reversePull.enabled)
        {
            // Wait for cooldown
            yield return new WaitForSeconds(reversePull.pullCooldown / currentDifficultyMultiplier);
            
            if (!isActive) break;
            
            // Start pull
            isReversePulling = true;
            OnInterferenceStarted?.Invoke("ReversePull");
            
            // Play sound
            if (audioSource != null && reversePullSound != null)
            {
                audioSource.PlayOneShot(reversePullSound);
            }
            
            // Calculate pull direction
            float pullDir = reversePull.pullDirection;
            if (reversePull.pullAwayFromKnot && targetCage != null)
            {
                // Pull away from the current knot region center
                pullDir = CalculatePullAwayDirection();
            }
            
            // Configure and enable boss interference on TimeAnchor
            if (currentTimeAnchor != null)
            {
                float adjustedStrength = reversePull.pullStrength * currentDifficultyMultiplier;
                currentTimeAnchor.ConfigureBossInterference(adjustedStrength, pullDir);
                currentTimeAnchor.SetBossInterference(true);
            }
            
            Debug.Log($"BossController: Reverse pull started (direction: {pullDir:F2})");
            
            // Wait for pull duration
            yield return new WaitForSeconds(reversePull.pullDuration);
            
            // Stop pull
            isReversePulling = false;
            if (currentTimeAnchor != null)
            {
                currentTimeAnchor.SetBossInterference(false);
            }
            
            OnInterferenceStopped?.Invoke("ReversePull");
            Debug.Log("BossController: Reverse pull stopped");
        }
    }
    
    /// <summary>
    /// Calculate direction that pulls away from current knot region
    /// </summary>
    private float CalculatePullAwayDirection()
    {
        if (targetCage == null || targetCage.KnotRegions == null) return 0f;
        
        int knotIndex = targetCage.CurrentActiveKnotIndex;
        if (knotIndex >= targetCage.KnotRegions.Length) return 0f;
        
        var knot = targetCage.KnotRegions[knotIndex];
        float knotCenter = (knot.minTimeValue + knot.maxTimeValue) / 2f;
        float currentValue = currentTimeAnchor != null ? currentTimeAnchor.TimeValue : 0f;
        
        // Pull in opposite direction from knot center
        // If player is below center, pull further down; if above, pull further up
        // This makes it harder to reach and stay in the knot
        if (currentValue < knotCenter)
        {
            return -1f; // Pull toward past (away from higher values)
        }
        else
        {
            return 1f; // Pull toward future (away from lower values)
        }
    }
    #endregion
    
    #region Noise Jitter Behavior
    /// <summary>
    /// Coroutine that periodically enables noise jitter
    /// </summary>
    private IEnumerator NoiseJitterRoutine()
    {
        while (isActive && noiseJitter.enabled)
        {
            // Wait for cooldown
            yield return new WaitForSeconds(noiseJitter.jitterCooldown / currentDifficultyMultiplier);
            
            if (!isActive) break;
            
            // Start jitter
            isJittering = true;
            noisePhase = 0f;
            OnInterferenceStarted?.Invoke("Jitter");
            
            // Play sound
            if (audioSource != null && jitterSound != null)
            {
                audioSource.PlayOneShot(jitterSound);
            }
            
            Debug.Log("BossController: Noise jitter started");
            
            // Wait for jitter duration
            yield return new WaitForSeconds(noiseJitter.jitterDuration);
            
            // Stop jitter
            isJittering = false;
            OnInterferenceStopped?.Invoke("Jitter");
            Debug.Log("BossController: Noise jitter stopped");
        }
    }
    
    /// <summary>
    /// Apply noise jitter effect to the timeline (called every frame during jitter)
    /// </summary>
    private void ApplyNoiseJitter()
    {
        if (currentTimeAnchor == null) return;
        
        // Update noise phase
        noisePhase += Time.deltaTime * noiseJitter.noiseFrequency;
        
        // Generate noise using multiple sine waves for more organic feel
        float noise1 = Mathf.Sin(noisePhase * 2.1f) * 0.5f;
        float noise2 = Mathf.Sin(noisePhase * 3.7f) * 0.3f;
        float noise3 = Mathf.Sin(noisePhase * 5.3f) * 0.2f;
        float combinedNoise = (noise1 + noise2 + noise3);
        
        // Apply noise with adjusted amplitude
        float adjustedAmplitude = noiseJitter.noiseAmplitude * currentDifficultyMultiplier;
        float noiseAmount = combinedNoise * adjustedAmplitude * Time.deltaTime * 10f;
        
        currentTimeAnchor.ApplyTimeJolt(noiseAmount);
    }
    #endregion
    
    #region Pulse Push Behavior
    /// <summary>
    /// Apply a sudden push to the timeline
    /// </summary>
    private void ApplyPulsePush()
    {
        if (currentTimeAnchor == null) return;
        
        // Determine push direction
        float pushDir;
        if (pulsePush.randomDirection)
        {
            pushDir = Random.value > 0.5f ? 1f : -1f;
        }
        else
        {
            // Alternate direction
            lastPushDirection *= -1;
            pushDir = lastPushDirection;
        }
        
        // Apply push
        float adjustedStrength = pulsePush.pushStrength * currentDifficultyMultiplier;
        currentTimeAnchor.ApplyTimeJolt(pushDir * adjustedStrength);
        
        // Play sound
        if (audioSource != null && pulseSound != null)
        {
            audioSource.PlayOneShot(pulseSound);
        }
        
        OnInterferenceStarted?.Invoke("Pulse");
        Debug.Log($"BossController: Pulse push applied (direction: {pushDir:F0}, strength: {adjustedStrength:F2})");
        
        // Fire stopped event immediately for pulse (it's instantaneous)
        OnInterferenceStopped?.Invoke("Pulse");
    }
    #endregion
    
    #region Event Handlers
    /// <summary>
    /// Called when a knot is activated - increase difficulty
    /// </summary>
    private void OnKnotActivated(int knotIndex, TimableCage.KnotRegion knot)
    {
        if (scaleDifficulty)
        {
            currentDifficultyMultiplier *= difficultyMultiplierPerKnot;
            Debug.Log($"BossController: Difficulty increased to {currentDifficultyMultiplier:F2}x");
        }
    }
    
    /// <summary>
    /// Called when cage is completed - deactivate boss
    /// </summary>
    private void OnCageCompleted()
    {
        DeactivateBoss();
        Debug.Log("BossController: Cage completed, boss defeated!");
    }
    #endregion
    
    #region Public API
    /// <summary>
    /// Temporarily boost interference intensity
    /// </summary>
    public void BoostInterference(float multiplier, float duration)
    {
        StartCoroutine(TemporaryBoostRoutine(multiplier, duration));
    }
    
    private IEnumerator TemporaryBoostRoutine(float multiplier, float duration)
    {
        float originalMultiplier = currentDifficultyMultiplier;
        currentDifficultyMultiplier *= multiplier;
        
        yield return new WaitForSeconds(duration);
        
        currentDifficultyMultiplier = originalMultiplier;
    }
    
    /// <summary>
    /// Enable or disable specific interference behavior at runtime
    /// </summary>
    public void SetBehaviorEnabled(string behaviorName, bool enabled)
    {
        switch (behaviorName.ToLower())
        {
            case "reversepull":
                reversePull.enabled = enabled;
                if (enabled && isActive && !isReversePulling)
                {
                    StartCoroutine(ReversePullRoutine());
                }
                break;
            case "jitter":
                noiseJitter.enabled = enabled;
                if (enabled && isActive && !isJittering)
                {
                    StartCoroutine(NoiseJitterRoutine());
                }
                else if (!enabled)
                {
                    isJittering = false;
                }
                break;
            case "pulse":
                pulsePush.enabled = enabled;
                break;
        }
    }
    
    /// <summary>
    /// Get current interference status
    /// </summary>
    public bool IsInterfering => isActive && (isReversePulling || isJittering);
    
    /// <summary>
    /// Get current difficulty multiplier
    /// </summary>
    public float CurrentDifficulty => currentDifficultyMultiplier;
    #endregion
}
