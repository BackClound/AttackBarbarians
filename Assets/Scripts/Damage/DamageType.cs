/// <summary>
/// 伤害类别（普通、暴击、持续、真实伤害等）。
/// </summary>
/// <remarks>纯枚举，无需挂载。</remarks>
public enum DamageType
{
    /// <summary>普通伤害。</summary>
    Normal = 0,
    /// <summary>强制暴击伤害。</summary>
    Critical = 1,
    /// <summary>持续伤害（DoT）。</summary>
    Dot = 2,
    /// <summary>真实伤害，无视护甲。</summary>
    True = 3
}
