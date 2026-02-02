using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unified interaction manager handling all VR input:
/// - Pause/Resume game
/// - Hand animations
/// - Time manipulation (raycast targeting + timeline control)
/// 
/// This is a singleton that persists across scenes.
/// Attach this to the VR rig or a persistent manager object.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    #region Menu Input
    [Header("Menu Input")]
    [Tooltip("Primary button on VR controller to pause/resume game")]
    [SerializeField] private InputActionProperty pauseAction;
    #endregion

    #region Hand Animation
    [Header("Hand Animation - Left Hand")]
    [SerializeField] private InputActionProperty leftTriggerValue;
    [SerializeField] private InputActionProperty leftGripValue;
    [SerializeField] private Animator leftHandAnimator;

    [Header("Hand Animation - Right Hand")]
    [SerializeField] private InputActionProperty rightTriggerValue;
    [SerializeField] private InputActionProperty rightGripValue;
    [SerializeField] private Animator rightHandAnimator;
    #endregion

    #region Time Manipulation Input
    [Header("Time Manipulation - VR Input")]
    [SerializeField, Tooltip("Right hand controller stick (for timeline scrubbing)")]
    private InputActionProperty rightStickAction;
    
    [SerializeField, Tooltip("Right hand trigger (for time mode activation)")]
    private InputActionProperty rightTimeTriggerAction;
    
    [SerializeField, Tooltip("Origin for raycast (e.g., camera or right controller)")]
    private Transform rayOrigin;
    #endregion

    #region Raycast Settings
    [Header("Raycast Settings")]
    [SerializeField, Tooltip("Maximum distance to detect Timable objects")]
    private float maxRayDistance = 10f;
    
    [SerializeField, Tooltip("Layer mask for raycast")]
    private LayerMask raycastMask = ~0;
    
    [SerializeField, Tooltip("Tag to identify Timable objects")]
    private string timableTag = "Timable";
    #endregion

    #region Timeline UI
    [Header("Timeline UI")]
    [SerializeField, Tooltip("Reference to default timeline UI GameObject (fixed position in front of player)")]
    private GameObject timelineUI;
    
    [SerializeField, Tooltip("Reference to knot UI for cage puzzles (TimableKnotUI1)")]
    private GameObject timableKnotUI1;
    #endregion

    #region Visual Feedback
    [Header("Visual Feedback")]
    [SerializeField, Tooltip("Line renderer for aiming ray")]
    private LineRenderer aimRay;
    
    [SerializeField, Tooltip("Color when targeting Timable object")]
    private Color targetingColor = Color.cyan;
    
    [SerializeField, Tooltip("Color when not targeting")]
    private Color defaultRayColor = Color.white;
    
    [Header("Hover Highlight Settings")]
    [SerializeField, Tooltip("Flash speed when hovering over Timable object (cycles per second)")]
    private float hoverFlashSpeed = 2f;
    
    [SerializeField, Tooltip("Minimum alpha during flash (0-1)")]
    private float hoverFlashMinAlpha = 0.3f;
    #endregion

    // Time manipulation state
    private TimableObject currentTarget;
    private bool isManipulatingTime = false;
    private RaycastHit lastHit;
    
    // Active UI reference (switches between default and knot UI based on target type)
    private GameObject activeTimelineUI;
    private TimableKnotUI activeKnotUIController;
    
    // Hover highlight state
    private TimableObject hoveredTarget;
    private Renderer hoveredRenderer;
    private Color hoveredOriginalColor;
    private bool isFlashing = false;

    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Set ray origin to camera if not assigned
        if (rayOrigin == null)
        {
            rayOrigin = Camera.main?.transform;
        }
        
        // Hide timeline UI initially
        if (timelineUI != null)
        {
            timelineUI.SetActive(false);
        }
        
        // Hide knot UI initially
        if (timableKnotUI1 != null)
        {
            timableKnotUI1.SetActive(false);
        }
        
        // Setup line renderer
        if (aimRay != null)
        {
            aimRay.startWidth = 0.01f;
            aimRay.endWidth = 0.01f;
        }
    }

    private void OnEnable()
    {
        // Enable all input actions
        EnableInputActions();
    }

    private void OnDisable()
    {
        // Disable all input actions
        DisableInputActions();
        
        // Clean up if manipulating time
        if (isManipulatingTime)
        {
            ExitTimeManipulation();
        }
    }

    private void Update()
    {
        // Handle pause input
        HandlePauseInput();

        // Update hand animations
        UpdateHandAnimations();

        // Handle time manipulation
        HandleTimeManipulation();
    }
    #endregion

    #region Input Actions Management
    private void EnableInputActions()
    {
        pauseAction.action?.Enable();
        leftTriggerValue.action?.Enable();
        leftGripValue.action?.Enable();
        rightTriggerValue.action?.Enable();
        rightGripValue.action?.Enable();
        rightStickAction.action?.Enable();
        rightTimeTriggerAction.action?.Enable();
    }

    private void DisableInputActions()
    {
        pauseAction.action?.Disable();
        leftTriggerValue.action?.Disable();
        leftGripValue.action?.Disable();
        rightTriggerValue.action?.Disable();
        rightGripValue.action?.Disable();
        rightStickAction.action?.Disable();
        rightTimeTriggerAction.action?.Disable();
    }
    #endregion

    #region Pause Handling
    private void HandlePauseInput()
    {
        if (pauseAction.action != null && pauseAction.action.WasPressedThisFrame())
        {
            if (GameManager.Instance == null) return;

            // Only allow pause/resume during Playing or Pause states
            if (GameManager.Instance.currentState == GameManager.GameState.Playing ||
                GameManager.Instance.currentState == GameManager.GameState.Pause)
            {
                GameManager.Instance.TogglePause();
            }
        }
    }
    #endregion

    #region Hand Animation
    private void UpdateHandAnimations()
    {
        // Left hand animation
        if (leftHandAnimator != null)
        {
            float leftTrigger = leftTriggerValue.action?.ReadValue<float>() ?? 0f;
            float leftGrip = leftGripValue.action?.ReadValue<float>() ?? 0f;
            leftHandAnimator.SetFloat("Trigger", leftTrigger);
            leftHandAnimator.SetFloat("Grip", leftGrip);
        }

        // Right hand animation
        if (rightHandAnimator != null)
        {
            float rightTrigger = rightTriggerValue.action?.ReadValue<float>() ?? 0f;
            float rightGrip = rightGripValue.action?.ReadValue<float>() ?? 0f;
            rightHandAnimator.SetFloat("Trigger", rightTrigger);
            rightHandAnimator.SetFloat("Grip", rightGrip);
        }
    }
    #endregion

    #region Time Manipulation
    private void HandleTimeManipulation()
    {
        // Always perform raycast to show targeting
        DetectTimableObject();
        
        // Update hover highlight (flash effect when targeting but not manipulating)
        UpdateHoverHighlight();
        
        // Update visual ray
        UpdateAimRay();
        
        // Check trigger input for time manipulation
        float triggerValue = rightTimeTriggerAction.action?.ReadValue<float>() ?? 0f;
        bool triggerPressed = triggerValue > 0.5f;
        
        if (triggerPressed && !isManipulatingTime && currentTarget != null)
        {
            EnterTimeManipulation();
        }
        else if (!triggerPressed && isManipulatingTime)
        {
            ExitTimeManipulation();
        }
        
        // If manipulating, read stick input and modify time
        if (isManipulatingTime && currentTarget != null)
        {
            ProcessTimeManipulation();
            UpdateTimelineUI();
        }
    }

    /// <summary>
    /// Raycast to find Timable objects in view
    /// </summary>
    private void DetectTimableObject()
    {
        if (rayOrigin == null) return;
        
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out lastHit, maxRayDistance, raycastMask))
        {
            if (lastHit.collider.CompareTag(timableTag))
            {
                TimableObject timable = lastHit.collider.GetComponent<TimableObject>();
                if (timable != null)
                {
                    // Skip completed cages (they cannot be targeted anymore)
                    TimableCage cage = timable as TimableCage;
                    if (cage != null && cage.IsCageCompleted)
                    {
                        if (!isManipulatingTime)
                        {
                            currentTarget = null;
                        }
                        return;
                    }
                    
                    currentTarget = timable;
                    return;
                }
            }
        }
        
        // No valid target found
        if (!isManipulatingTime)
        {
            currentTarget = null;
        }
    }
    
    /// <summary>
    /// Update hover highlight effect - flash the manipulation color when targeting but not manipulating
    /// </summary>
    private void UpdateHoverHighlight()
    {
        // Determine if we should be flashing
        bool shouldFlash = currentTarget != null && !isManipulatingTime;
        
        // Handle target change
        if (shouldFlash && hoveredTarget != currentTarget)
        {
            // Stop flashing previous target
            StopHoverFlash();
            
            // Start flashing new target
            StartHoverFlash(currentTarget);
        }
        else if (!shouldFlash && isFlashing)
        {
            // Stop flashing when no target or started manipulating
            StopHoverFlash();
        }
        
        // Apply flash effect
        if (isFlashing && hoveredRenderer != null)
        {
            // Calculate flash alpha using sine wave
            float flash = (Mathf.Sin(Time.time * hoverFlashSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float alpha = Mathf.Lerp(hoverFlashMinAlpha, 1f, flash);
            
            // Get the manipulation color from the TimableObject
            Color targetColor = hoveredTarget.GetManipulationColor();
            Color flashColor = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * alpha);
            
            // Blend between original color and flash color
            hoveredRenderer.material.color = Color.Lerp(hoveredOriginalColor, flashColor, flash);
        }
    }
    
    /// <summary>
    /// Start flashing effect on a target
    /// </summary>
    private void StartHoverFlash(TimableObject target)
    {
        if (target == null) return;
        
        hoveredTarget = target;
        hoveredRenderer = target.GetComponent<Renderer>();
        
        if (hoveredRenderer != null && hoveredRenderer.material != null)
        {
            hoveredOriginalColor = hoveredRenderer.material.color;
            isFlashing = true;
        }
    }
    
    /// <summary>
    /// Stop flashing effect and restore original color
    /// </summary>
    private void StopHoverFlash()
    {
        if (hoveredRenderer != null && hoveredRenderer.material != null)
        {
            hoveredRenderer.material.color = hoveredOriginalColor;
        }
        
        hoveredTarget = null;
        hoveredRenderer = null;
        isFlashing = false;
    }

    /// <summary>
    /// Enter time manipulation mode
    /// </summary>
    private void EnterTimeManipulation()
    {
        if (currentTarget == null) return;
        
        isManipulatingTime = true;
        
        // Determine which UI to use based on target type
        SelectActiveUI();
        
        // Activate the selected UI
        if (activeTimelineUI != null)
        {
            activeTimelineUI.SetActive(true);
        }
        
        Debug.Log($"Entered time manipulation on: {currentTarget.name}");
    }

    /// <summary>
    /// Exit time manipulation mode
    /// </summary>
    private void ExitTimeManipulation()
    {
        isManipulatingTime = false;
        
        // Unsubscribe from cage completion event if applicable
        TimableCage cage = currentTarget as TimableCage;
        if (cage != null)
        {
            cage.OnCageCompleted -= OnCageCompleted;
        }
        
        if (currentTarget != null && currentTarget.TimeAnchor != null)
        {
            currentTarget.TimeAnchor.ReleaseManipulation();
        }
        
        // Clean up knot UI if it was active
        if (activeKnotUIController != null)
        {
            activeKnotUIController.Cleanup();
            activeKnotUIController = null;
        }
        
        // Hide the active UI
        if (activeTimelineUI != null)
        {
            activeTimelineUI.SetActive(false);
            activeTimelineUI = null;
        }
        
        // Also ensure both UIs are hidden
        if (timelineUI != null)
        {
            timelineUI.SetActive(false);
        }
        if (timableKnotUI1 != null)
        {
            timableKnotUI1.SetActive(false);
        }
        
        Debug.Log("Exited time manipulation");
    }
    
    /// <summary>
    /// Select the appropriate UI based on target type
    /// Uses TimableKnotUI1 for cages, default timeline UI for others
    /// </summary>
    private void SelectActiveUI()
    {
        // Check if target is a cage (use knot UI)
        TimableCage cage = currentTarget as TimableCage;
        
        if (cage != null && timableKnotUI1 != null)
        {
            // Use knot UI for cages
            activeTimelineUI = timableKnotUI1;
            activeKnotUIController = timableKnotUI1.GetComponent<TimableKnotUI>();
            
            // Subscribe to cage completion event to auto-exit when done
            cage.OnCageCompleted += OnCageCompleted;
            
            // Initialize knot UI with cage's knot regions
            if (activeKnotUIController != null)
            {
                activeKnotUIController.Initialize(cage);
            }
            
            Debug.Log($"Using TimableKnotUI1 for cage: {cage.name}");
        }
        else
        {
            // Use default timeline UI for other timable objects
            activeTimelineUI = timelineUI;
            activeKnotUIController = null;
            
            Debug.Log($"Using default timeline UI for: {currentTarget.name}");
        }
    }
    
    /// <summary>
    /// Called when a cage completes all knots - automatically exit manipulation
    /// </summary>
    private void OnCageCompleted()
    {
        Debug.Log("InteractionManager: Cage completed! Exiting time manipulation.");
        
        // Unsubscribe from the event
        TimableCage cage = currentTarget as TimableCage;
        if (cage != null)
        {
            cage.OnCageCompleted -= OnCageCompleted;
        }
        
        // Delay exit slightly so player can see completion message
        StartCoroutine(DelayedExitTimeManipulation(1.5f));
    }
    
    /// <summary>
    /// Exit time manipulation after a delay
    /// </summary>
    private System.Collections.IEnumerator DelayedExitTimeManipulation(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExitTimeManipulation();
    }

    /// <summary>
    /// Process stick input to modify time value
    /// </summary>
    private void ProcessTimeManipulation()
    {
        if (currentTarget?.TimeAnchor == null) return;
        
        Vector2 stickInput = rightStickAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        float horizontalInput = stickInput.x;
        
        // Apply dead zone
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            horizontalInput = 0f;
        }
        
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            currentTarget.TimeAnchor.ModifyTimeValue(horizontalInput, Time.deltaTime);
        }
    }

    /// <summary>
    /// Update timeline UI display (position is fixed, set in scene)
    /// </summary>
    private void UpdateTimelineUI()
    {
        if (activeTimelineUI == null || currentTarget == null) return;
        
        // Update the appropriate UI controller (position is fixed, set in scene)
        if (activeKnotUIController != null)
        {
            // Update knot UI for cages
            activeKnotUIController.UpdateTimeValue(currentTarget.TimeAnchor.TimeValue);
        }
        else
        {
            // Update default timeline UI
            TimelineUIController uiController = activeTimelineUI.GetComponent<TimelineUIController>();
            if (uiController != null && currentTarget.TimeAnchor != null)
            {
                uiController.UpdateTimeValue(currentTarget.TimeAnchor.TimeValue);
            }
        }
    }

    /// <summary>
    /// Update visual ray display
    /// </summary>
    private void UpdateAimRay()
    {
        if (aimRay == null || rayOrigin == null) return;
        
        aimRay.enabled = true;
        
        aimRay.startColor = currentTarget != null ? targetingColor : defaultRayColor;
        aimRay.endColor = currentTarget != null ? targetingColor : defaultRayColor;
        
        aimRay.SetPosition(0, rayOrigin.position);
        
        if (currentTarget != null && lastHit.collider != null)
        {
            aimRay.SetPosition(1, lastHit.point);
        }
        else
        {
            aimRay.SetPosition(1, rayOrigin.position + rayOrigin.forward * maxRayDistance);
        }
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Returns true if currently manipulating time on a target
    /// </summary>
    public bool IsManipulatingTime => isManipulatingTime;

    /// <summary>
    /// Returns the current Timable target, if any
    /// </summary>
    public TimableObject CurrentTarget => currentTarget;
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        if (rayOrigin == null) return;
        
        Gizmos.color = currentTarget != null ? Color.green : Color.white;
        Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * maxRayDistance);
    }
    #endregion
}
