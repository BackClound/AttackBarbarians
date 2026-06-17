using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD 编排器：组合顶栏、城墙区、技能栏子 Panel。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。常显层，由 <see cref="UIManager"/> 在 Playing 状态启用。</para>
/// <para><b>布局：</b>中央留空给玩法区（见科技废土视觉规范）。</para>
/// </remarks>
public class GameplayHudPresenter : MonoBehaviour
{
    [SerializeField] private GameplayHudTopPanel topPanel;
    [SerializeField] private GameplayHudWallPanel wallPanel;
    [SerializeField] private GameplayHudSkillRailPanel skillRailPanel;

    [Header("Legacy (optional backward compat)")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private Button pauseButton;

    /// <summary>本局累计击杀数，供结算面板读取。</summary>
    public int SessionKillCount => topPanel != null ? topPanel.SessionKillCount : 0;

    /// <summary>由 <see cref="UIManager"/> 控制 HUD 整体显隐。</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>刷新所有子 Panel。</summary>
    public void RefreshAll()
    {
        topPanel?.RefreshAll();
        wallPanel?.RefreshAll();
        skillRailPanel?.RefreshAll();
    }
}
