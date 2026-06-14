using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Meta 商店管理器：负责局外商品购买、资源兑换、限购计数与免费奖励领取。
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

    /// <summary>商店服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>当前绑定的商店目录配置。</summary>
    public ShopCatalogSO Catalog => catalog;

    /// <summary>
    /// 初始化商店服务，加载目录并解析依赖。
    /// </summary>
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

    /// <summary>
    /// 每帧更新（商店无逐帧逻辑）。
    /// </summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭商店服务并重置初始化状态。
    /// </summary>
    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>
    /// 获取目录中所有商品条目。
    /// </summary>
    /// <returns>商品列表；无目录时返回空数组。</returns>
    public IReadOnlyList<ShopItemSO> GetCatalogItems()
    {
        return catalog != null ? catalog.Items : Array.Empty<ShopItemSO>();
    }

    /// <summary>
    /// 获取指定商品在当前刷新周期内的有效购买次数。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <param name="refreshPeriod">限购刷新周期。</param>
    /// <returns>有效购买次数；存档未就绪时返回 0。</returns>
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

    /// <summary>
    /// 获取指定商品的剩余可购买次数。
    /// </summary>
    /// <param name="item">商品配置。</param>
    /// <returns>剩余次数；不限购时返回 <see cref="int.MaxValue"/>。</returns>
    public int GetRemainingPurchases(ShopItemSO item)
    {
        if (item == null || !item.HasPurchaseLimit)
        {
            return int.MaxValue;
        }

        int used = GetEffectivePurchaseCount(item.ConfigId, item.RefreshPeriod);
        return Mathf.Max(0, item.PurchaseLimit - used);
    }

    /// <summary>
    /// 校验指定商品是否可购买（不扣费、不发奖）。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <param name="failureReason">不可购买时的失败说明文案。</param>
    /// <param name="reason">不可购买时的失败原因枚举。</param>
    /// <returns>可购买时返回 true。</returns>
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

    /// <summary>
    /// 尝试购买指定商品：校验、扣费、发奖并记录限购计数。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <returns>购买成功时返回 true。</returns>
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

    /// <summary>
    /// 校验当前是否可领取免费钻石。
    /// </summary>
    /// <param name="failureReason">不可领取时的失败说明文案。</param>
    /// <param name="cooldownRemaining">剩余冷却时长。</param>
    /// <returns>可领取时返回 true。</returns>
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

    /// <summary>
    /// 尝试领取免费钻石并更新冷却时间。
    /// </summary>
    /// <returns>领取成功返回 true，否则 false。</returns>
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

    /// <summary>
    /// 获取免费钻石领取的剩余冷却时长。
    /// </summary>
    /// <param name="remaining">剩余冷却时长。</param>
    /// <returns>仍在冷却中返回 true，否则 false。</returns>
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

    /// <summary>
    /// 获取当前广告券数量。
    /// </summary>
    /// <returns>广告券数量。</returns>
    public int GetAdTicketCount()
    {
        return resourceManager != null
            ? resourceManager.GetAdTicketCount()
            : saveManager?.Current?.adTickets ?? 0;
    }

    /// <summary>
    /// 获取当前科技点数量。
    /// </summary>
    /// <returns>科技点数量。</returns>
    public long GetTechPoints()
    {
        return saveManager?.Current?.techPoints ?? 0;
    }

    /// <summary>
    /// 获取商品的价格与奖励展示文本。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <param name="costText">价格展示文本。</param>
    /// <param name="rewardText">奖励展示文本。</param>
    /// <returns>找到商品返回 true，否则 false。</returns>
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

    /// <summary>
    /// 获取每个广告补给位每日可领取次数上限。
    /// </summary>
    /// <returns>每日上限；无目录时默认 5。</returns>
    public int GetAdFreeSupplyDailyLimit()
    {
        return catalog != null ? catalog.AdFreeSupplyDailyLimit : 5;
    }

    /// <summary>
    /// 获取广告补给位今日剩余可领取次数。
    /// </summary>
    /// <param name="configId">补给位配置 ID。</param>
    /// <returns>剩余次数；未找到配置时返回 0。</returns>
    public int GetRemainingAdFreeSupplyCount(string configId)
    {
        if (!TryResolveAdFreeSupply(configId, out AdFreeSupplyRule rule))
        {
            return 0;
        }

        int used = GetEffectivePurchaseCount(configId, ShopRefreshPeriod.Daily);
        return Math.Max(0, rule.DailyLimit - used);
    }

    /// <summary>
    /// 校验是否可领取广告补给（不发放奖励）。
    /// </summary>
    /// <param name="configId">补给位配置 ID。</param>
    /// <param name="failureReason">不可领取时的说明文本。</param>
    /// <returns>可领取返回 true，否则 false。</returns>
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

    /// <summary>
    /// 尝试领取广告补给奖励并记录每日次数。
    /// </summary>
    /// <param name="configId">补给位配置 ID。</param>
    /// <returns>领取成功返回 true，否则 false。</returns>
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

    /// <summary>
    /// 校验是否可用广告券兑换指定商品。
    /// </summary>
    /// <param name="configId">兑换项配置 ID。</param>
    /// <param name="failureReason">失败时的说明文本。</param>
    /// <param name="reason">失败原因枚举。</param>
    /// <returns>可兑换返回 true，否则 false。</returns>
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

    /// <summary>
    /// 尝试用广告券兑换奖励。
    /// </summary>
    /// <param name="configId">兑换项配置 ID。</param>
    /// <returns>兑换成功返回 true，否则 false。</returns>
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

    /// <summary>
    /// 获取广告券兑换项的展示文本。
    /// </summary>
    /// <param name="configId">兑换项配置 ID。</param>
    /// <param name="ticketCostText">所需广告券展示文本。</param>
    /// <param name="rewardPreviewText">奖励预览文本。</param>
    /// <returns>找到兑换项返回 true，否则 false。</returns>
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

    /// <summary>
    /// 按商品配置发放奖励。
    /// </summary>
    /// <param name="item">商品配置。</param>
    /// <returns>发奖成功返回 true，否则 false。</returns>
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

    /// <summary>
    /// 按奖励类型发放对应资源。
    /// </summary>
    /// <param name="rewardType">奖励类型。</param>
    /// <param name="amount">奖励数量。</param>
    /// <returns>发奖成功返回 true，否则 false。</returns>
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

    /// <summary>
    /// 从升级卡卡池发放奖励。
    /// </summary>
    /// <param name="poolConfigId">卡池配置 ID。</param>
    /// <param name="drawCount">抽取次数。</param>
    /// <returns>发放成功时返回 true。</returns>
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

    /// <summary>
    /// 判断商品配置 ID 是否属于宝箱类商品。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <returns>是宝箱类商品时返回 true。</returns>
    private static bool IsCrateItem(string configId) =>
        configId != null && (
            configId.Contains("crate") ||
            configId.Contains("ad_crate"));

    /// <summary>
    /// 增加广告券数量。
    /// </summary>
    /// <param name="amount">增加数量。</param>
    /// <returns>增加成功时返回 true。</returns>
    private bool TryAddAdTickets(int amount)
    {
        if (resourceManager == null || amount <= 0)
        {
            return false;
        }

        return resourceManager.TryGrantAdTicketReward(amount, ResourceChangeReason.ShopPurchase, out _);
    }

    /// <summary>
    /// 增加科技点数量并写入存档。
    /// </summary>
    /// <param name="amount">增加数量。</param>
    /// <returns>增加成功时返回 true。</returns>
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

    /// <summary>
    /// 记录广告补给领取次数与 UTC 时间戳。
    /// </summary>
    /// <param name="configId">补给位配置 ID。</param>
    private void RecordAdFreeClaim(string configId)
    {
        SaveData save = saveManager.Current;
        int count = GetEffectivePurchaseCount(configId, ShopRefreshPeriod.Daily) + 1;
        ConfigIdIntPairListUtility.SetValue(save.shopPurchaseCounts, configId, count);
        ConfigIdLongPairListUtility.SetValue(save.shopLastPurchaseUtcTicks, configId, DateTime.UtcNow.Ticks);
        saveManager.MarkDirty();
    }

    /// <summary>
    /// 记录商品购买次数与 UTC 时间戳。
    /// </summary>
    /// <param name="item">已购买的商品配置。</param>
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

    /// <summary>
    /// 从目录或 ConfigManager 解析商品配置。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <param name="item">找到的商品配置。</param>
    /// <returns>找到返回 true，否则 false。</returns>
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

    /// <summary>
    /// 将商店奖励类型映射为货币类型。
    /// </summary>
    /// <param name="rewardType">奖励类型。</param>
    /// <returns>对应的货币类型。</returns>
    private static CurrencyType MapRewardToCurrency(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => CurrencyType.Gold,
            ShopRewardType.Diamond => CurrencyType.Diamond,
            ShopRewardType.Energy => CurrencyType.AdTicket,
            ShopRewardType.AdTicket => CurrencyType.AdTicket,
            _ => CurrencyType.Gold,
        };

    /// <summary>
    /// 格式化商品购买价格展示文本。
    /// </summary>
    /// <param name="item">商品配置。</param>
    /// <returns>价格展示文本。</returns>
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

    /// <summary>
    /// 格式化奖励类型的中文标签。
    /// </summary>
    /// <param name="rewardType">奖励类型。</param>
    /// <returns>中文标签文本。</returns>
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

    /// <summary>
    /// 格式化广告券兑换奖励预览文本。
    /// </summary>
    /// <param name="rewardType">奖励类型。</param>
    /// <param name="amount">奖励数量。</param>
    /// <returns>奖励预览文本。</returns>
    private static string FormatExchangeReward(ShopRewardType rewardType, long amount) =>
        rewardType switch
        {
            ShopRewardType.Gold => amount >= 1000 ? $"{amount / 1000}k 金币" : $"{amount} 金币",
            ShopRewardType.Diamond => $"{amount} 水晶",
            _ => $"{amount} {FormatRewardLabel(rewardType)}",
        };

    /// <summary>
    /// 格式化冷却剩余时间为可读文本。
    /// </summary>
    /// <param name="span">剩余时长。</param>
    /// <returns>可读冷却文本。</returns>
    private static string FormatCooldown(TimeSpan span)
    {
        if (span.TotalHours >= 1d)
        {
            return $"{Mathf.CeilToInt((float)span.TotalHours)} 小时";
        }

        return $"{Mathf.CeilToInt((float)span.TotalMinutes)} 分钟";
    }

    /// <summary>
    /// 解析广告补给位内置规则。
    /// </summary>
    /// <param name="configId">补给位配置 ID。</param>
    /// <param name="rule">解析到的补给规则。</param>
    /// <returns>找到规则返回 true，否则 false。</returns>
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

    /// <summary>
    /// 解析广告券兑换项内置规则。
    /// </summary>
    /// <param name="configId">兑换项配置 ID。</param>
    /// <param name="rule">解析到的兑换规则。</param>
    /// <returns>找到规则返回 true，否则 false。</returns>
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

    /// <summary>
    /// 记录购买失败日志（受 ConfigManager 日志开关控制）。
    /// </summary>
    /// <param name="configId">商品或上下文配置 ID。</param>
    /// <param name="message">失败说明。</param>
    /// <param name="reason">失败原因。</param>
    private void LogFailure(string configId, string message, ShopPurchaseFailedReason reason)
    {
        if (configManager != null && configManager.ShouldLog() && !string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[ShopManager] {configId} 失败 ({reason}): {message}");
        }
    }

    /// <summary>
    /// 调试：发放测试钻石。
    /// </summary>
    [ContextMenu("Debug/Grant Test Diamonds")]
    private void DebugGrantDiamonds()
    {
        if (resourceManager != null)
        {
            resourceManager.TryAdd(CurrencyType.Diamond, 100, ResourceChangeReason.Debug, out _);
        }
    }

    /// <summary>
    /// 调试：发放测试金币。
    /// </summary>
    [ContextMenu("Debug/Grant Test Gold")]
    private void DebugGrantGold()
    {
        if (resourceManager != null)
        {
            resourceManager.TryAdd(CurrencyType.Gold, 5000, ResourceChangeReason.Debug, out _);
        }
    }

    /// <summary>
    /// 调试：发放测试广告券。
    /// </summary>
    [ContextMenu("Debug/Grant Test Ad Tickets")]
    private void DebugGrantAdTickets()
    {
        TryAddAdTickets(12);
    }

    /// <summary>
    /// 广告补给位内置规则。
    /// </summary>
    private readonly struct AdFreeSupplyRule
    {
        /// <summary>奖励类型。</summary>
        public ShopRewardType RewardType { get; }

        /// <summary>奖励数量。</summary>
        public long RewardAmount { get; }

        /// <summary>每日领取上限。</summary>
        public int DailyLimit { get; }

        /// <summary>
        /// 创建广告补给规则。
        /// </summary>
        /// <param name="rewardType">奖励类型。</param>
        /// <param name="rewardAmount">奖励数量。</param>
        /// <param name="dailyLimit">每日上限。</param>
        public AdFreeSupplyRule(ShopRewardType rewardType, long rewardAmount, int dailyLimit)
        {
            RewardType = rewardType;
            RewardAmount = rewardAmount;
            DailyLimit = dailyLimit;
        }
    }

    /// <summary>
    /// 广告券兑换项内置规则。
    /// </summary>
    private readonly struct ExchangeRule
    {
        /// <summary>所需广告券数量。</summary>
        public int TicketCost { get; }

        /// <summary>兑换奖励类型。</summary>
        public ShopRewardType RewardType { get; }

        /// <summary>兑换奖励数量。</summary>
        public long RewardAmount { get; }

        /// <summary>
        /// 创建兑换规则。
        /// </summary>
        /// <param name="ticketCost">所需广告券数量。</param>
        /// <param name="rewardType">奖励类型。</param>
        /// <param name="rewardAmount">奖励数量。</param>
        public ExchangeRule(int ticketCost, ShopRewardType rewardType, long rewardAmount)
        {
            TicketCost = ticketCost;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }
    }
}
