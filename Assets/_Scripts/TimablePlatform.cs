using UnityEngine;

/// <summary>
/// Platform that moves based on time value OR auto-moves when not controlled.
/// When player controls via timeline: TimeValue directly controls position.
/// When not controlled: Platform auto-moves back and forth.
/// 
/// Purpose: Floating platform puzzle - part of the time manipulation system.
/// Attach to platform GameObjects. Stone pillars are the Timable objects that control them.
/// 
/// NOTE: This does NOT inherit from TimableObject because the TimeAnchor is on a DIFFERENT object (stone pillar).
/// </summary>
public class TimablePlatform : MonoBehaviour
{
    [Header("Time Anchor Reference")]
    [SerializeField, Tooltip("Reference to the TimeAnchor on the stone pillar that controls this platform")]
    private TimeAnchor timeAnchor;
    
    [Header("Platform Movement Settings")]
    [SerializeField, Tooltip("Smooth movement speed when player controls")]
    private float movementSpeed = 3f;
    
    [Header("Simple Linear Movement")]
    [SerializeField, Tooltip("Position when TimeValue = -1 (left/start)")]
    private Vector3 startPosition;
    
    [SerializeField, Tooltip("Position when TimeValue = +1 (right/end)")]
    private Vector3 endPosition;
    
    [Header("Auto Movement (when not controlled)")]
    [SerializeField, Tooltip("Enable auto movement when player is not controlling")]
    private bool enableAutoMovement = false;
    
    [SerializeField, Tooltip("Auto movement speed (platform moves on its own)")]
    private float autoMoveSpeed = 0.3f;
    
    [SerializeField, Tooltip("Positive direction: true = start→end first, false = end→start first")]
    private bool positiveDirection = true;
    
    [SerializeField, Tooltip("Pause time at each end before reversing")]
    private float pauseAtEnds = 0.3f;
    
    [Header("Time Sync Settings")]
    [SerializeField, Tooltip("Sync TimeAnchor value with auto movement progress")]
    private bool syncTimeValueWithProgress = true;
    
    // Current target position
    private Vector3 targetPosition;
    
    // Auto movement state
    private float currentProgress = 0.5f; // 0 = start, 1 = end
    private int moveDirection = 1; // 1 = toward end, -1 = toward start
    private float pauseTimer = 0f;
    private bool wasBeingManipulated = false;
    
    // Cached initial position for offset calculation
    private Vector3 initialPosition;
    private bool positionsInitialized = false;
    
    // Cached last time value to detect changes
    private float lastTimeValue = 0f;
    
    private void Awake()
    {
        initialPosition = transform.position;
        
        // Initialize positions if not set
        if (startPosition == Vector3.zero && endPosition == Vector3.zero)
        {
            startPosition = initialPosition + Vector3.left * 5f;
            endPosition = initialPosition + Vector3.right * 5f;
        }
        
        positionsInitialized = true;
        
        // Set initial direction based on setting
        moveDirection = positiveDirection ? 1 : -1;
        
        // Initialize progress based on direction:
        // positiveDirection = true (向右移动): 从最左边开始 (progress = 0)
        // positiveDirection = false (向左移动): 从最右边开始 (progress = 1)
        currentProgress = positiveDirection ? 0f : 1f;
        
        // Sync TimeValue with initial progress
        if (timeAnchor != null)
        {
            float initialTimeValue = (currentProgress * 2f) - 1f; // Map 0..1 to -1..1
            timeAnchor.SetTimeValue(initialTimeValue);
        }
        
        // Set initial target position
        UpdatePositionFromProgress(currentProgress);
    }
    
