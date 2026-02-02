using UnityEngine;
using System;

/// <summary>
/// Core component that stores and manages the TimeValue (-1 to 1) for a Timable object.
/// Handles boss interference and provides safe access to modify time.
/// 
/// Purpose: This is the "time state" holder. Any object that can be manipulated through time needs this.
/// </summary>
public class TimeAnchor : MonoBehaviour
{
    [Header("Time Configuration")]
    [SerializeField, Range(-1f, 1f)] 
    private float initialTimeValue = 0f;
    
    [SerializeField, Tooltip("How fast TimeValue changes when player drags the timeline")]
    private float timeChangeSpeed = 1.0f;
    
    [Header("Boss Interference")]
    [SerializeField, Tooltip("Is this object affected by boss interference?")]
    private bool allowBossInterference = true;
    
    [SerializeField, Tooltip("Strength of boss interference force")]
    private float bossInterferenceStrength = 0.3f;
    
    [SerializeField, Tooltip("Direction the boss tries to pull (-1 for past, 1 for future)")]
    private float bossInterferenceDirection = -1f;
    
    // Current time value (-1 = past, 0 = stable, 1 = future)
    private float currentTimeValue;
    
    // Is boss currently interfering?
    private bool isBossInterferingActive = false;
    
    // Events for external systems to listen to time changes
    public event Action<float> OnTimeValueChanged;
    
    /// <summary>
    /// Gets the current time value
    /// </summary>
    public float TimeValue => currentTimeValue;
    
    /// <summary>
    /// Gets whether player is currently manipulating this time anchor
    /// </summary>
    public bool IsBeingManipulated { get; private set; }
    
    private void Awake()
    {
        currentTimeValue = initialTimeValue;
    }
    
    private void Update()
    {
        // Apply boss interference when active
        if (isBossInterferingActive && allowBossInterference)
        {
            ApplyBossInterference(Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Modify the time value based on player input
    /// </summary>
    /// <param name="delta">Change amount (-1 to 1)</param>
    /// <param name="deltaTime">Time elapsed since last frame</param>
    public void ModifyTimeValue(float delta, float deltaTime)
    {
        IsBeingManipulated = true;
        
        float change = delta * timeChangeSpeed * deltaTime;
        SetTimeValue(currentTimeValue + change);
    }
    
    /// <summary>
    /// Directly set the time value (clamped to -1, 1)
    /// </summary>
    public void SetTimeValue(float newValue)
    {
        float oldValue = currentTimeValue;
        currentTimeValue = Mathf.Clamp(newValue, -1f, 1f);
        
        // Only notify if value actually changed
        if (Mathf.Abs(oldValue - currentTimeValue) > 0.001f)
        {
            OnTimeValueChanged?.Invoke(currentTimeValue);
        }
    }
    
    /// <summary>
    /// Reset manipulation state (call when player releases control)
    /// </summary>
    public void ReleaseManipulation()
    {
        IsBeingManipulated = false;
    }
    
    /// <summary>
    /// Enable or disable boss interference on this object
    /// </summary>
    public void SetBossInterference(bool active)
    {
        isBossInterferingActive = active;
    }
    
    /// <summary>
    /// Configure boss interference parameters at runtime
    /// </summary>
    public void ConfigureBossInterference(float strength, float direction)
    {
        bossInterferenceStrength = strength;
        bossInterferenceDirection = Mathf.Clamp(direction, -1f, 1f);
    }
    
    /// <summary>
    /// Apply boss interference force (pulls time toward a specific direction)
    /// </summary>
    private void ApplyBossInterference(float deltaTime)
    {
        // Boss tries to pull time in their preferred direction
        float targetValue = bossInterferenceDirection;
        float difference = targetValue - currentTimeValue;
        
        // Apply interference force (gentle pull, not instant)
        float interferenceAmount = difference * bossInterferenceStrength * deltaTime;
        SetTimeValue(currentTimeValue + interferenceAmount);
    }
    
    /// <summary>
    /// Apply a temporary "jolt" to the time value (for sudden boss attacks)
    /// </summary>
    public void ApplyTimeJolt(float joltAmount)
    {
        SetTimeValue(currentTimeValue + joltAmount);
    }
    
    // Debug visualization in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.Lerp(Color.red, Color.green, (currentTimeValue + 1f) / 2f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
