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

    public event Action<string> PurchaseRequested;
    public event Action<string> AdFreeRequested;

    private void Awake()
    {
        WireCrate(commonCrate);
        WireCrate(premiumCrate);
        WireGoldSupply(goldSupply);
    }

    private void OnDestroy()
    {
        UnwireCrate(commonCrate);
        UnwireCrate(premiumCrate);
        UnwireGoldSupply(goldSupply);
    }

    public void Refresh()
    {
        RefreshCrate(
            commonCrate,
            "01",
            "普通补给箱",
            "技能升级卡 · 普通装备",
            GameConstants.ConfigIds.ShopCrateCommonSingle,
            GameConstants.ConfigIds.ShopCrateCommonTen,
            GameConstants.ConfigIds.ShopAdCrateCommon);

        RefreshCrate(
            premiumCrate,
            "02",
            "高级补给箱",
            "高级技能卡 · 稀有/史诗装备",
            GameConstants.ConfigIds.ShopCratePremiumSingle,
            GameConstants.ConfigIds.ShopCratePremiumTen,
            GameConstants.ConfigIds.ShopAdCratePremium);

        RefreshGoldSupply();
    }

    private void RefreshCrate(
        ShopCrateWidget crate,
        string panelIndex,
        string title,
        string rewardsHint,
        string singleId,
        string tenId,
        string adId)
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
        adLabel = adAvailable ? availableLabel : failureReason ?? "暂不可领取";
    }

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

    private void WireCrate(ShopCrateWidget crate)
    {
        if (crate == null)
        {
            return;
        }

        crate.PurchaseRequested += OnPurchaseRequested;
        crate.AdFreeRequested += OnAdFreeRequested;
    }

    private void UnwireCrate(ShopCrateWidget crate)
    {
        if (crate == null)
        {
            return;
        }

        crate.PurchaseRequested -= OnPurchaseRequested;
        crate.AdFreeRequested -= OnAdFreeRequested;
    }

    private void WireGoldSupply(ShopGoldSupplyWidget widget)
    {
        if (widget == null)
        {
            return;
        }

        widget.PurchaseRequested += OnPurchaseRequested;
        widget.AdClaimRequested += OnAdFreeRequested;
    }

    private void UnwireGoldSupply(ShopGoldSupplyWidget widget)
    {
        if (widget == null)
        {
            return;
        }

        widget.PurchaseRequested -= OnPurchaseRequested;
        widget.AdClaimRequested -= OnAdFreeRequested;
    }

    private void OnPurchaseRequested(string configId)
    {
        PurchaseRequested?.Invoke(configId);
    }

    private void OnAdFreeRequested(string configId)
    {
        AdFreeRequested?.Invoke(configId);
    }
}
