/// <summary>
/// 体力系统常量，供 <see cref="ResourceManager"/> 与存档迁移使用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。默认值同步写入 <see cref="SaveData.CreateDefault"/>。</para>
/// </remarks>
public static class StaminaConstants
{
    /// <summary>每恢复 1 点体力所需秒数（2 分钟）。</summary>
    public const int SecondsPerPoint = 120;

    /// <summary>默认最大体力上限。</summary>
    public const int DefaultMaxStamina = 30;

    /// <summary>新存档默认起始体力。</summary>
    public const int DefaultStartingStamina = 30;

    /// <summary>每次开局消耗的体力点数。</summary>
    public const int BattleEntryCost = 5;
}
