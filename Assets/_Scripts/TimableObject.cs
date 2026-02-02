using UnityEngine;

/// <summary>
/// Base class for all objects that can be manipulated through time.
/// Subclasses must implement ApplyTimeValue to define their specific behavior.
/// 
/// Purpose: Provides a common interface for all time-affected objects.
/// Any object that responds to timeline changes should inherit from this.
/// </summary>
[RequireComponent(typeof(TimeAnchor))]
public abstract class TimableObject : MonoBehaviour
{
    [Header("Timable Object Settings")]
    [SerializeField, Tooltip("Tag to identify this as a timable object")]
    protected string timableTag = "Timable";
    
    [SerializeField, Tooltip("Visual feedback when being manipulated")]
    protected bool showVisualFeedback = true;
    
    [SerializeField, Tooltip("Color when time is being manipulated")]
    protected Color manipulationColor = new Color(0.5f, 1f, 1f, 0.5f);
    
    // Reference to the time anchor component
    protected TimeAnchor timeAnchor;
    
    // Cached renderer for visual feedback
    protected Renderer objectRenderer;
    private Color originalColor;
    private bool wasBeingManipulated = false;
    
    /// <summary>
    /// Public accessor to the TimeAnchor
    /// </summary>
    public TimeAnchor TimeAnchor => timeAnchor;
    
    /// <summary>
    /// Get the manipulation color for hover highlight effect
    /// </summary>
    public Color GetManipulationColor() => manipulationColor;
    
    protected virtual void Awake()
    {
        // Get or add TimeAnchor component
        timeAnchor = GetComponent<TimeAnchor>();
        if (timeAnchor == null)
        {
            timeAnchor = gameObject.AddComponent<TimeAnchor>();
        }
        
        // Set the tag
        if (!string.IsNullOrEmpty(timableTag))
        {
            gameObject.tag = timableTag;
        }
        
        // Cache renderer for visual feedback
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.material != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }
    
    protected virtual void OnEnable()
    {
        // Subscribe to time value changes
        if (timeAnchor != null)
        {
            timeAnchor.OnTimeValueChanged += OnTimeValueChanged;
        }
    }
    
    protected virtual void OnDisable()
    {
        // Unsubscribe from time value changes
        if (timeAnchor != null)
        {
            timeAnchor.OnTimeValueChanged -= OnTimeValueChanged;
        }
    }
    
    protected virtual void Update()
    {
        // Handle visual feedback
        if (showVisualFeedback && objectRenderer != null)
        {
            bool isManipulated = timeAnchor.IsBeingManipulated;
            
            if (isManipulated != wasBeingManipulated)
            {
                if (isManipulated)
                {
                    objectRenderer.material.color = manipulationColor;
                }
                else
                {
                    objectRenderer.material.color = originalColor;
                }
                wasBeingManipulated = isManipulated;
            }
        }
    }
    
    /// <summary>
    /// Called when the time value changes
    /// </summary>
    private void OnTimeValueChanged(float newTimeValue)
    {
        ApplyTimeValue(newTimeValue);
    }
    
    /// <summary>
    /// Override this method to define how this object responds to time changes
    /// </summary>
    /// <param name="timeValue">Current time value (-1 = past, 0 = stable, 1 = future)</param>
    protected abstract void ApplyTimeValue(float timeValue);
    
    /// <summary>
    /// Helper method to map time value to a normalized range (0 to 1)
    /// </summary>
    protected float TimeValueToNormalized(float timeValue)
    {
        return (timeValue + 1f) / 2f; // Maps -1..1 to 0..1
    }
    
    /// <summary>
    /// Helper method to evaluate a curve based on time value
    /// </summary>
    protected float EvaluateTimeValue(AnimationCurve curve, float timeValue)
    {
        float normalized = TimeValueToNormalized(timeValue);
        return curve.Evaluate(normalized);
    }
}
