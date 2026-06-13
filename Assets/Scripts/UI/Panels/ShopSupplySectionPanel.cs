using System;
using UnityEngine;

/// <summary>
/// 商城补给区：普通箱、高级箱、金币补给。
/// </summary>
public class ShopSupplySectionPanel : MonoBehaviour
{
    [SerializeField] private ShopCrateWidget commonCrate;
    [SerializeField] private ShopCrateWidget premiumCrate;
    [SerializeField] private ShopGoldSupplyWidget goldSupply;

    /// <summary>补给区请求购买时向上层抛出配置 ID。</summary>
    public event Action<string> PurchaseRequested;
    /// <summary>补给区请求广告免费领取时向上层抛出配置 ID。</summary>
    public event Action<string> AdFreeRequested;
    /// <summary>补给区请求奖池预览时向上层抛出池 ID 与标题。</summary>
    public event Action<string, string> PreviewRequested;

    /// <summary>绑定补给箱与金币补给 Widget 事件。</summary>
    private void Awake()
    {
        WireCrate(commonCrate);
        WireCrate(premiumCrate);
        WireGoldSupply(goldSupply);
    }

    /// <summary>解绑补给区 Widget 事件。</summary>
    private void OnDestroy()
    {
        UnwireCrate(commonCrate);
        UnwireCrate(premiumCrate);
        UnwireGoldSupply(goldSupply);
    }

    /// <summary>从 <see cref="ShopManager"/> 刷新普通箱、高级箱与金币补给展示。</summary>
    public void Refresh()
    {
        RefreshCrate(
            commonCrate,
            "01",
            "普通补给箱",
            "技能升级卡 · 属性升级卡",
            GameConstants.ConfigIds.ShopCrateCommonSingle,
            GameConstants.ConfigIds.ShopCrateCommonTen,
            GameConstants.ConfigIds.ShopAdCrateCommon,
            UpgradeCardConstants.PoolIds.ShopCrateCommon);

        RefreshCrate(
            premiumCrate,
            "02",
            "高级补给箱",
            "高级技能卡 · 稀有升级卡",
            GameConstants.ConfigIds.ShopCratePremiumSingle,
            GameConstants.ConfigIds.ShopCratePremiumTen,
            GameConstants.ConfigIds.ShopAdCratePremium,
            UpgradeCardConstants.PoolIds.ShopCratePremium);

        RefreshGoldSupply();
    }

    /// <summary>刷新单个补给箱 Widget 的价格、可用性与广告状态。</summary>
    private void RefreshCrate(
        ShopCrateWidget crate,
        string panelIndex,
        string title,
        string rewardsHint,
        string singleId,
        string tenId,
        string adId,
        string previewPoolId)
    {
        if (crate == null)
        {
            return;
        }

        crate.SetDisplay(panelIndex, title, rewardsHint);

        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            return;
        }

        bool singleAvailable = shop.CanPurchase(singleId, out _, out _);
        bool tenAvailable = shop.CanPurchase(tenId, out _, out _);
        crate.SetPullButtonsInteractable(singleAvailable, tenAvailable);

        shop.TryGetItemDisplay(singleId, out string singleCost, out _);
        shop.TryGetItemDisplay(tenId, out string tenCost, out _);
        crate.SetCosts(singleCost, tenCost);

