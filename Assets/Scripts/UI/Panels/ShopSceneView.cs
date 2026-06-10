using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城页面 Presenter：编排资源条、补给区、广告券兑换与状态提示。
/// </summary>
/// <remarks>
/// <para><b>层级：</b><c>ShopPageRoot</c> → <c>PageBackground</c> + <c>SafeAreaRoot</c>（交互 UI，可滚动区仅在 ShopScrollHost 内）。</para>
/// <para>BottomNav 挂在 <c>MainSceneUI</c> 下，不随本页 ScrollView 滚动。</para>
/// </remarks>
public class ShopSceneView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Image backgroundImage;

    [Header("Sub Panels")]
    [SerializeField] private ShopResourcePanel resourcePanel;
    [SerializeField] private ShopSupplySectionPanel supplySection;
    [SerializeField] private ShopAdTicketExchangePanel exchangePanel;
    [SerializeField] private TMP_Text statusText;

    [Header("Crate Popups")]
    [SerializeField] private ShopCrateRewardPopupPanel crateRewardPopup;
    [SerializeField] private ShopCratePoolPreviewPanel cratePoolPreview;

    private bool isSubscribed;
    private string pendingCratePurchaseId;
    private string pendingPoolConfigId;
    private string pendingCrateTitle;

    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (!isSubscribed)
        {
            return;
        }

        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeShopPurchased(OnShopPurchased);
        GameEvents.UnsubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        GameEvents.UnsubscribeAdRewardCompleted(OnAdRewardCompleted);
        GameEvents.UnsubscribeAdRewardFailed(OnAdRewardFailed);
        GameEvents.UnsubscribeAdRewardStateChanged(OnAdRewardStateChanged);
        GameEvents.UnsubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = false;
    }

    private void Awake()
    {
        if (resourcePanel != null)
        {
            resourcePanel.AddClicked += OnResourceAddClicked;
        }

        if (supplySection != null)
        {
            supplySection.PurchaseRequested += OnPurchaseRequested;
            supplySection.AdFreeRequested += OnAdFreeRequested;
            supplySection.PreviewRequested += OnPreviewRequested;
        }

        if (exchangePanel != null)
        {
            exchangePanel.ExchangeRequested += OnExchangeRequested;
        }

        if (crateRewardPopup != null)
        {
            crateRewardPopup.BindCallbacks(OnPreviewRequested, OnPurchaseRequested);
        }
    }

    private void OnDestroy()
    {
        if (resourcePanel != null)
        {
            resourcePanel.AddClicked -= OnResourceAddClicked;
        }

        if (supplySection != null)
        {
            supplySection.PurchaseRequested -= OnPurchaseRequested;
            supplySection.AdFreeRequested -= OnAdFreeRequested;
            supplySection.PreviewRequested -= OnPreviewRequested;
        }

        if (exchangePanel != null)
        {
            exchangePanel.ExchangeRequested -= OnExchangeRequested;
        }
    }

    public void RefreshAll()
    {
        resourcePanel?.Refresh();
        supplySection?.Refresh();
        exchangePanel?.Refresh();
    }

    private void TrySubscribeEvents()
    {
        if (isSubscribed || !ServiceLocator.TryGet(out EventBus _))
        {
            return;
        }

        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeShopPurchased(OnShopPurchased);
        GameEvents.SubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        GameEvents.SubscribeAdRewardCompleted(OnAdRewardCompleted);
        GameEvents.SubscribeAdRewardFailed(OnAdRewardFailed);
        GameEvents.SubscribeAdRewardStateChanged(OnAdRewardStateChanged);
        GameEvents.SubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = true;
    }

    private void OnResourceAddClicked(CurrencyType currency)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (currency == CurrencyType.AdTicket || currency == CurrencyType.Stamina)
        {
            if (ServiceLocator.TryGet(out AdRewardService adService))
            {
                adService.TryShowRewardedForAdTicket();
            }
            else
            {
                SetStatus("广告服务未就绪");
            }

            return;
        }

        string message = currency switch
        {
            CurrencyType.Diamond => "水晶补充入口待接入",
            CurrencyType.Gold => "金币补充入口待接入",
            _ => "补充入口待接入",
        };
        SetStatus(message);
    }

    private void OnPurchaseRequested(string configId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            SetStatus("商店未就绪");
            return;
        }

        if (IsCratePurchase(configId))
        {
            RememberPendingCrateContext(configId);
        }

        shop.TryPurchase(configId);
    }

    private void OnAdFreeRequested(string configId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        if (!ServiceLocator.TryGet(out AdRewardService adService))
        {
            SetStatus("广告服务未就绪");
            return;
        }

        if (IsCratePurchase(configId))
        {
            RememberPendingCrateContext(configId);
        }

        SetStatus("正在加载广告…");
        adService.TryShowRewardedForShop(configId);
    }

    private void OnPreviewRequested(string poolConfigId, string crateTitle)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (cratePoolPreview == null)
        {
            SetStatus("预览面板未绑定");
            return;
        }

        cratePoolPreview.Show(poolConfigId, crateTitle);
    }

    private void OnExchangeRequested(string configId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            SetStatus("商店未就绪");
            return;
        }

        shop.TryExchangeAdTickets(configId);
    }

    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    private void OnShopPurchased(GameEventContext ctx)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();
        if (ctx.Payload is ShopPurchaseEventArgs args && !IsCratePurchase(args.ItemConfigId))
        {
            SetStatus($"获得 {args.RewardAmount} {FormatReward(args.RewardType)}");
        }
    }

    private void OnUpgradeCardGranted(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeCardGrantedEventArgs args ||
            args.Source != UpgradeCardRewardSource.ShopCrate)
        {
            return;
        }

        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();

        if (crateRewardPopup != null)
        {
            crateRewardPopup.Show(
                args,
                pendingCratePurchaseId,
                pendingPoolConfigId,
                pendingCrateTitle);
        }

        SetStatus($"获得 {args.Grants?.Count ?? 0} 张升级卡");
    }

    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        RefreshAll();
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    private void OnAdRewardCompleted(GameEventContext ctx)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();
        if (ctx.Payload is not AdRewardCompletedEventArgs args)
        {
            return;
        }

        if (args.Source == AdRewardSource.Shop && IsCratePurchase(pendingCratePurchaseId))
        {
            return;
        }

        SetStatus(args.Source switch
        {
            AdRewardSource.Shop => BuildStandardAdRewardStatus("补给奖励"),
            AdRewardSource.AdTicket => BuildStandardAdRewardStatus(null),
            _ => BuildStandardAdRewardStatus(null),
        });
    }

    private static string BuildStandardAdRewardStatus(string prefix)
    {
        int ticketAmount = AdTicketConstants.DefaultRewardPerAd;
        if (ServiceLocator.TryGet(out AdRewardService adService) && adService.Config != null)
        {
            ticketAmount = adService.Config.RewardAdTicketAmount;
        }

        string rewardText = $"+{ticketAmount} 广告券，体力已回满";
        return string.IsNullOrEmpty(prefix) ? $"获得 {rewardText}" : $"获得{prefix}、{rewardText}";
    }

    private void OnAdRewardFailed(GameEventContext ctx)
    {
        RefreshAll();
        if (ctx.Payload is AdRewardFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    private void OnAdRewardStateChanged(GameEventContext ctx) => RefreshAll();

    private void RememberPendingCrateContext(string configId)
    {
        pendingCratePurchaseId = configId;
        pendingPoolConfigId = ResolvePoolConfigId(configId);
        pendingCrateTitle = ResolveCrateTitle(configId);
    }

    private static bool IsCratePurchase(string configId) =>
        !string.IsNullOrWhiteSpace(configId) &&
        (configId.Contains("crate") || configId.Contains("ad_crate"));

    private static string ResolvePoolConfigId(string configId) =>
        configId != null && configId.Contains("premium")
            ? UpgradeCardConstants.PoolIds.ShopCratePremium
            : UpgradeCardConstants.PoolIds.ShopCrateCommon;

    private static string ResolveCrateTitle(string configId) =>
        configId != null && configId.Contains("premium") ? "高级补给箱" : "普通补给箱";

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message ?? string.Empty;
        statusText.color = string.IsNullOrEmpty(message)
            ? UiTechWastelandPalette.TextSecondary
            : UiTechWastelandPalette.HazardYellow;
    }

    private void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }

    private static string FormatReward(ShopRewardType reward) =>
        reward switch
        {
            ShopRewardType.Gold => "金币",
            ShopRewardType.Diamond => "水晶",
            ShopRewardType.Energy => "广告券",
            ShopRewardType.AdTicket => "广告券",
            ShopRewardType.TechPoint => "科技点",
            ShopRewardType.UpgradeCard => "升级卡",
            _ => reward.ToString(),
        };
}
