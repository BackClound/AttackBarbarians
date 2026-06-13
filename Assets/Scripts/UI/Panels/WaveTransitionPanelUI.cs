using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 波次过渡横幅：显示下一波编号与倒计时提示。
/// </summary>
public class WaveTransitionPanelUI : UiPanelBase
{
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image hazardStripe;

    private int pendingWaveIndex = 1;

    /// <summary>设置即将开始的波次编号并刷新横幅文案。</summary>
    /// <param name="waveIndex">波次序号（从 1 起）。</param>
    public void SetWaveIndex(int waveIndex)
    {
        pendingWaveIndex = Mathf.Max(1, waveIndex);
        RefreshTexts();
    }

    /// <summary>显示时刷新波次文案并应用警示条纹配色。</summary>
    protected override void OnShow()
    {
        RefreshTexts();
        if (hazardStripe != null)
        {
            hazardStripe.color = UiTechWastelandPalette.HazardYellow;
        }
    }

    /// <summary>刷新波次横幅与副标题文案。</summary>
    private void RefreshTexts()
    {
        if (bannerText != null)
        {
            bannerText.text = $"WAVE {pendingWaveIndex} INBOUND";
            bannerText.color = UiTechWastelandPalette.AccentAmber;
        }

        if (subtitleText != null)
        {
            subtitleText.text = "模块重组准备中…";
            subtitleText.color = UiTechWastelandPalette.TextSecondary;
        }
    }
}
