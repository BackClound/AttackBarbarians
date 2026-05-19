/// <summary>
/// 可配置、可修正的属性类型枚举。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>使用方式：</b><see cref="StatModifierConfig"/>、<see cref="AttackInfo"/>、各类 Data SO。</para>
/// </remarks>
public enum StatType
{
    None = 0,
    MaxHp = 1,
    MoveSpeed = 2,
    AttackSpeed = 3,
    AttackSpeedMulti = 4,
    Damage = 10,
    CritChance = 11,
    CritPower = 12,
    FireDamage = 13,
    IceDamage = 14,
    LightningDamage = 15,
    Armor = 20,
    ArmorReduce = 21,
    Cooldown = 30,
    AttackRadius = 31,
    ExperienceGain = 40
}
