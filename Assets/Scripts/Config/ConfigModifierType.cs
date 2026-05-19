/// <summary>
/// 属性修正运算类型：加法、百分比加算、百分比乘算、最终倍率。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>使用方式：</b>在 <see cref="StatModifierConfig"/> 与 Buff 管线中引用。</para>
/// </remarks>
public enum ConfigModifierType
{
    /// <summary>固定值加算（Flat）。</summary>
    Flat = 0,

    /// <summary>百分比加算，如 +10% 表示在基础值上叠加 base * 0.1。</summary>
    PercentAdd = 1,

    /// <summary>百分比乘算，如 1.2 表示 ×120%。</summary>
    PercentMultiply = 2,

    /// <summary>最终倍率，在所有 Flat/Percent 之后应用。</summary>
    FinalMultiplier = 3
}
