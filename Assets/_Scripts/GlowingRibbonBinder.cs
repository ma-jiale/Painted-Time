using UnityEngine;
using UnityEngine.UI;

// Binds a Scrollbar or Slider value to the material _Progress for the glowing ribbon shader.
[RequireComponent(typeof(Graphic))]
public class GlowingRibbonBinder : MonoBehaviour
{
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private Slider slider;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private string progressProperty = "_Progress";

    void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    void OnEnable()
    {
        if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        BindEvents(true);
        UpdateProgress();
    }

    void OnDisable()
    {
        BindEvents(false);
    }

    void BindEvents(bool subscribe)
    {
        if (scrollbar != null)
        {
            if (subscribe) scrollbar.onValueChanged.AddListener(OnValueChanged);
            else scrollbar.onValueChanged.RemoveListener(OnValueChanged);
        }
        if (slider != null)
        {
            if (subscribe) slider.onValueChanged.AddListener(OnValueChanged);
            else slider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }

    void OnValueChanged(float v)
    {
        SetProgress(v);
    }

    void UpdateProgress()
    {
        if (scrollbar != null) SetProgress(scrollbar.value);
        else if (slider != null) SetProgress(slider.value);
    }

    void SetProgress(float v)
    {
        if (targetGraphic == null) return;
        var mat = targetGraphic.materialForRendering;
        if (mat != null && mat.HasProperty(progressProperty))
            mat.SetFloat(progressProperty, Mathf.Clamp01(v));
    }
}
