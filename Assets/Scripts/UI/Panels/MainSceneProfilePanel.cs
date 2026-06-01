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

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
