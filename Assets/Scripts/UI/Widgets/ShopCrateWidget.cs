using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 补给箱面板：单抽 / 十连 / 广告免费抽。
/// </summary>
public class ShopCrateWidget : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text panelIndexText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image crateImage;
    [SerializeField] private TMP_Text rewardsHintText;

    [Header("Pull Buttons")]
    [SerializeField] private Button singlePullButton;
    [SerializeField] private TMP_Text singleCostText;
    [SerializeField] private Button tenPullButton;
    [SerializeField] private TMP_Text tenCostText;
    [SerializeField] private Button adPullButton;
    [SerializeField] private TMP_Text adPullText;

    [Header("Config")]
    [SerializeField] private string singleItemConfigId;
    [SerializeField] private string tenItemConfigId;
    [SerializeField] private string adFreeConfigId;
    [SerializeField] private string previewPoolConfigId;
    [SerializeField] private string crateTitle;

    [Header("Preview")]
    [SerializeField] private Button previewButton;

    /// <summary>单抽商品配置 ID。</summary>
    public string SingleItemConfigId => singleItemConfigId;
    /// <summary>十连商品配置 ID。</summary>
    public string TenItemConfigId => tenItemConfigId;
    /// <summary>广告免费抽配置 ID。</summary>
    public string AdFreeConfigId => adFreeConfigId;
    /// <summary>奖池预览配置 ID。</summary>
    public string PreviewPoolConfigId => previewPoolConfigId;
    /// <summary>补给箱展示标题。</summary>
    public string CrateTitle => crateTitle;

    /// <summary>请求购买（单抽/十连）时触发。</summary>
    public event Action<string> PurchaseRequested;
    /// <summary>请求广告免费抽取时触发。</summary>
    public event Action<string> AdFreeRequested;
    /// <summary>请求打开奖池预览时触发。</summary>
    public event Action<string, string> PreviewRequested;

    /// <summary>绑定单抽、十连、广告与预览按钮。</summary>
    private void Awake()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.AddListener(OnSinglePullClicked);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.AddListener(OnTenPullClicked);
        }

        if (adPullButton != null)
        {
            adPullButton.onClick.AddListener(OnAdPullClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.AddListener(OnPreviewClicked);
        }
    }

    /// <summary>解绑所有按钮事件。</summary>
    private void OnDestroy()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.RemoveListener(OnSinglePullClicked);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.RemoveListener(OnTenPullClicked);
        }

        if (adPullButton != null)
        {
            adPullButton.onClick.RemoveListener(OnAdPullClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.RemoveListener(OnPreviewClicked);
        }
    }

    /// <summary>刷新面板序号、标题与奖励提示文案。</summary>
    /// <param name="panelIndex">区域序号。</param>
    /// <param name="title">补给箱名称。</param>
    /// <param name="rewardsHint">奖励类型提示。</param>
    public void SetDisplay(string panelIndex, string title, string rewardsHint)
    {
        SetText(panelIndexText, panelIndex);
        SetText(titleText, title);
        SetText(rewardsHintText, rewardsHint);
    }

    /// <summary>刷新单抽与十连消耗文案。</summary>
    /// <param name="singleCost">单抽价格文本。</param>
    /// <param name="tenCost">十连价格文本。</param>
    public void SetCosts(string singleCost, string tenCost)
    {
        SetText(singleCostText, singleCost);
        SetText(tenCostText, tenCost);
    }

    /// <summary>设置广告免费抽按钮可用性与标签。</summary>
    /// <param name="available">是否可点击。</param>
    /// <param name="label">按钮说明文案。</param>
    public void SetAdPullAvailable(bool available, string label)
    {
        if (adPullButton != null)
        {
            adPullButton.interactable = available;
        }

        SetText(adPullText, label);
    }

    /// <summary>设置单抽/十连按钮可交互状态。</summary>
    /// <param name="singleAvailable">单抽是否可用。</param>
    /// <param name="tenAvailable">十连是否可用。</param>
    public void SetPullButtonsInteractable(bool singleAvailable, bool tenAvailable)
    {
        if (singlePullButton != null)
        {
            singlePullButton.interactable = singleAvailable;
        }

        if (tenPullButton != null)
        {
            tenPullButton.interactable = tenAvailable;
        }
    }

    /// <summary>单抽按钮回调，抛出单抽配置 ID。</summary>
    private void OnSinglePullClicked()
    {
        if (!string.IsNullOrWhiteSpace(singleItemConfigId))
        {
            PurchaseRequested?.Invoke(singleItemConfigId);
        }
    }

    /// <summary>十连按钮回调，抛出十连配置 ID。</summary>
    private void OnTenPullClicked()
    {
        if (!string.IsNullOrWhiteSpace(tenItemConfigId))
        {
            PurchaseRequested?.Invoke(tenItemConfigId);
        }
    }

    /// <summary>广告免费抽按钮回调。</summary>
    private void OnAdPullClicked()
    {
        if (!string.IsNullOrWhiteSpace(adFreeConfigId))
        {
            AdFreeRequested?.Invoke(adFreeConfigId);
        }
    }

    /// <summary>预览按钮回调，抛出奖池 ID 与标题。</summary>
    private void OnPreviewClicked()
    {
        if (!string.IsNullOrWhiteSpace(previewPoolConfigId))
        {
            PreviewRequested?.Invoke(previewPoolConfigId, crateTitle);
        }
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
