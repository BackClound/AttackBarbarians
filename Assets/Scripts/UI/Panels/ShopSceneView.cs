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

    /// <summary>订阅商城相关事件并刷新全页。</summary>
    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    /// <summary>取消商城事件订阅。</summary>
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

    /// <summary>绑定资源条、补给区、兑换区与弹窗回调。</summary>
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

    /// <summary>解绑各子 Panel 事件。</summary>
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

    /// <summary>刷新资源条、补给区与广告券兑换区。</summary>
    public void RefreshAll()
    {
        resourcePanel?.Refresh();
        supplySection?.Refresh();
        exchangePanel?.Refresh();
    }

    /// <summary>延迟订阅商城相关 GameEvents。</summary>
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

    /// <summary>处理资源加号点击。</summary>
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

    /// <summary>转发补给购买请求到 ShopManager。</summary>
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

    /// <summary>转发广告免费补给请求。</summary>
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

    /// <summary>打开奖池预览弹窗。</summary>
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

    /// <summary>转发广告券兑换请求。</summary>
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

    /// <summary>资源变化时全页刷新。</summary>
    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>购买成功后刷新并更新状态栏。</summary>
    private void OnShopPurchased(GameEventContext ctx)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();
        if (ctx.Payload is ShopPurchaseEventArgs args && !IsCratePurchase(args.ItemConfigId))
        {
            SetStatus($"获得 {args.RewardAmount} {FormatReward(args.RewardType)}");
        }
    }

    /// <summary>补给箱升级卡发放后展示结果弹窗。</summary>
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

    /// <summary>购买失败后刷新并显示错误。</summary>
    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        RefreshAll();
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    /// <summary>广告奖励完成后刷新并提示。</summary>
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

    /// <summary>构建广告券奖励状态栏文案。</summary>
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

    /// <summary>广告失败后刷新并显示错误。</summary>
    private void OnAdRewardFailed(GameEventContext ctx)
    {
        RefreshAll();
        if (ctx.Payload is AdRewardFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    /// <summary>广告状态变化时刷新页面。</summary>
    private void OnAdRewardStateChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>记录待展示的补给箱上下文供弹窗使用。</summary>
    private void RememberPendingCrateContext(string configId)
    {
        pendingCratePurchaseId = configId;
        pendingPoolConfigId = ResolvePoolConfigId(configId);
        pendingCrateTitle = ResolveCrateTitle(configId);
    }

    /// <summary>判断配置 ID 是否为补给箱购买。</summary>
    private static bool IsCratePurchase(string configId) =>
        !string.IsNullOrWhiteSpace(configId) &&
        (configId.Contains("crate") || configId.Contains("ad_crate"));

    /// <summary>根据购买 ID 解析奖池配置 ID。</summary>
    private static string ResolvePoolConfigId(string configId) =>
        configId != null && configId.Contains("premium")
            ? UpgradeCardConstants.PoolIds.ShopCratePremium
            : UpgradeCardConstants.PoolIds.ShopCrateCommon;

    /// <summary>根据购买 ID 解析补给箱展示标题。</summary>
    private static string ResolveCrateTitle(string configId) =>
        configId != null && configId.Contains("premium") ? "高级补给箱" : "普通补给箱";

    /// <summary>更新商城底部状态栏文案与颜色。</summary>
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

    /// <summary>播放 UI 音效。</summary>
    private void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }

    /// <summary>将商店奖励类型格式化为中文名称。</summary>
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
