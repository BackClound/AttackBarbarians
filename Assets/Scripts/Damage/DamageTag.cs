using System;

/// <summary>
/// 伤害行为标签（可组合）。
/// </summary>
/// <remarks>纯枚举，无需挂载。</remarks>
[Flags]
public enum DamageTag
{
    None = 0,
    // 击退
    Knockback = 1 << 0,
    // 穿透
    Pierce = 1 << 1,
    // 范围
    Area = 1 << 2,
    // 链式
    Chain = 1 << 3,
    // 真实伤害
    TrueDamage = 1 << 4,
    /// <summary>跳过暴击掷骰，使用传入的最终伤害（兼容旧 float 入口）。</summary>
    SkipCalculation = 1 << 5
}
