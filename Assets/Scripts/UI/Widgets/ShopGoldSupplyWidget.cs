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

    public GoldSupplyTier LowTier => lowTier;
    public GoldSupplyTier StandardTier => standardTier;

    [Header("Ad")]
    [SerializeField] private Button adClaimButton;
    [SerializeField] private TMP_Text adClaimText;
    [SerializeField] private string adClaimConfigId;

    public string AdClaimConfigId => adClaimConfigId;

    public event Action<string> PurchaseRequested;
    public event Action<string> AdClaimRequested;

    private void Awake()
    {
        BindTier(lowTier);
        BindTier(standardTier);

        if (adClaimButton != null)
        {
            adClaimButton.onClick.AddListener(OnAdClaimClicked);
        }
    }

    private void OnDestroy()
    {
        UnbindTier(lowTier);
        UnbindTier(standardTier);

        if (adClaimButton != null)
        {
            adClaimButton.onClick.RemoveListener(OnAdClaimClicked);
        }
    }

    public void SetDisplay(string panelIndex, string title)
    {
        SetText(panelIndexText, panelIndex);
        SetText(titleText, title);
    }

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

    public void SetAdClaimAvailable(bool available, string label)
    {
        if (adClaimButton != null)
        {
            adClaimButton.interactable = available;
        }

        SetText(adClaimText, label);
    }

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

    private void OnAdClaimClicked()
    {
        if (!string.IsNullOrWhiteSpace(adClaimConfigId))
        {
            AdClaimRequested?.Invoke(adClaimConfigId);
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
