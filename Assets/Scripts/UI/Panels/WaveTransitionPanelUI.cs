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

    public void SetWaveIndex(int waveIndex)
    {
        pendingWaveIndex = Mathf.Max(1, waveIndex);
        RefreshTexts();
    }

    protected override void OnShow()
    {
        RefreshTexts();
        if (hazardStripe != null)
        {
            hazardStripe.color = UiTechWastelandPalette.HazardYellow;
        }
    }

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
