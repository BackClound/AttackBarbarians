using System;
using UnityEngine;

/// <summary>
/// 局外资源管理：增减、查询、体力自然恢复、持久化，并通过 <see cref="GameEvents"/> 通知 UI。
/// </summary>
public class ResourceManager : MonoBehaviour, IGameSystem
{
    [Header("Debug (Play Mode)")]
    [SerializeField] private long debugGoldAmount = 100000;
    [SerializeField] private long debugDiamondAmount = 10000;

    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out configManager);
        SyncStaminaRecovery();
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        SyncStaminaRecovery();
    }

    public void Shutdown()
    {
        isInitialized = false;
    }

    public long GetAmount(CurrencyType currency)
    {
        if (!isInitialized || saveManager?.Current == null)
        {
            return 0;
        }

        if (currency == CurrencyType.Stamina)
        {
            SyncStaminaRecovery();
        }

        return currency switch
        {
            CurrencyType.Gold => saveManager.Gold,
            CurrencyType.Diamond => saveManager.Diamonds,
            CurrencyType.AdTicket => saveManager.Current.adTickets,
            CurrencyType.Stamina => saveManager.Current.energy,
            _ => 0,
        };
    }

    public int GetAdTicketCount() => (int)GetAmount(CurrencyType.AdTicket);

    public int GetMaxStamina()
    {
        if (saveManager?.Current == null)
        {
            return StaminaConstants.DefaultMaxStamina;
        }

        return Math.Max(1, saveManager.Current.maxEnergy);
    }

    public string GetStaminaDisplayText()
    {
        SyncStaminaRecovery();
        return $"{GetAmount(CurrencyType.Stamina)}/{GetMaxStamina()}";
    }

    public bool IsStaminaFull()
    {
        SyncStaminaRecovery();
        return GetAmount(CurrencyType.Stamina) >= GetMaxStamina();
    }

    /// <summary>体力未满时返回恢复满所需倒计时文案；已满时返回 null。</summary>
    public string GetStaminaRecoverySubtitle()
    {
        if (!TryGetStaminaRecoveryRemaining(out TimeSpan remaining))
        {
            return null;
        }

        return FormatRecoveryDuration(remaining);
    }

    public bool TryGetStaminaRecoveryRemaining(out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        SyncStaminaRecovery();

        if (saveManager?.Current == null || IsStaminaFull())
        {
            return false;
        }

        SaveData save = saveManager.Current;
        int missing = GetMaxStamina() - save.energy;
        if (missing <= 0)
        {
            return false;
        }

        long lastTicks = save.lastEnergyRecoverUtcTicks;
        if (lastTicks <= 0)
        {
            remaining = TimeSpan.FromSeconds(missing * StaminaConstants.SecondsPerPoint);
            return remaining > TimeSpan.Zero;
        }

        DateTime lastUtc = new DateTime(lastTicks, DateTimeKind.Utc);
        DateTime fullAtUtc = lastUtc.AddSeconds(missing * StaminaConstants.SecondsPerPoint);
        remaining = fullAtUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        return true;
    }

    public bool CanAfford(CurrencyType currency, long amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        return GetAmount(currency) >= amount;
    }

    public bool CanStartBattle(out string failureReason)
    {
        failureReason = null;
        return CanAfford(CurrencyType.Stamina, StaminaConstants.BattleEntryCost, out failureReason);
    }

    public bool CanAfford(CurrencyType currency, long amount, out string failureReason)
    {
        failureReason = null;
        if (CanAfford(currency, amount))
        {
            return true;
        }

        failureReason = FormatInsufficientFunds(currency, amount, GetAmount(currency));
        return false;
    }

    public bool TryGrantAdTicketReward(int amount, ResourceChangeReason reason, out ResourceChangedEventArgs change)
    {
        return TryAdd(CurrencyType.AdTicket, amount, reason, out change);
    }

    public bool TryRefillStaminaFromAd(out ResourceChangedEventArgs change)
    {
        change = default;
        SyncStaminaRecovery();
        return TrySetStamina(GetMaxStamina(), ResourceChangeReason.AdReward, out change);
    }

    public bool TryAdd(
        CurrencyType currency,
        long amount,
        ResourceChangeReason reason,
        out ResourceChangedEventArgs change)
    {
        change = default;
        if (amount <= 0)
        {
            return false;
        }

        if (!isInitialized || saveManager?.Current == null)
        {
            return false;
        }

        if (currency == CurrencyType.Stamina)
        {
            long cappedTarget = Math.Min(GetMaxStamina(), GetAmount(CurrencyType.Stamina) + amount);
            return TrySetStamina(cappedTarget, reason, out change);
        }

        long previous = GetAmount(currency);
        long next = previous + amount;
        ApplyAmount(currency, next);
        saveManager.MarkDirty();

        change = new ResourceChangedEventArgs(currency, previous, next, reason);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
        return true;
    }

    public bool TrySpend(
        CurrencyType currency,
        long amount,
        ResourceChangeReason reason,
        out string failureReason)
    {
        failureReason = null;
        if (amount <= 0)
        {
            return true;
        }

        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "资源系统未就绪";
            return false;
        }

        if (currency == CurrencyType.Stamina)
        {
            SyncStaminaRecovery();
        }

        long previous = GetAmount(currency);
        if (previous < amount)
        {
            failureReason = FormatInsufficientFunds(currency, amount, previous);
            return false;
        }

        long next = previous - amount;
        ApplyAmount(currency, next);
        saveManager.MarkDirty();

        ResourceChangedEventArgs change = new ResourceChangedEventArgs(currency, previous, next, reason);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
        return true;
    }

    public static string FormatRecoveryDuration(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        if (remaining.TotalHours >= 1d)
        {
            return $"{(int)remaining.TotalHours}小时{remaining.Minutes}分后满";
        }

        if (remaining.TotalMinutes >= 1d)
        {
            return $"{remaining.Minutes}分{remaining.Seconds}秒后满";
        }

        return $"{Mathf.Max(1, remaining.Seconds)}秒后满";
    }

    public static string FormatInsufficientFunds(CurrencyType currency, long required, long current)
    {
        string label = currency switch
        {
            CurrencyType.Gold => "废料金",
            CurrencyType.Diamond => "量子钻",
            CurrencyType.AdTicket => "广告券",
            CurrencyType.Stamina => "体力",
            _ => currency.ToString(),
        };

        return $"{label}不足（需要 {required}，当前 {current}）";
    }

    private bool TrySetStamina(long targetAmount, ResourceChangeReason reason, out ResourceChangedEventArgs change)
    {
        change = default;
        if (!isInitialized || saveManager?.Current == null)
        {
            return false;
        }

        SyncStaminaRecovery();
        long previous = GetAmount(CurrencyType.Stamina);
        long next = Math.Clamp(targetAmount, 0, GetMaxStamina());
        if (next == previous)
        {
            return false;
        }

        ApplyAmount(CurrencyType.Stamina, next);
        saveManager.MarkDirty();

        change = new ResourceChangedEventArgs(CurrencyType.Stamina, previous, next, reason);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
        return true;
    }

    private void SyncStaminaRecovery()
    {
        if (saveManager?.Current == null)
        {
            return;
        }

        SaveData save = saveManager.Current;
        int maxStamina = GetMaxStamina();
        if (save.maxEnergy <= 0)
        {
            save.maxEnergy = maxStamina;
        }

        if (save.energy >= maxStamina)
        {
            save.energy = maxStamina;
            save.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
            return;
        }

        long lastTicks = save.lastEnergyRecoverUtcTicks;
        DateTime now = DateTime.UtcNow;
        if (lastTicks <= 0)
        {
            save.lastEnergyRecoverUtcTicks = now.Ticks;
            return;
        }

        DateTime lastUtc = new DateTime(lastTicks, DateTimeKind.Utc);
        int recoveredPoints = (int)((now - lastUtc).TotalSeconds / StaminaConstants.SecondsPerPoint);
        if (recoveredPoints <= 0)
        {
            return;
        }

        int previous = save.energy;
        int next = Math.Min(maxStamina, previous + recoveredPoints);
        if (next == previous)
        {
            return;
        }

        save.energy = next;
        save.lastEnergyRecoverUtcTicks = lastUtc
            .AddSeconds(recoveredPoints * StaminaConstants.SecondsPerPoint)
            .Ticks;
        saveManager.MarkDirty();

        ResourceChangedEventArgs change = new ResourceChangedEventArgs(
            CurrencyType.Stamina,
            previous,
            next,
            ResourceChangeReason.EnergyRecover);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
    }

    private void ApplyAmount(CurrencyType currency, long amount)
    {
        switch (currency)
        {
            case CurrencyType.Gold:
                saveManager.Gold = Math.Max(0, amount);
                break;
            case CurrencyType.Diamond:
                saveManager.Diamonds = Math.Max(0, amount);
                break;
            case CurrencyType.AdTicket:
                saveManager.Current.adTickets = (int)Math.Min(int.MaxValue, amount);
                break;
            case CurrencyType.Stamina:
                saveManager.Current.energy = (int)Math.Clamp(amount, 0, GetMaxStamina());
                if (saveManager.Current.energy >= GetMaxStamina())
                {
                    saveManager.Current.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
                }
                else if (saveManager.Current.lastEnergyRecoverUtcTicks <= 0)
                {
                    saveManager.Current.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
                }

                break;
            default:
                Debug.LogWarning($"[ResourceManager] 暂不支持的资源类型: {currency}");
                break;
        }
    }

    public bool TrySet(
        CurrencyType currency,
        long amount,
        ResourceChangeReason reason,
        out ResourceChangedEventArgs change)
    {
        change = default;
        if (!isInitialized || saveManager?.Current == null)
        {
            return false;
        }

        if (currency == CurrencyType.Stamina)
        {
            return TrySetStamina(amount, reason, out change);
        }

        long previous = GetAmount(currency);
        long next = Math.Max(0, amount);
        if (previous == next)
        {
            return true;
        }

        ApplyAmount(currency, next);
        saveManager.MarkDirty();
        saveManager.SaveImmediate();

        change = new ResourceChangedEventArgs(currency, previous, next, reason);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
        return true;
    }

    private void LogChange(ResourceChangedEventArgs change)
    {
        if (configManager == null || !configManager.ShouldLog())
        {
            return;
        }

        Debug.Log(
            $"[ResourceManager] {change.Currency} {change.PreviousAmount} -> {change.NewAmount} " +
            $"(delta={change.Delta}, reason={change.Reason})");
    }

    [ContextMenu("Debug/Set Gold To Test Value")]
    private void DebugSetGold() =>
        TrySet(CurrencyType.Gold, debugGoldAmount, ResourceChangeReason.Debug, out _);

    [ContextMenu("Debug/Set Diamonds To Test Value")]
    private void DebugSetDiamonds() =>
        TrySet(CurrencyType.Diamond, debugDiamondAmount, ResourceChangeReason.Debug, out _);

    [ContextMenu("Debug/Add 5000 Gold")]
    private void DebugAddGold() =>
        TryAdd(CurrencyType.Gold, 5000, ResourceChangeReason.Debug, out _);

    [ContextMenu("Debug/Add 100 Diamonds")]
    private void DebugAddDiamonds() =>
        TryAdd(CurrencyType.Diamond, 100, ResourceChangeReason.Debug, out _);
}
