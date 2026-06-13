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

    /// <summary>本奖励卡片绑定的动作标识。</summary>
    public MainSceneAction Action => action;

    /// <summary>奖励卡片被点击时触发。</summary>
    public event Action<MainSceneAction> Clicked;

    /// <summary>绑定按钮点击事件。</summary>
    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    /// <summary>解绑按钮点击事件。</summary>
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>刷新奖励标题、计时与状态文案。</summary>
    /// <param name="title">奖励名称。</param>
    /// <param name="timer">倒计时或累计时长文本。</param>
    /// <param name="status">领取状态（可领取/累计中等）。</param>
    public void SetDisplay(string title, string timer, string status)
    {
        SetText(titleText, title);
        SetText(timerText, timer);
        SetText(statusText, status);
    }

    /// <summary>根据是否可领取控制红点显隐。</summary>
    /// <param name="visible">为 <c>true</c> 时显示红点。</param>
    public void SetRedDot(bool visible)
    {
        if (redDot != null)
        {
            redDot.SetVisible(visible);
        }
    }

    /// <summary>按钮点击回调，向上层抛出奖励动作。</summary>
    private void OnButtonClick()
    {
        Clicked?.Invoke(action);
    }

    /// <summary>安全写入 TMP 文本。</summary>
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
