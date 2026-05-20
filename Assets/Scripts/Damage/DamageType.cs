/// <summary>
/// 伤害类别（普通、暴击、持续、真实伤害等）。
/// </summary>
/// <remarks>纯枚举，无需挂载。</remarks>
public enum DamageType
{
    Normal = 0,
    Critical = 1,
    Dot = 2,
    True = 3
}
