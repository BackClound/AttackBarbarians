using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用图标按钮卡片：背景、图标、标题、副标题、消耗、红点。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。侧边栏、促销、底栏、系统入口等 Prefab 根物体。</para>
/// </remarks>
public class GeneralCardPanel : MonoBehaviour
{
    [SerializeField] private MainSceneAction action;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private UI_RedDot redDot;

    /// <summary>本卡片绑定的页面内动作标识。</summary>
    public MainSceneAction Action => action;
    /// <summary>卡片可点击按钮引用。</summary>
    public Button Button => button;

    /// <summary>卡片被点击时触发，携带 <see cref="MainSceneAction"/>。</summary>
    public event Action<MainSceneAction> Clicked;

    private Color defaultBackgroundColor;
    private Color defaultTitleColor;

    /// <summary>缓存默认配色并绑定按钮点击。</summary>
    private void Awake()
    {
        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }

        if (titleText != null)
        {
            defaultTitleColor = titleText.color;
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    /// <summary>解绑按钮点击。</summary>
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>批量设置标题、副标题与消耗文案。</summary>
    /// <param name="title">主标题。</param>
    /// <param name="subtitle">副标题，可选。</param>
    /// <param name="cost">消耗说明，可选。</param>
    public void SetTexts(string title, string subtitle = null, string cost = null)
    {
        SetText(titleText, title);
        SetText(subtitleText, subtitle);
        SetText(costText, cost);
    }

    /// <summary>设置红点可见性，用于可领取/新内容提示。</summary>
    /// <param name="visible">为 <c>true</c> 时显示红点。</param>
    public void SetRedDot(bool visible)
    {
        if (redDot != null)
        {
            redDot.SetVisible(visible);
        }
    }

    /// <summary>设置底栏/导航选中高亮态。</summary>
    /// <param name="selected">为 <c>true</c> 时应用选中配色。</param>
    public void SetSelectedHighlight(bool selected)
    {
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = selected
            ? Color.Lerp(defaultBackgroundColor, UiTechWastelandPalette.PrimaryCyan, 0.4f)
            : defaultBackgroundColor;

        if (titleText != null)
        {
            titleText.color = selected ? UiTechWastelandPalette.TextPrimary : defaultTitleColor;
        }
    }

    /// <summary>按钮点击回调，向上层抛出动作。</summary>
    private void OnButtonClick()
    {
        Clicked?.Invoke(action);
    }

    /// <summary>安全写入 TMP 文本，空值写为空串。</summary>
    private static void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value ?? string.Empty;
    }
}
