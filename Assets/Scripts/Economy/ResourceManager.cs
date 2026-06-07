using System;
using UnityEngine;

/// <summary>
/// 局外资源管理：增减、查询、体力自然恢复、持久化，并通过 <see cref="GameEvents"/> 通知 UI。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 SaveManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ResourceManager&gt;()</c>。</para>
/// <para><b>规则：</b>金币/钻石/体力变更应经本类，不直接写 <see cref="SaveData"/>。</para>
/// </remarks>
public class ResourceManager : MonoBehaviour, IGameSystem
{
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
        SyncEnergyRecovery();
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        SyncEnergyRecovery();
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

        if (currency == CurrencyType.Energy)
        {
            SyncEnergyRecovery();
        }

        return currency switch
        {
            CurrencyType.Gold => saveManager.Gold,
            CurrencyType.Diamond => saveManager.Diamonds,
            CurrencyType.Energy => saveManager.Current.energy,
            CurrencyType.TechPoint => saveManager.Current.techPoints,
            _ => 0,
        };
    }

    public int GetMaxEnergy()
    {
        if (saveManager?.Current == null)
        {
            return EnergyConstants.DefaultMaxEnergy;
        }

        return Math.Max(1, saveManager.Current.maxEnergy);
    }

    public string GetEnergyDisplayText()
    {
        SyncEnergyRecovery();
        return $"{GetAmount(CurrencyType.Energy)}/{GetMaxEnergy()}";
    }

    public bool CanAfford(CurrencyType currency, long amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        return GetAmount(currency) >= amount;
    }

    public bool CanClaimAdEnergyReward(out string failureReason)
    {
        failureReason = null;
        SyncEnergyRecovery();

        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "资源系统未就绪";
            return false;
        }

        if (GetAmount(CurrencyType.Energy) >= GetMaxEnergy())
        {
            failureReason = "体力已满";
            return false;
        }

        return true;
    }

    public bool TryGrantAdEnergyReward(bool refillToMax, int rewardAmount, out ResourceChangedEventArgs change)
    {
        change = default;
        if (!CanClaimAdEnergyReward(out _))
        {
            return false;
        }

        long target = refillToMax
            ? GetMaxEnergy()
            : Math.Min(GetMaxEnergy(), GetAmount(CurrencyType.Energy) + Math.Max(1, rewardAmount));

        return TrySetEnergy(target, ResourceChangeReason.AdReward, out change);
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

        if (currency == CurrencyType.Energy)
        {
            long cappedTarget = Math.Min(GetMaxEnergy(), GetAmount(CurrencyType.Energy) + amount);
            return TrySetEnergy(cappedTarget, reason, out change);
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

        if (currency == CurrencyType.Energy)
        {
            SyncEnergyRecovery();
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

    public static string FormatInsufficientFunds(CurrencyType currency, long required, long current)
    {
        string label = currency switch
        {
            CurrencyType.Gold => "废料金",
            CurrencyType.Diamond => "量子钻",
            CurrencyType.Energy => "体力",
            CurrencyType.TechPoint => "科技点",
            _ => currency.ToString(),
        };

        return $"{label}不足（需要 {required}，当前 {current}）";
    }

    private bool TrySetEnergy(long targetAmount, ResourceChangeReason reason, out ResourceChangedEventArgs change)
    {
        change = default;
        if (!isInitialized || saveManager?.Current == null)
        {
            return false;
        }

        SyncEnergyRecovery();
        long previous = GetAmount(CurrencyType.Energy);
        long next = Math.Clamp(targetAmount, 0, GetMaxEnergy());
        if (next == previous)
        {
            return false;
        }

        ApplyAmount(CurrencyType.Energy, next);
        saveManager.MarkDirty();

        change = new ResourceChangedEventArgs(CurrencyType.Energy, previous, next, reason);
        GameEvents.RaiseResourceChanged(this, change);
        LogChange(change);
        return true;
    }

    private void SyncEnergyRecovery()
    {
        if (saveManager?.Current == null)
        {
            return;
        }

        SaveData save = saveManager.Current;
        int maxEnergy = Math.Max(1, save.maxEnergy);
        if (save.energy >= maxEnergy)
        {
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
        int recoveredPoints = (int)((now - lastUtc).TotalSeconds / EnergyConstants.SecondsPerPoint);
        if (recoveredPoints <= 0)
        {
            return;
        }

        int previous = save.energy;
        int next = Math.Min(maxEnergy, previous + recoveredPoints);
        if (next == previous)
        {
            return;
        }

        save.energy = next;
        save.lastEnergyRecoverUtcTicks = lastUtc
            .AddSeconds(recoveredPoints * EnergyConstants.SecondsPerPoint)
            .Ticks;
        saveManager.MarkDirty();

        ResourceChangedEventArgs change = new ResourceChangedEventArgs(
            CurrencyType.Energy,
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
            case CurrencyType.Energy:
                saveManager.Current.energy = (int)Math.Clamp(amount, 0, GetMaxEnergy());
                if (saveManager.Current.energy >= GetMaxEnergy())
                {
                    saveManager.Current.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
                }
                else if (saveManager.Current.lastEnergyRecoverUtcTicks <= 0)
                {
                    saveManager.Current.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
                }

                break;
            case CurrencyType.TechPoint:
                saveManager.Current.techPoints = Math.Max(0, amount);
                break;
            default:
                Debug.LogWarning($"[ResourceManager] 暂不支持的资源类型: {currency}");
                break;
        }
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
}
