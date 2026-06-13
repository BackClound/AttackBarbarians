using System;
using UnityEngine;

/// <summary>
/// 局外资源管理：增减、查询、体力自然恢复、持久化，并通过 <see cref="GameEvents"/> 通知 UI。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 Save 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ResourceManager&gt;()</c>。</para>
/// <para><b>持久化：</b>所有资源变更通过 <see cref="SaveManager"/> 写入本地 JSON 存档。</para>
/// </remarks>
public class ResourceManager : MonoBehaviour, IGameSystem
{
    [Header("Debug (Play Mode)")]
    [SerializeField] private long debugGoldAmount = 100000;
    [SerializeField] private long debugDiamondAmount = 10000;

    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 初始化资源管理器并同步体力自然恢复。
    /// </summary>
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

    /// <summary>
    /// 每帧同步体力自然恢复状态。
    /// </summary>
    /// <param name="deltaTime">经过的时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        SyncStaminaRecovery();
    }

    /// <summary>
    /// 关闭资源管理器。
    /// </summary>
    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>
    /// 查询指定资源的当前持有量。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <returns>当前数量；系统未就绪时返回 0。</returns>
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

    /// <summary>查询当前广告券数量。</summary>
    /// <returns>广告券持有量（int 截断）。</returns>
    public int GetAdTicketCount() => (int)GetAmount(CurrencyType.AdTicket);

    /// <summary>查询体力上限。</summary>
    /// <returns>最大体力值；存档未就绪时返回默认值。</returns>
    public int GetMaxStamina()
    {
        if (saveManager?.Current == null)
        {
            return StaminaConstants.DefaultMaxStamina;
        }

        return Math.Max(1, saveManager.Current.maxEnergy);
    }

    /// <summary>获取体力显示文本（当前/上限）。</summary>
    /// <returns>格式为 "当前/上限" 的体力文本。</returns>
    public string GetStaminaDisplayText()
    {
        SyncStaminaRecovery();
        return $"{GetAmount(CurrencyType.Stamina)}/{GetMaxStamina()}";
    }

    /// <summary>判断体力是否已满。</summary>
    /// <returns>当前体力 &gt;= 上限时返回 true。</returns>
    public bool IsStaminaFull()
    {
        SyncStaminaRecovery();
        return GetAmount(CurrencyType.Stamina) >= GetMaxStamina();
    }

    /// <summary>体力未满时返回恢复满所需倒计时文案；已满时返回 null。</summary>
    /// <returns>中文倒计时文案；体力已满时返回 null。</returns>
    public string GetStaminaRecoverySubtitle()
    {
        if (!TryGetStaminaRecoveryRemaining(out TimeSpan remaining))
        {
            return null;
        }

        return FormatRecoveryDuration(remaining);
    }

    /// <summary>
    /// 尝试获取体力恢复至满所需的剩余时间。
    /// </summary>
    /// <param name="remaining">剩余恢复时间；体力已满时为 Zero。</param>
    /// <returns>体力未满且可计算时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 判断当前资源是否足够支付指定数量。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">所需数量；&lt;= 0 时视为足够。</param>
    /// <returns>持有量 &gt;= 所需量时返回 true。</returns>
    public bool CanAfford(CurrencyType currency, long amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        return GetAmount(currency) >= amount;
    }

    /// <summary>
    /// 判断是否有足够体力开始战斗。
    /// </summary>
    /// <param name="failureReason">不足时的失败原因文案；足够时为 null。</param>
    /// <returns>体力足够时返回 true，否则返回 false。</returns>
    public bool CanStartBattle(out string failureReason)
    {
        failureReason = null;
        return CanAfford(CurrencyType.Stamina, StaminaConstants.BattleEntryCost, out failureReason);
    }

    /// <summary>
    /// 判断是否有足够资源，并在不足时返回本地化失败原因。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">所需数量。</param>
    /// <param name="failureReason">不足时的失败原因文案；足够时为 null。</param>
    /// <returns>持有量足够时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 尝试发放广告券奖励。
    /// </summary>
    /// <param name="amount">发放数量。</param>
    /// <param name="reason">变更来源。</param>
    /// <param name="change">变更事件负载。</param>
    /// <returns>发放成功时返回 true，否则返回 false。</returns>
    public bool TryGrantAdTicketReward(int amount, ResourceChangeReason reason, out ResourceChangedEventArgs change)
    {
        return TryAdd(CurrencyType.AdTicket, amount, reason, out change);
    }

    /// <summary>
    /// 尝试通过广告将体力恢复至上限。
    /// </summary>
    /// <param name="change">变更事件负载。</param>
    /// <returns>体力发生变化时返回 true，否则返回 false。</returns>
    public bool TryRefillStaminaFromAd(out ResourceChangedEventArgs change)
    {
        change = default;
        SyncStaminaRecovery();
        return TrySetStamina(GetMaxStamina(), ResourceChangeReason.AdReward, out change);
    }

    /// <summary>
    /// 尝试增加指定资源（体力增加时受上限约束）。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">增加数量；须 &gt; 0。</param>
    /// <param name="reason">变更来源。</param>
    /// <param name="change">变更事件负载。</param>
    /// <returns>增加成功时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 尝试消耗指定资源。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">消耗数量；&lt;= 0 时视为成功。</param>
    /// <param name="reason">变更来源。</param>
    /// <param name="failureReason">不足或系统未就绪时的失败原因。</param>
    /// <returns>消耗成功时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 将剩余恢复时间格式化为中文倒计时文案。
    /// </summary>
    /// <param name="remaining">剩余时间。</param>
    /// <returns>格式化后的倒计时文本；已到期时返回空字符串。</returns>
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

    /// <summary>
    /// 生成资源不足时的本地化提示文案。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="required">所需数量。</param>
    /// <param name="current">当前持有量。</param>
    /// <returns>包含资源名称与数量对比的提示文本。</returns>
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

    /// <summary>
    /// 将体力设置为指定值（受上限约束），并广播变更事件。
    /// </summary>
    /// <param name="targetAmount">目标体力值。</param>
    /// <param name="reason">变更来源。</param>
    /// <param name="change">变更事件负载。</param>
    /// <returns>体力实际发生变化时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 根据离线/在线时间差计算并应用体力自然恢复。
    /// </summary>
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

    /// <summary>
    /// 将指定资源的持有量写入存档内存（不含事件广播）。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">目标数量。</param>
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

    /// <summary>
    /// 尝试将指定资源设置为绝对值（体力受上限约束，变更后立即写盘）。
    /// </summary>
    /// <param name="currency">资源类型。</param>
    /// <param name="amount">目标数量。</param>
    /// <param name="reason">变更来源。</param>
    /// <param name="change">变更事件负载。</param>
    /// <returns>设置成功时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 在调试日志开启时输出资源变更详情。
    /// </summary>
    /// <param name="change">资源变更事件负载。</param>
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

    /// <summary>Inspector 调试：将金币设为测试值。</summary>
    [ContextMenu("Debug/Set Gold To Test Value")]
    private void DebugSetGold() =>
        TrySet(CurrencyType.Gold, debugGoldAmount, ResourceChangeReason.Debug, out _);

    /// <summary>Inspector 调试：将钻石设为测试值。</summary>
    [ContextMenu("Debug/Set Diamonds To Test Value")]
    private void DebugSetDiamonds() =>
        TrySet(CurrencyType.Diamond, debugDiamondAmount, ResourceChangeReason.Debug, out _);

    /// <summary>Inspector 调试：增加 5000 金币。</summary>
    [ContextMenu("Debug/Add 5000 Gold")]
    private void DebugAddGold() =>
        TryAdd(CurrencyType.Gold, 5000, ResourceChangeReason.Debug, out _);

    /// <summary>Inspector 调试：增加 100 钻石。</summary>
    [ContextMenu("Debug/Add 100 Diamonds")]
    private void DebugAddDiamonds() =>
        TryAdd(CurrencyType.Diamond, 100, ResourceChangeReason.Debug, out _);
}
