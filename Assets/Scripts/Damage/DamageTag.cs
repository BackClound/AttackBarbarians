using System;

/// <summary>
/// 伤害行为标签（可组合）。
/// </summary>
/// <remarks>纯枚举，无需挂载。</remarks>
[Flags]
public enum DamageTag
{
    /// <summary>无特殊标签。</summary>
    None = 0,
    /// <summary>附带击退效果。</summary>
    Knockback = 1 << 0,
    /// <summary>穿透多个目标。</summary>
    Pierce = 1 << 1,
    /// <summary>范围伤害。</summary>
    Area = 1 << 2,
    /// <summary>链式弹射伤害。</summary>
    Chain = 1 << 3,
    /// <summary>真实伤害，无视护甲。</summary>
    TrueDamage = 1 << 4,
    /// <summary>跳过暴击掷骰，使用传入的最终伤害（兼容旧 float 入口）。</summary>
    SkipCalculation = 1 << 5
}
