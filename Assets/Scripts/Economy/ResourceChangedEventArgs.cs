/// <summary>
/// 资源数量变更事件负载。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// </remarks>
public readonly struct ResourceChangedEventArgs
{
    public CurrencyType Currency { get; }
    public long PreviousAmount { get; }
    public long NewAmount { get; }
    public long Delta { get; }
    public ResourceChangeReason Reason { get; }

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
