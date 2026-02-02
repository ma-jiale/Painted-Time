using UnityEngine;

/// <summary>
/// Example implementation: Tree that grows/shrinks based on time value.
/// -1 (past) = small/young tree
///  0 (stable) = normal size
/// +1 (future) = large/old tree
/// 
/// Purpose: Demonstrates how to create a simple visual response to time changes.
/// Attach to tree models that should grow/shrink with time.
/// </summary>
public class TimableTree : TimableObject
{
    [Header("Tree Growth Settings")]
    [SerializeField, Tooltip("Scale when TimeValue = -1 (past/young)")]
    private float minScale = 0.3f;
    
    [SerializeField, Tooltip("Scale when TimeValue = 0 (present/normal)")]
    private float normalScale = 1.0f;
    
    [SerializeField, Tooltip("Scale when TimeValue = +1 (future/old)")]
    private float maxScale = 2.0f;
    
    [SerializeField, Tooltip("Optional: Different model for withered state")]
    private GameObject witheredModel;
    
    [SerializeField, Tooltip("Optional: Different model for flourishing state")]
    private GameObject flourishingModel;
    
    [SerializeField, Tooltip("Smooth transition speed")]
    private float transitionSpeed = 2f;
    
    // Target scale based on time value
    private Vector3 targetScale;
    private Vector3 initialScale;
    
    protected override void Awake()
    {
        base.Awake();
        initialScale = transform.localScale;
        targetScale = initialScale;
    }
    
    /// <summary>
    /// Apply time value to tree growth
    /// </summary>
    protected override void ApplyTimeValue(float timeValue)
    {
        // Map time value to scale multiplier
        float scaleMultiplier;
        
        if (timeValue < 0)
        {
            // Past: interpolate between min and normal
            scaleMultiplier = Mathf.Lerp(minScale, normalScale, (timeValue + 1f));
        }
        else
        {
            // Future: interpolate between normal and max
            scaleMultiplier = Mathf.Lerp(normalScale, maxScale, timeValue);
        }
        
        targetScale = initialScale * scaleMultiplier;
        
        // Handle model swapping if configured
        UpdateModelVisibility(timeValue);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Smoothly transition to target scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * transitionSpeed
        );
    }
    
    /// <summary>
    /// Optional: Switch between different tree models
    /// </summary>
    private void UpdateModelVisibility(float timeValue)
    {
        if (witheredModel != null)
        {
            witheredModel.SetActive(timeValue < -0.5f);
        }
        
        if (flourishingModel != null)
        {
            flourishingModel.SetActive(timeValue > 0.5f);
        }
    }
}