    private void Update()
    {
        if (timeAnchor == null)
        {
            Debug.LogWarning($"TimablePlatform '{name}': No TimeAnchor assigned! Please assign the stone pillar's TimeAnchor.");
            return;
        }
        
        // Check if player is currently manipulating
        bool isCurrentlyManipulated = timeAnchor != null && timeAnchor.IsBeingManipulated;
        
        // When player stops controlling, sync progress from TimeValue
        if (wasBeingManipulated && !isCurrentlyManipulated)
        {
            currentProgress = (timeAnchor.TimeValue + 1f) / 2f;
            // Reset direction based on where we are
            if (currentProgress > 0.5f)
            {
                moveDirection = positiveDirection ? -1 : 1;
            }
            else
            {
                moveDirection = positiveDirection ? 1 : -1;
            }
        }
        
        wasBeingManipulated = isCurrentlyManipulated;
        
        // Auto movement when not being controlled
        if (enableAutoMovement && !isCurrentlyManipulated)
        {
            ProcessAutoMovement();
        }
        else if (isCurrentlyManipulated)
        {
            // When being manipulated, apply time value directly
            ApplyTimeValue(timeAnchor.TimeValue);
        }
        
        // Smoothly move to target position
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * movementSpeed
        );
    }
    
    /// <summary>
    /// Process automatic back-and-forth movement
    /// </summary>
    private void ProcessAutoMovement()
    {
        // Handle pause at ends
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }
        
        // Calculate movement delta
        float delta = autoMoveSpeed * Time.deltaTime * moveDirection;
        currentProgress += delta;
        
        // Check bounds and reverse
        if (currentProgress >= 1f)
        {
            currentProgress = 1f;
            moveDirection = -1;
            pauseTimer = pauseAtEnds;
        }
        else if (currentProgress <= 0f)
        {
            currentProgress = 0f;
            moveDirection = 1;
            pauseTimer = pauseAtEnds;
        }
        
        // Update position based on progress
        UpdatePositionFromProgress(currentProgress);
        
        // Sync TimeValue with progress if enabled (so UI shows correct value)
        if (syncTimeValueWithProgress && timeAnchor != null)
        {
            float newTimeValue = (currentProgress * 2f) - 1f; // Map 0..1 to -1..1
            timeAnchor.SetTimeValue(newTimeValue);
        }
    }
    
    /// <summary>
    /// Apply time value to platform position (called when player controls timeline)
    /// </summary>
    private void ApplyTimeValue(float timeValue)
    {
        // Map -1..1 to 0..1 progress
        float progress = (timeValue + 1f) / 2f;
        
        // Update progress and position
        currentProgress = progress;
        UpdatePositionFromProgress(progress);
    }
    
    /// <summary>
    /// Update target position based on progress (0 to 1)
    /// </summary>
    private void UpdatePositionFromProgress(float progress)
    {
        targetPosition = Vector3.Lerp(startPosition, endPosition, progress);
    }
    
    /// <summary>
    /// Set positions at runtime
    /// </summary>
    public void SetMovementRange(Vector3 start, Vector3 end)
    {
        startPosition = start;
        endPosition = end;
    }
    
    /// <summary>
    /// Get current progress (0 to 1)
    /// </summary>
    public float GetProgress()
    {
        return currentProgress;
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        Vector3 start = positionsInitialized ? startPosition : transform.position + Vector3.left * 5f;
        Vector3 end = positionsInitialized ? endPosition : transform.position + Vector3.right * 5f;
        
        // Draw path line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);
        
        // Draw start point (green)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, 0.3f);
        
        // Draw end point (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, 0.3f);
        
        // Draw direction arrow
        Gizmos.color = positiveDirection ? Color.yellow : Color.magenta;
        Vector3 midPoint = (start + end) / 2f;
        Vector3 direction = (positiveDirection ? (end - start) : (start - end)).normalized;
        Gizmos.DrawRay(midPoint, direction * 0.8f);
        
        // Draw current position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        Vector3 start = positionsInitialized ? startPosition : transform.position + Vector3.left * 5f;
        Vector3 end = positionsInitialized ? endPosition : transform.position + Vector3.right * 5f;
        
        UnityEditor.Handles.Label(start + Vector3.up * 0.5f, "起点 (TimeValue=-1)");
        UnityEditor.Handles.Label(end + Vector3.up * 0.5f, "终点 (TimeValue=+1)");
        
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up, 
                $"Progress: {currentProgress:F2}\nDir: {(moveDirection > 0 ? "→" : "←")}\nControlled: {wasBeingManipulated}");
        }
        #endif
    }
}
