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

    public MainSceneAction Action => action;
    public Button Button => button;

    public event Action<MainSceneAction> Clicked;

    private Color defaultBackgroundColor;
    private Color defaultTitleColor;

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

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    public void SetTexts(string title, string subtitle = null, string cost = null)
    {
        SetText(titleText, title);
        SetText(subtitleText, subtitle);
        SetText(costText, cost);
    }

    public void SetRedDot(bool visible)
    {
        if (redDot != null)
        {
            redDot.SetVisible(visible);
        }
    }

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

    private void OnButtonClick()
    {
        Clicked?.Invoke(action);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value ?? string.Empty;
    }
}
