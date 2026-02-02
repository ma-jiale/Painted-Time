using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for the timeline interface.
/// Displays current time value and time state indicators.
/// 
/// Purpose: Provides visual feedback to the player during time manipulation.
/// Attach to the Timeline UI GameObject (Canvas).
/// </summary>
public class TimelineUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("UI Image/RawImage with material containing Progress parameter")]
    private Graphic timelineGraphic;
    
    [SerializeField, Tooltip("Name of the progress property in shader")]
    private string progressPropertyName = "_Progress";
    
    [SerializeField, Tooltip("Text showing current time value (使用旧版Text支持中文)")]
    private Text timeValueText;
    
    [SerializeField, Tooltip("Text showing time state (过去/现在/未来)")]
    private Text timeStateText;
    
    [Header("Colors")]
    [SerializeField] private Color pastColor = new Color(0.5f, 0.5f, 1f);
    [SerializeField] private Color presentColor = Color.white;
    [SerializeField] private Color futureColor = new Color(1f, 1f, 0.5f);
    
    // Current time value being displayed
    private float currentTimeValue = 0f;
    
    // Cached material instance
    private Material timelineMaterial;
    
    // Cached property ID for better performance
    private int progressPropertyId;
    
    private void Awake()
    {
        // Cache the material and property ID
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
    }
    
    private void OnDestroy()
    {
        // Clean up the material instance to prevent memory leaks
        if (timelineMaterial != null)
        {
            Destroy(timelineMaterial);
        }
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
            timeValueText.text = $"时间: {timeValue:F2}";
        }
        
        // Update state text and color
        UpdateTimeState(timeValue);
    }
    
    /// <summary>
    /// Update time state text and colors
    /// </summary>
    private void UpdateTimeState(float timeValue)
    {
        string stateText;
        Color stateColor;
        
        if (timeValue < -0.3f)
        {
            stateText = "过去";
            stateColor = pastColor;
        }
        else if (timeValue > 0.3f)
        {
            stateText = "未来";
            stateColor = futureColor;
        }
        else
        {
            stateText = "现在";
            stateColor = presentColor;
        }
        
        if (timeStateText != null)
        {
            timeStateText.text = stateText;
            timeStateText.color = stateColor;
        }
        
        // Apply color to timeline material (optional - if your shader supports it)
        if (timelineMaterial != null && timelineMaterial.HasProperty("_Color"))
        {
            timelineMaterial.SetColor("_Color", stateColor);
        }
    }
}
