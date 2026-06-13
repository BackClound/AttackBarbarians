using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainScene 左上账号区：头像、昵称、VIP、等级、经验条。
/// </summary>
public class MainSceneProfilePanel : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text vipText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expText;

    [Header("Fallback")]
    [SerializeField] private string playerNameFallback = "涛王不可";
    [SerializeField] private string vipFallback = "VIP6";
    [SerializeField] private int playerLevelFallback = 60;
    [SerializeField] private int currentExpFallback = 4560;
    [SerializeField] private int requiredExpFallback = 9800;

    /// <summary>刷新头像区昵称、VIP、等级与经验条展示。</summary>
    /// <summary>
    /// 刷新账号区显示：昵称、VIP、等级与经验条。
    /// </summary>
    public void Refresh()
    {
        SetText(playerNameText, playerNameFallback);
        SetText(vipText, vipFallback);
        SetText(playerLevelText, playerLevelFallback.ToString(CultureInfo.InvariantCulture));
        SetText(expText, $"EXP {currentExpFallback} / {requiredExpFallback}");

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = Mathf.Max(1, requiredExpFallback);
            expSlider.value = Mathf.Clamp(currentExpFallback, 0, requiredExpFallback);
        }
    }

    /// <summary>安全写入 TMP 文本。</summary>
    /// <summary>
    /// 安全设置 TMP 文本（组件为 null 时跳过）。
    /// </summary>
    /// <param name="text">目标文本组件。</param>
    /// <param name="value">要显示的字符串。</param>
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
