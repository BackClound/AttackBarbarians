/// <summary>
/// 局外可消耗/展示的资源类型。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>关联系统：</b><see cref="ResourceManager"/>、<see cref="SaveData"/>。</para>
/// </remarks>
public enum CurrencyType
{
    /// <summary>废料金，局内结算与商店消费的主要货币。</summary>
    Gold = 0,
    /// <summary>量子钻，高级消费与奖励货币。</summary>
    Diamond = 1,
    /// <summary>广告券，观看激励广告获得，可用于特定兑换。</summary>
    AdTicket = 2,
    /// <summary>体力，开局消耗并随时间自然恢复。</summary>
    Stamina = 3,
}
