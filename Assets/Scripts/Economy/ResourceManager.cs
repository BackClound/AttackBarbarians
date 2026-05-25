using System;
using UnityEngine;

/// <summary>
/// 局外资源管理：增减、查询、持久化，并通过 <see cref="GameEvents"/> 通知 UI。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 SaveManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ResourceManager&gt;()</c>。</para>
/// <para><b>规则：</b>金币/钻石变更应经本类，不直接写 <see cref="SaveData"/>。</para>
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
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

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

        return currency switch
        {
            CurrencyType.Gold => saveManager.Gold,
            CurrencyType.Diamond => saveManager.Diamonds,
            CurrencyType.Energy => 0,
            _ => 0,
        };
    }

    public bool CanAfford(CurrencyType currency, long amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        return GetAmount(currency) >= amount;
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
            _ => currency.ToString(),
        };

        return $"{label}不足（需要 {required}，当前 {current}）";
    }

    private void ApplyAmount(CurrencyType currency, long amount)
    {
        amount = Math.Max(0, amount);
        switch (currency)
        {
            case CurrencyType.Gold:
                saveManager.Gold = amount;
                break;
            case CurrencyType.Diamond:
                saveManager.Diamonds = amount;
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
