using UnityEngine;

/// <summary>
/// A simple Timable object that acts as a controller for other objects.
/// This object itself doesn't change when time value changes, 
/// but other objects (like platforms) can reference its TimeAnchor.
/// 
/// Purpose: Attach to stone pillars that control floating platforms.
/// The pillar is what the player aims at and manipulates.
/// </summary>
public class TimableController : TimableObject
{
    [Header("Controller Settings")]
    [SerializeField, Tooltip("Optional: Visual indicator when being manipulated")]
    private GameObject manipulationIndicator;
    
    [SerializeField, Tooltip("Optional: Particle effect when manipulating")]
    private ParticleSystem manipulationParticles;
    
    private bool wasManipulated = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Hide indicator initially
        if (manipulationIndicator != null)
        {
            manipulationIndicator.SetActive(false);
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Track manipulation state for visual feedback
        bool isCurrentlyManipulated = timeAnchor != null && timeAnchor.IsBeingManipulated;
        
        if (isCurrentlyManipulated != wasManipulated)
        {
            OnManipulationStateChanged(isCurrentlyManipulated);
            wasManipulated = isCurrentlyManipulated;
        }
    }
    
    /// <summary>
    /// Called when manipulation state changes - override for custom effects
    /// </summary>
    protected virtual void OnManipulationStateChanged(bool isManipulating)
    {
        // Show/hide indicator
        if (manipulationIndicator != null)
        {
            manipulationIndicator.SetActive(isManipulating);
        }
        
        // Play/stop particles
        if (manipulationParticles != null)
        {
            if (isManipulating)
            {
                manipulationParticles.Play();
            }
            else
            {
                manipulationParticles.Stop();
            }
        }
    }
    
    /// <summary>
    /// This controller doesn't change based on time value.
    /// The TimeAnchor just stores the value for other objects to read.
    /// </summary>
    protected override void ApplyTimeValue(float timeValue)
    {
        // Intentionally empty - this controller doesn't respond to time changes
        // Other objects (like TimablePlatform) read the TimeAnchor value
    }
}
