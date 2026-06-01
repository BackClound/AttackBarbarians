using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 奖励状态卡片：标题、倒计时/计时、状态文案、红点。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。主场景奖励行每个槽位。</para>
/// </remarks>
public class GeneralRewardCardPanel : MonoBehaviour
{
    [SerializeField] private MainSceneAction action;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UI_RedDot redDot;

    public MainSceneAction Action => action;

    public event Action<MainSceneAction> Clicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    public void SetDisplay(string title, string timer, string status)
    {
        SetText(titleText, title);
        SetText(timerText, timer);
        SetText(statusText, status);
    }

    public void SetRedDot(bool visible)
    {
        if (redDot != null)
        {
            redDot.SetVisible(visible);
        }
    }

    private void OnButtonClick()
    {
        Clicked?.Invoke(action);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
