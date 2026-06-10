using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店管理：购买校验、扣费、发奖、限购与免费钻石领取。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 ResourceManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c>。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ShopManager&gt;()</c>。</para>
/// </remarks>
public class ShopManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private ShopCatalogSO catalog;

    private SaveManager saveManager;
    private ResourceManager resourceManager;
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public ShopCatalogSO Catalog => catalog;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (catalog == null)
        {
            catalog = Resources.Load<ShopCatalogSO>(GameConstants.ResourcePaths.ShopCatalog);
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out resourceManager);
        ServiceLocator.TryGet(out configManager);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        isInitialized = false;
    }

    public IReadOnlyList<ShopItemSO> GetCatalogItems()
    {
        return catalog != null ? catalog.Items : Array.Empty<ShopItemSO>();
    }

    public int GetEffectivePurchaseCount(string configId, ShopRefreshPeriod refreshPeriod)
    {
        if (saveManager?.Current == null || string.IsNullOrWhiteSpace(configId))
        {
            return 0;
        }

        SaveData save = saveManager.Current;
        if (refreshPeriod == ShopRefreshPeriod.Daily)
        {
            long lastTicks = ConfigIdLongPairListUtility.GetValue(save.shopLastPurchaseUtcTicks, configId);
            if (lastTicks > 0)
            {
                DateTime lastUtc = new DateTime(lastTicks, DateTimeKind.Utc);
                if (lastUtc.Date < DateTime.UtcNow.Date)
                {
                    return 0;
                }
            }
        }

        return ConfigIdIntPairListUtility.GetValue(save.shopPurchaseCounts, configId);
    }

    public int GetRemainingPurchases(ShopItemSO item)
    {
        if (item == null || !item.HasPurchaseLimit)
        {
            return int.MaxValue;
        }

        int used = GetEffectivePurchaseCount(item.ConfigId, item.RefreshPeriod);
        return Mathf.Max(0, item.PurchaseLimit - used);
    }

    public bool CanPurchase(string configId, out string failureReason, out ShopPurchaseFailedReason reason)
    {
        failureReason = null;
        reason = ShopPurchaseFailedReason.None;

        if (!isInitialized || saveManager?.Current == null || resourceManager == null)
        {
            failureReason = "商店未就绪";
            reason = ShopPurchaseFailedReason.NotInitialized;
            return false;
        }

        if (!TryResolveItem(configId, out ShopItemSO item))
        {
            failureReason = $"未找到商品: {configId}";
            reason = ShopPurchaseFailedReason.ItemNotFound;
            return false;
        }

        bool hasUpgradeCardReward = item.RewardType == ShopRewardType.UpgradeCard ||
            (!string.IsNullOrWhiteSpace(item.RewardConfigId) && IsCrateItem(item.ConfigId));
        if (item.RewardAmount <= 0 && !hasUpgradeCardReward)
        {
            failureReason = "商品奖励配置无效";
            reason = ShopPurchaseFailedReason.InvalidConfiguration;
            return false;
        }

        if (item.HasPurchaseLimit && GetRemainingPurchases(item) <= 0)
        {
            failureReason = "已达购买上限";
            reason = ShopPurchaseFailedReason.PurchaseLimitReached;
            return false;
        }

        if (item.PriceAmount > 0 && !resourceManager.CanAfford(item.PriceCurrency, item.PriceAmount))
        {
            failureReason = ResourceManager.FormatInsufficientFunds(
                item.PriceCurrency,
                item.PriceAmount,
                resourceManager.GetAmount(item.PriceCurrency));
            reason = ShopPurchaseFailedReason.InsufficientFunds;
            return false;
        }

        return true;
    }

    public bool TryPurchase(string configId)
    {
        if (!CanPurchase(configId, out string failureReason, out ShopPurchaseFailedReason reason))
        {
            LogFailure(configId, failureReason, reason);
            GameEvents.RaiseShopPurchaseFailed(this, configId, reason, failureReason);
            return false;
        }

        ShopItemSO item = null;
        TryResolveItem(configId, out item);

        if (item.PriceAmount > 0 &&
            !resourceManager.TrySpend(
                item.PriceCurrency,
                item.PriceAmount,
                ResourceChangeReason.ShopPurchase,
                out failureReason))
        {
            LogFailure(configId, failureReason, ShopPurchaseFailedReason.InsufficientFunds);
            GameEvents.RaiseShopPurchaseFailed(this, configId, ShopPurchaseFailedReason.InsufficientFunds, failureReason);
            return false;
        }

        if (!GrantReward(item))
        {
            failureReason = "发放奖励失败";
            GameEvents.RaiseShopPurchaseFailed(
                this,
                configId,
                ShopPurchaseFailedReason.InvalidConfiguration,
                failureReason);
            return false;
        }

        RecordPurchase(item);

        GameEvents.RaiseShopPurchased(
            this,
            new ShopPurchaseEventArgs(
                item.ConfigId,
                item.RewardType,
                item.RewardAmount,
                item.PriceCurrency,
                item.PriceAmount));

        if (configManager != null && configManager.ShouldLog())
        {
            Debug.Log($"[ShopManager] 购买成功 item={item.ConfigId} reward={item.RewardType}x{item.RewardAmount}");
        }

        return true;
    }

    public bool CanClaimFreeDiamond(out string failureReason, out TimeSpan cooldownRemaining)
    {
        failureReason = null;
        cooldownRemaining = TimeSpan.Zero;

        if (!isInitialized || saveManager?.Current == null || catalog == null)
        {
            failureReason = "商店未就绪";
            return false;
        }

        if (!TryGetFreeDiamondCooldownRemaining(out cooldownRemaining))
        {
            return true;
        }

        failureReason = $"免费钻石冷却中（剩余 {FormatCooldown(cooldownRemaining)}）";
        return false;
    }

    public bool TryClaimFreeDiamond()
    {
        if (!CanClaimFreeDiamond(out string failureReason, out _))
        {
            LogFailure("free_diamond", failureReason, ShopPurchaseFailedReason.PurchaseLimitReached);
            GameEvents.RaiseShopPurchaseFailed(
                this,
                GameConstants.ConfigIds.ShopFreeDiamond,
                ShopPurchaseFailedReason.PurchaseLimitReached,
                failureReason);
            return false;
        }

        if (!resourceManager.TryAdd(
                CurrencyType.Diamond,
                catalog.FreeDiamondGrantAmount,
                ResourceChangeReason.ShopFreeDiamond,
                out _))
        {
            return false;
        }

        saveManager.Current.lastFreeDiamondClaimUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();

        GameEvents.RaiseShopPurchased(
            this,
            new ShopPurchaseEventArgs(
                GameConstants.ConfigIds.ShopFreeDiamond,
                ShopRewardType.Diamond,
                catalog.FreeDiamondGrantAmount,
                CurrencyType.Diamond,
                0));

        return true;
    }

    public bool TryGetFreeDiamondCooldownRemaining(out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        if (catalog == null || saveManager?.Current == null)
        {
            return false;
        }

        long lastTicks = saveManager.Current.lastFreeDiamondClaimUtcTicks;
        if (lastTicks <= 0)
        {
            return false;
        }

        DateTime lastUtc = new DateTime(lastTicks, DateTimeKind.Utc);
        TimeSpan cooldown = TimeSpan.FromHours(catalog.FreeDiamondCooldownHours);
        DateTime readyAt = lastUtc + cooldown;
        if (DateTime.UtcNow >= readyAt)
        {
            return false;
        }

        remaining = readyAt - DateTime.UtcNow;
        return true;
    }

    public int GetAdTicketCount()
    {
        return resourceManager != null
            ? resourceManager.GetAdTicketCount()
            : saveManager?.Current?.adTickets ?? 0;
    }

    public long GetTechPoints()
    {
        return saveManager?.Current?.techPoints ?? 0;
    }

    public bool TryGetItemDisplay(string configId, out string costText, out string rewardText)
    {
        costText = string.Empty;
        rewardText = string.Empty;
        if (!TryResolveItem(configId, out ShopItemSO item))
        {
            return false;
        }

        costText = FormatPullCost(item);
        rewardText = $"{item.RewardAmount} {FormatRewardLabel(item.RewardType)}";
        return true;
    }

    public int GetAdFreeSupplyDailyLimit()
    {
        return catalog != null ? catalog.AdFreeSupplyDailyLimit : 5;
    }

    public int GetRemainingAdFreeSupplyCount(string configId)
    {
        if (!TryResolveAdFreeSupply(configId, out AdFreeSupplyRule rule))
        {
            return 0;
        }

        int used = GetEffectivePurchaseCount(configId, ShopRefreshPeriod.Daily);
        return Math.Max(0, rule.DailyLimit - used);
    }

    public bool CanClaimAdFreeSupply(string configId, out string failureReason)
    {
        failureReason = null;
        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "商店未就绪";
            return false;
        }

        if (!TryResolveAdFreeSupply(configId, out AdFreeSupplyRule rule))
        {
            failureReason = "未找到广告补给配置";
            return false;
        }

        int remaining = GetRemainingAdFreeSupplyCount(configId);
        if (remaining <= 0)
        {
            failureReason = $"今日次数已用完（{rule.DailyLimit}/{rule.DailyLimit}）";
            return false;
        }

        return true;
    }

    public bool TryClaimAdFreeSupply(string configId)
    {
        if (!CanClaimAdFreeSupply(configId, out string failureReason))
        {
            GameEvents.RaiseShopPurchaseFailed(
                this,
                configId,
                ShopPurchaseFailedReason.PurchaseLimitReached,
                failureReason);
            return false;
        }

        if (!TryResolveAdFreeSupply(configId, out AdFreeSupplyRule rule))
        {
            return false;
        }

        bool granted = GrantRewardType(rule.RewardType, rule.RewardAmount);
        if (IsCrateItem(configId))
        {
            string poolId = configId == GameConstants.ConfigIds.ShopAdCratePremium
                ? UpgradeCardConstants.PoolIds.ShopCratePremium
                : UpgradeCardConstants.PoolIds.ShopCrateCommon;
            granted |= GrantUpgradeCardReward(poolId, 1);
        }

        if (!granted)
        {
            GameEvents.RaiseShopPurchaseFailed(
                this,
                configId,
                ShopPurchaseFailedReason.InvalidConfiguration,
                "发放奖励失败");
            return false;
        }

        RecordAdFreeClaim(configId);
        GameEvents.RaiseShopPurchased(
            this,
            new ShopPurchaseEventArgs(configId, rule.RewardType, rule.RewardAmount, CurrencyType.Diamond, 0));
        return true;
    }

    public bool CanExchangeAdTickets(string configId, out string failureReason, out ShopPurchaseFailedReason reason)
    {
        failureReason = null;
        reason = ShopPurchaseFailedReason.None;

        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "商店未就绪";
            reason = ShopPurchaseFailedReason.NotInitialized;
            return false;
        }

        if (!TryResolveExchange(configId, out ExchangeRule rule))
        {
            failureReason = "未找到兑换配置";
            reason = ShopPurchaseFailedReason.ItemNotFound;
            return false;
        }

        if (GetAdTicketCount() < rule.TicketCost)
        {
            failureReason = $"广告券不足（需要 {rule.TicketCost}，当前 {GetAdTicketCount()}）";
            reason = ShopPurchaseFailedReason.InsufficientFunds;
            return false;
        }

        return true;
    }

    public bool TryExchangeAdTickets(string configId)
    {
        if (!CanExchangeAdTickets(configId, out string failureReason, out ShopPurchaseFailedReason reason))
        {
            GameEvents.RaiseShopPurchaseFailed(this, configId, reason, failureReason);
            return false;
        }

        if (!TryResolveExchange(configId, out ExchangeRule rule))
        {
            return false;
        }

        if (!resourceManager.TrySpend(CurrencyType.AdTicket, rule.TicketCost, ResourceChangeReason.ShopPurchase, out failureReason))
        {
            GameEvents.RaiseShopPurchaseFailed(this, configId, ShopPurchaseFailedReason.InsufficientFunds, failureReason);
            return false;
        }

        if (!GrantRewardType(rule.RewardType, rule.RewardAmount))
        {
            resourceManager.TryAdd(CurrencyType.AdTicket, rule.TicketCost, ResourceChangeReason.ShopPurchase, out _);
            GameEvents.RaiseShopPurchaseFailed(
                this,
                configId,
                ShopPurchaseFailedReason.InvalidConfiguration,
                "兑换发奖失败");
            return false;
        }

        GameEvents.RaiseShopPurchased(
            this,
            new ShopPurchaseEventArgs(configId, rule.RewardType, rule.RewardAmount, CurrencyType.AdTicket, rule.TicketCost));
        return true;
    }

    public bool TryGetExchangeDisplay(string configId, out string ticketCostText, out string rewardPreviewText)
    {
        ticketCostText = string.Empty;
        rewardPreviewText = string.Empty;
        if (!TryResolveExchange(configId, out ExchangeRule rule))
        {
            return false;
        }

        ticketCostText = $"{rule.TicketCost} 券";
        rewardPreviewText = FormatExchangeReward(rule.RewardType, rule.RewardAmount);
        return true;
    }

    private bool GrantReward(ShopItemSO item)
    {
        if (item.RewardType == ShopRewardType.UpgradeCard)
        {
            return GrantUpgradeCardReward(item.RewardConfigId, (int)item.RewardAmount);
        }

        if (!string.IsNullOrWhiteSpace(item.RewardConfigId) && IsCrateItem(item.ConfigId))
        {
            return GrantUpgradeCardReward(item.RewardConfigId, (int)Mathf.Max(1, item.RewardAmount));
        }

        return GrantRewardType(item.RewardType, item.RewardAmount);
    }

    private bool GrantRewardType(ShopRewardType rewardType, long amount)
    {
        switch (rewardType)
        {
            case ShopRewardType.AdTicket:
                return TryAddAdTickets((int)amount);
            case ShopRewardType.TechPoint:
                return TryAddTechPoints(amount);
            case ShopRewardType.UpgradeCard:
                return false;
            default:
                CurrencyType currency = MapRewardToCurrency(rewardType);
                return resourceManager.TryAdd(currency, amount, ResourceChangeReason.ShopPurchase, out _);
        }
    }

    private bool GrantUpgradeCardReward(string poolConfigId, int drawCount)
    {
        if (!ServiceLocator.TryGet(out UpgradeCardManager upgradeCardManager))
        {
            return false;
        }

        return upgradeCardManager.TryGrantFromPool(
            poolConfigId,
            drawCount,
            UpgradeCardRewardSource.ShopCrate,
            out _);
    }

    private static bool IsCrateItem(string configId) =>
        configId != null && (
            configId.Contains("crate") ||
            configId.Contains("ad_crate"));

    private bool TryAddAdTickets(int amount)
    {
        if (resourceManager == null || amount <= 0)
        {
            return false;
        }

        return resourceManager.TryGrantAdTicketReward(amount, ResourceChangeReason.ShopPurchase, out _);
    }

    private bool TryAddTechPoints(long amount)
    {
        if (saveManager?.Current == null || amount <= 0)
        {
            return false;
        }

        saveManager.Current.techPoints += amount;
        saveManager.MarkDirty();
        return true;
    }

    private void RecordAdFreeClaim(string configId)
    {
        SaveData save = saveManager.Current;
        int count = GetEffectivePurchaseCount(configId, ShopRefreshPeriod.Daily) + 1;
        ConfigIdIntPairListUtility.SetValue(save.shopPurchaseCounts, configId, count);
        ConfigIdLongPairListUtility.SetValue(save.shopLastPurchaseUtcTicks, configId, DateTime.UtcNow.Ticks);
        saveManager.MarkDirty();
    }

    private void RecordPurchase(ShopItemSO item)
    {
        SaveData save = saveManager.Current;
        int count = GetEffectivePurchaseCount(item.ConfigId, item.RefreshPeriod) + 1;
        ConfigIdIntPairListUtility.SetValue(save.shopPurchaseCounts, item.ConfigId, count);
        ConfigIdLongPairListUtility.SetValue(
            save.shopLastPurchaseUtcTicks,
            item.ConfigId,
            DateTime.UtcNow.Ticks);
        saveManager.MarkDirty();
    }

    private bool TryResolveItem(string configId, out ShopItemSO item)
    {
        item = null;
        if (catalog != null && catalog.TryGetItem(configId, out item))
        {
            return true;
        }

        if (configManager != null && configManager.TryGetShopItem(configId, out item))
        {
            return true;
        }

        return false;
    }

    private static CurrencyType MapRewardToCurrency(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => CurrencyType.Gold,
            ShopRewardType.Diamond => CurrencyType.Diamond,
            ShopRewardType.Energy => CurrencyType.AdTicket,
            ShopRewardType.AdTicket => CurrencyType.AdTicket,
            _ => CurrencyType.Gold,
        };

    private static string FormatPullCost(ShopItemSO item)
    {
        string currencyLabel = item.PriceCurrency switch
        {
            CurrencyType.Gold => "金币",
            CurrencyType.Diamond => "水晶",
            _ => item.PriceCurrency.ToString(),
        };

        return item.PriceAmount >= 10000
            ? $"{item.PriceAmount / 1000}k {currencyLabel}"
            : $"{item.PriceAmount} {currencyLabel}";
    }

    private static string FormatRewardLabel(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => "金币",
            ShopRewardType.Diamond => "水晶",
            ShopRewardType.Energy => "广告券",
            ShopRewardType.AdTicket => "广告券",
            ShopRewardType.TechPoint => "科技点",
            _ => rewardType.ToString(),
        };

    private static string FormatExchangeReward(ShopRewardType rewardType, long amount) =>
        rewardType switch
        {
            ShopRewardType.Gold => amount >= 1000 ? $"{amount / 1000}k 金币" : $"{amount} 金币",
            ShopRewardType.Diamond => $"{amount} 水晶",
            _ => $"{amount} {FormatRewardLabel(rewardType)}",
        };

    private static string FormatCooldown(TimeSpan span)
    {
        if (span.TotalHours >= 1d)
        {
            return $"{Mathf.CeilToInt((float)span.TotalHours)} 小时";
        }

        return $"{Mathf.CeilToInt((float)span.TotalMinutes)} 分钟";
    }

    private bool TryResolveAdFreeSupply(string configId, out AdFreeSupplyRule rule)
    {
        int dailyLimit = GetAdFreeSupplyDailyLimit();
        rule = default;
        switch (configId)
        {
            case GameConstants.ConfigIds.ShopAdCrateCommon:
                rule = new AdFreeSupplyRule(ShopRewardType.Gold, 500, dailyLimit);
                return true;
            case GameConstants.ConfigIds.ShopAdCratePremium:
                rule = new AdFreeSupplyRule(ShopRewardType.Diamond, 5, dailyLimit);
                return true;
            case GameConstants.ConfigIds.ShopAdGoldSupply:
                rule = new AdFreeSupplyRule(ShopRewardType.Gold, 2000, dailyLimit);
                return true;
            default:
                return false;
        }
    }

    private static bool TryResolveExchange(string configId, out ExchangeRule rule)
    {
        rule = default;
        switch (configId)
        {
            case GameConstants.ConfigIds.ShopExchangeDiamond1:
                rule = new ExchangeRule(1, ShopRewardType.Diamond, 10);
                return true;
            case GameConstants.ConfigIds.ShopExchangeDiamond2:
                rule = new ExchangeRule(2, ShopRewardType.Diamond, 30);
                return true;
            case GameConstants.ConfigIds.ShopExchangeDiamond3:
                rule = new ExchangeRule(3, ShopRewardType.Diamond, 80);
                return true;
            case GameConstants.ConfigIds.ShopExchangeDiamond4:
                rule = new ExchangeRule(4, ShopRewardType.Diamond, 150);
                return true;
            case GameConstants.ConfigIds.ShopExchangeGold1:
                rule = new ExchangeRule(1, ShopRewardType.Gold, 10000);
                return true;
            case GameConstants.ConfigIds.ShopExchangeGold2:
                rule = new ExchangeRule(2, ShopRewardType.Gold, 30000);
                return true;
            case GameConstants.ConfigIds.ShopExchangeGold3:
                rule = new ExchangeRule(3, ShopRewardType.Gold, 80000);
                return true;
            case GameConstants.ConfigIds.ShopExchangeGold4:
                rule = new ExchangeRule(4, ShopRewardType.Gold, 150000);
                return true;
            default:
                return false;
        }
    }

    private void LogFailure(string configId, string message, ShopPurchaseFailedReason reason)
    {
        if (configManager != null && configManager.ShouldLog() && !string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[ShopManager] {configId} 失败 ({reason}): {message}");
        }
    }

    [ContextMenu("Debug/Grant Test Diamonds")]
    private void DebugGrantDiamonds()
    {
        if (resourceManager != null)
        {
            resourceManager.TryAdd(CurrencyType.Diamond, 100, ResourceChangeReason.Debug, out _);
        }
    }

    [ContextMenu("Debug/Grant Test Gold")]
    private void DebugGrantGold()
    {
        if (resourceManager != null)
        {
            resourceManager.TryAdd(CurrencyType.Gold, 5000, ResourceChangeReason.Debug, out _);
        }
    }

    [ContextMenu("Debug/Grant Test Ad Tickets")]
    private void DebugGrantAdTickets()
    {
        TryAddAdTickets(12);
    }

    private readonly struct AdFreeSupplyRule
    {
        public ShopRewardType RewardType { get; }
        public long RewardAmount { get; }
        public int DailyLimit { get; }

        public AdFreeSupplyRule(ShopRewardType rewardType, long rewardAmount, int dailyLimit)
        {
            RewardType = rewardType;
            RewardAmount = rewardAmount;
            DailyLimit = dailyLimit;
        }
    }

    private readonly struct ExchangeRule
    {
        public int TicketCost { get; }
        public ShopRewardType RewardType { get; }
        public long RewardAmount { get; }

        public ExchangeRule(int ticketCost, ShopRewardType rewardType, long rewardAmount)
        {
            TicketCost = ticketCost;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }
    }
}
