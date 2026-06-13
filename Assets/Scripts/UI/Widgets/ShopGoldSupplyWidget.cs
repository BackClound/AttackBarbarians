using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金币补给面板：低级 / 标准两档补给与广告领取。
/// </summary>
public class ShopGoldSupplyWidget : MonoBehaviour
{
    [Serializable]
    /// <summary>金币补给档位配置：标题、商品 ID 与按钮引用。</summary>
    public class GoldSupplyTier
    {
        public string title;
        public string singleItemConfigId;
        public string tenItemConfigId;
        public Button singlePullButton;
        public TMP_Text singleCostText;
        public Button tenPullButton;
        public TMP_Text tenCostText;
    }

    [Header("Display")]
    [SerializeField] private TMP_Text panelIndexText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GoldSupplyTier lowTier;
    [SerializeField] private GoldSupplyTier standardTier;

    /// <summary>低级金币补给档位配置。</summary>
    public GoldSupplyTier LowTier => lowTier;
    /// <summary>标准金币补给档位。</summary>
    public GoldSupplyTier StandardTier => standardTier;

    [Header("Ad")]
    [SerializeField] private Button adClaimButton;
    [SerializeField] private TMP_Text adClaimText;
    [SerializeField] private string adClaimConfigId;

    /// <summary>广告领取金币的配置 ID。</summary>
    public string AdClaimConfigId => adClaimConfigId;

    /// <summary>请求购买金币补给时触发。</summary>
    public event Action<string> PurchaseRequested;
    /// <summary>请求广告领取金币时触发。</summary>
    public event Action<string> AdClaimRequested;

    /// <summary>绑定各档位购买按钮与广告领取按钮。</summary>
    private void Awake()
    {
        BindTier(lowTier);
        BindTier(standardTier);

        if (adClaimButton != null)
        {
            adClaimButton.onClick.AddListener(OnAdClaimClicked);
        }
    }

    /// <summary>解绑所有按钮事件。</summary>
    private void OnDestroy()
    {
        UnbindTier(lowTier);
        UnbindTier(standardTier);

        if (adClaimButton != null)
        {
            adClaimButton.onClick.RemoveListener(OnAdClaimClicked);
        }
    }

    /// <summary>刷新面板序号与标题。</summary>
    /// <param name="panelIndex">区域序号。</param>
    /// <param name="title">补给区标题。</param>
    public void SetDisplay(string panelIndex, string title)
    {
        SetText(panelIndexText, panelIndex);
        SetText(titleText, title);
    }

    /// <summary>刷新指定档位的单抽/十连价格与可用性。</summary>
    /// <param name="tier">目标档位。</param>
    /// <param name="singleCost">单抽价格文本。</param>
    /// <param name="tenCost">十连价格文本。</param>
    /// <param name="singleAvailable">单抽是否可用。</param>
    /// <param name="tenAvailable">十连是否可用。</param>
    public void SetTierCosts(GoldSupplyTier tier, string singleCost, string tenCost, bool singleAvailable, bool tenAvailable)
    {
        SetText(tier?.singleCostText, singleCost);
        SetText(tier?.tenCostText, tenCost);

        if (tier?.singlePullButton != null)
        {
            tier.singlePullButton.interactable = singleAvailable;
        }

        if (tier?.tenPullButton != null)
        {
            tier.tenPullButton.interactable = tenAvailable;
        }
    }

    /// <summary>设置广告领取按钮可用性与标签。</summary>
    /// <param name="available">是否可点击。</param>
    /// <param name="label">按钮说明文案。</param>
    public void SetAdClaimAvailable(bool available, string label)
    {
        if (adClaimButton != null)
        {
            adClaimButton.interactable = available;
        }

        SetText(adClaimText, label);
    }

    /// <summary>绑定档位内单抽/十连按钮。</summary>
    private void BindTier(GoldSupplyTier tier)
    {
        if (tier == null)
        {
            return;
        }

        if (tier.singlePullButton != null && !string.IsNullOrWhiteSpace(tier.singleItemConfigId))
        {
            string capturedId = tier.singleItemConfigId;
            tier.singlePullButton.onClick.AddListener(() => PurchaseRequested?.Invoke(capturedId));
        }

        if (tier.tenPullButton != null && !string.IsNullOrWhiteSpace(tier.tenItemConfigId))
        {
            string capturedId = tier.tenItemConfigId;
            tier.tenPullButton.onClick.AddListener(() => PurchaseRequested?.Invoke(capturedId));
        }
    }

    /// <summary>解绑档位按钮监听。</summary>
    private static void UnbindTier(GoldSupplyTier tier)
    {
        if (tier == null)
        {
            return;
        }

        if (tier.singlePullButton != null)
        {
            tier.singlePullButton.onClick.RemoveAllListeners();
        }

        if (tier.tenPullButton != null)
        {
            tier.tenPullButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>广告领取按钮回调。</summary>
    private void OnAdClaimClicked()
    {
        if (!string.IsNullOrWhiteSpace(adClaimConfigId))
        {
            AdClaimRequested?.Invoke(adClaimConfigId);
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
