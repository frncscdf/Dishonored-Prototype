using UnityEngine.Rendering.PostProcessing;
using UnityEngine;
using System.Collections;

public class PPManager : Singleton<PPManager>
{
    private PostProcessVolume m_PPVolume;
    private ColorGrading m_ColorGrading;
    private Vignette m_Vignette;

    public Vignette Vignette { get => m_Vignette; }
    public ColorGrading ColorGrading { get => m_ColorGrading; }

    public void Awake()
    {
        m_PPVolume = GameObject.Find("PlayerA").GetComponentInChildren<Camera>().GetComponent<PostProcessVolume>();
        m_ColorGrading = m_PPVolume.profile.GetSetting<ColorGrading>();
        m_Vignette = m_PPVolume.profile.GetSetting<Vignette>();
    }

    public void VignetteSmoothness(float value, float damp)
    {
        m_Vignette.smoothness.value = Mathf.Lerp(m_Vignette.smoothness.value, value, 1 - Mathf.Exp(-damp * Time.unscaledDeltaTime));
    }

    public IEnumerator VignetteSmoothness(float from, float to, float duration)
    {
        float elapsed = 0;

        while(elapsed < duration)
        {
            m_Vignette.smoothness.value = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }

        m_Vignette.smoothness.value = to;
    }

    public void ResetVignette()
    {
        m_Vignette.smoothness.value = 0.0f;
    }

    public void SaturateColorGrading(float value, float damp)
    {
        m_ColorGrading.saturation.value = Mathf.Lerp(m_ColorGrading.saturation.value, value, 1 - Mathf.Exp(-damp * Time.unscaledDeltaTime));
    }

    public void ResetColorGrading()
    {
        m_ColorGrading.saturation.value = 0.0f;
    }

    public void ResetAllEffects()
    {
        ResetVignette();
        ResetColorGrading();
    }
}
