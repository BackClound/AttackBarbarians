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

        if (item.RewardAmount <= 0)
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

    private bool GrantReward(ShopItemSO item)
    {
        CurrencyType currency = MapRewardToCurrency(item.RewardType);
        if (currency == CurrencyType.Energy)
        {
            Debug.LogWarning($"[ShopManager] 体力奖励暂未实现 item={item.ConfigId}");
            return false;
        }

        return resourceManager.TryAdd(
            currency,
            item.RewardAmount,
            ResourceChangeReason.ShopPurchase,
            out _);
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
            ShopRewardType.Energy => CurrencyType.Energy,
            _ => CurrencyType.Gold,
        };

    private static string FormatCooldown(TimeSpan span)
    {
        if (span.TotalHours >= 1d)
        {
            return $"{Mathf.CeilToInt((float)span.TotalHours)} 小时";
        }

        return $"{Mathf.CeilToInt((float)span.TotalMinutes)} 分钟";
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
}
