/// <summary>
/// 资源数量变更事件负载。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// </remarks>
public readonly struct ResourceChangedEventArgs
{
    /// <summary>变更的资源类型。</summary>
    public CurrencyType Currency { get; }

    /// <summary>变更前的持有量。</summary>
    public long PreviousAmount { get; }

    /// <summary>变更后的持有量。</summary>
    public long NewAmount { get; }

    /// <summary>数量变化量（新值减旧值）。</summary>
    public long Delta { get; }

    /// <summary>变更来源。</summary>
    public ResourceChangeReason Reason { get; }

    /// <summary>
    /// 构造资源变更事件负载。
    /// </summary>
    /// <param name="currency">变更的资源类型。</param>
    /// <param name="previousAmount">变更前数量。</param>
    /// <param name="newAmount">变更后数量。</param>
    /// <param name="reason">变更来源。</param>
    public ResourceChangedEventArgs(
        CurrencyType currency,
        long previousAmount,
        long newAmount,
        ResourceChangeReason reason)
    {
        Currency = currency;
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        Delta = newAmount - previousAmount;
        Reason = reason;
    }
}