        ResolveAdSupplyState(
            shop,
            adId,
            "观看广告免费抽取",
            out bool adAvailable,
            out string adLabel);
        crate.SetAdPullAvailable(adAvailable, adLabel);
    }

    /// <summary>刷新金币补给区各档位与广告领取。</summary>
    private void RefreshGoldSupply()
    {
        if (goldSupply == null)
        {
            return;
        }

        goldSupply.SetDisplay("03", "金币补给");

        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            return;
        }

        RefreshTierDisplay(
            shop,
            goldSupply,
            goldSupply.LowTier,
            GameConstants.ConfigIds.ShopGoldSupplyLowSingle,
            GameConstants.ConfigIds.ShopGoldSupplyLowTen);

        RefreshTierDisplay(
            shop,
            goldSupply,
            goldSupply.StandardTier,
            GameConstants.ConfigIds.ShopGoldSupplyStdSingle,
            GameConstants.ConfigIds.ShopGoldSupplyStdTen);

        ResolveAdSupplyState(
            shop,
            GameConstants.ConfigIds.ShopAdGoldSupply,
            "广告领取",
            out bool adAvailable,
            out string adLabel);
        goldSupply.SetAdClaimAvailable(adAvailable, adLabel);
    }

    /// <summary>解析广告补给可用性与按钮标签。</summary>
    private static void ResolveAdSupplyState(
        ShopManager shop,
        string adConfigId,
        string availableLabel,
        out bool adAvailable,
        out string adLabel)
    {
        adAvailable = false;
        adLabel = string.Empty;

        if (ServiceLocator.TryGet(out AdRewardService adService) && adService.IsShowing)
        {
            adLabel = "广告播放中";
            return;
        }

        if (shop == null)
        {
            adLabel = "商店未就绪";
            return;
        }

        adAvailable = shop.CanClaimAdFreeSupply(adConfigId, out string failureReason);
        if (adAvailable)
        {
            int remaining = shop.GetRemainingAdFreeSupplyCount(adConfigId);
            int dailyLimit = shop.GetAdFreeSupplyDailyLimit();
            adLabel = $"{availableLabel} ({remaining}/{dailyLimit})";
        }
        else
        {
            adLabel = failureReason ?? "暂不可领取";
        }
    }

    /// <summary>刷新金币补给指定档位的价格与按钮。</summary>
    private static void RefreshTierDisplay(
        ShopManager shop,
        ShopGoldSupplyWidget widget,
        ShopGoldSupplyWidget.GoldSupplyTier tier,
        string singleId,
        string tenId)
    {
        if (widget == null || tier == null)
        {
            return;
        }

        shop.TryGetItemDisplay(singleId, out string singleCost, out _);
        shop.TryGetItemDisplay(tenId, out string tenCost, out _);
        bool singleAvailable = shop.CanPurchase(singleId, out _, out _);
        bool tenAvailable = shop.CanPurchase(tenId, out _, out _);
        widget.SetTierCosts(tier, singleCost, tenCost, singleAvailable, tenAvailable);
    }

    /// <summary>绑定补给箱 Widget 事件。</summary>
    private void WireCrate(ShopCrateWidget crate)
    {
        if (crate == null)
        {
            return;
        }

        crate.PurchaseRequested += OnPurchaseRequested;
        crate.AdFreeRequested += OnAdFreeRequested;
        crate.PreviewRequested += OnPreviewRequested;
    }

    /// <summary>解绑补给箱 Widget 事件。</summary>
    private void UnwireCrate(ShopCrateWidget crate)
    {
        if (crate == null)
        {
            return;
        }

        crate.PurchaseRequested -= OnPurchaseRequested;
        crate.AdFreeRequested -= OnAdFreeRequested;
        crate.PreviewRequested -= OnPreviewRequested;
    }

    /// <summary>绑定金币补给 Widget 事件。</summary>
    private void WireGoldSupply(ShopGoldSupplyWidget widget)
    {
        if (widget == null)
        {
            return;
        }

        widget.PurchaseRequested += OnPurchaseRequested;
        widget.AdClaimRequested += OnAdFreeRequested;
    }

    /// <summary>解绑金币补给 Widget 事件。</summary>
    private void UnwireGoldSupply(ShopGoldSupplyWidget widget)
    {
        if (widget == null)
        {
            return;
        }

        widget.PurchaseRequested -= OnPurchaseRequested;
        widget.AdClaimRequested -= OnAdFreeRequested;
    }

    /// <summary>转发补给箱/金币购买请求。</summary>
    private void OnPurchaseRequested(string configId)
    {
        PurchaseRequested?.Invoke(configId);
    }

    /// <summary>转发广告免费领取请求。</summary>
    private void OnAdFreeRequested(string configId)
    {
        AdFreeRequested?.Invoke(configId);
    }

    /// <summary>转发奖池预览请求。</summary>
    private void OnPreviewRequested(string poolConfigId, string crateTitle)
    {
        PreviewRequested?.Invoke(poolConfigId, crateTitle);
    }
}
