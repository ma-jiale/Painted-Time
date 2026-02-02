using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller that displays boss interference status.
/// Shows visual warnings when boss is actively interfering with the timeline.
/// 
/// Purpose: Provides player feedback about boss actions so they can react.
/// Attach to a UI GameObject with warning indicators.
/// </summary>
public class BossInterferenceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("Warning text displayed during interference")]
    private Text warningText;
    
    [SerializeField, Tooltip("Warning icon/image")]
    private Image warningIcon;
    
    [SerializeField, Tooltip("Background panel for warnings")]
    private Image warningPanel;
    
    [Header("Colors")]
    [SerializeField] private Color reversePullColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color jitterColor = new Color(1f, 0.7f, 0.2f, 0.8f);
    [SerializeField] private Color pulseColor = new Color(0.8f, 0.2f, 1f, 0.8f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    [Header("Animation")]
    [SerializeField, Tooltip("Flash speed during interference")]
    private float flashSpeed = 3f;
    
    [SerializeField, Tooltip("Shake amount during jitter")]
    private float shakeAmount = 5f;
    
    // State
    private BossController bossController;
    private string currentInterference = "";
    private bool isShowingWarning = false;
    private Vector3 originalPosition;
    private float flashPhase = 0f;
    
    private void Start()
    {
        // Find boss controller
        bossController = BossController.Instance;
        if (bossController == null)
        {
            bossController = FindAnyObjectByType<BossController>();
        }
        
        // Subscribe to events
        if (bossController != null)
        {
            bossController.OnInterferenceStarted += OnInterferenceStarted;
            bossController.OnInterferenceStopped += OnInterferenceStopped;
        }
        
        // Cache original position for shake effect
        if (warningPanel != null)
        {
            originalPosition = warningPanel.rectTransform.anchoredPosition;
        }
        
        // Hide warning initially
        SetWarningVisible(false);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (bossController != null)
        {
            bossController.OnInterferenceStarted -= OnInterferenceStarted;
            bossController.OnInterferenceStopped -= OnInterferenceStopped;
        }
    }
    
    private void Update()
    {
        if (!isShowingWarning) return;
        
        // Flash effect
        flashPhase += Time.deltaTime * flashSpeed;
        float alpha = (Mathf.Sin(flashPhase) + 1f) / 2f * 0.5f + 0.5f;
        
        if (warningIcon != null)
        {
            Color iconColor = warningIcon.color;
            iconColor.a = alpha;
            warningIcon.color = iconColor;
        }
        
        // Shake effect during jitter
        if (currentInterference == "Jitter" && warningPanel != null)
        {
            float shakeX = Mathf.Sin(Time.time * 50f) * shakeAmount;
            float shakeY = Mathf.Cos(Time.time * 47f) * shakeAmount;
            warningPanel.rectTransform.anchoredPosition = originalPosition + new Vector3(shakeX, shakeY, 0);
        }
        else if (warningPanel != null)
        {
            warningPanel.rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    /// <summary>
    /// Called when boss starts an interference behavior
    /// </summary>
    private void OnInterferenceStarted(string interferenceType)
    {
        currentInterference = interferenceType;
        isShowingWarning = true;
        flashPhase = 0f;
        
        // Update UI based on interference type
        Color warningColor;
        string warningMessage;
        
        switch (interferenceType)
        {
            case "ReversePull":
                warningColor = reversePullColor;
                warningMessage = "Timeline Distortion!";
                break;
            case "Jitter":
                warningColor = jitterColor;
                warningMessage = "Temporal Interference!";
                break;
            case "Pulse":
                warningColor = pulseColor;
                warningMessage = "Time Pulse!";
                break;
            default:
                warningColor = inactiveColor;
                warningMessage = "Boss Interference!";
                break;
        }
        
        if (warningText != null)
        {
            warningText.text = warningMessage;
            warningText.color = warningColor;
        }
        
        if (warningPanel != null)
        {
            warningPanel.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0.3f);
        }
        
        if (warningIcon != null)
        {
            warningIcon.color = warningColor;
        }
        
        SetWarningVisible(true);
    }
    
    /// <summary>
    /// Called when boss stops an interference behavior
    /// </summary>
    private void OnInterferenceStopped(string interferenceType)
    {
        // Only hide if this was the current interference
        if (interferenceType == currentInterference)
        {
            currentInterference = "";
            isShowingWarning = false;
            SetWarningVisible(false);
            
            // Reset panel position
            if (warningPanel != null)
            {
                warningPanel.rectTransform.anchoredPosition = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// Show or hide the warning UI
    /// </summary>
    private void SetWarningVisible(bool visible)
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(visible);
        }
        
        if (warningIcon != null)
        {
            warningIcon.gameObject.SetActive(visible);
        }
        
        if (warningPanel != null)
        {
            warningPanel.gameObject.SetActive(visible);
        }
    }
}
