/// <summary>
/// 可配置、可修正的属性类型枚举。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>使用方式：</b><see cref="StatModifierConfig"/>、<see cref="AttackInfo"/>、各类 Data SO。</para>
/// </remarks>
public enum StatType
{
    /// <summary>无效或未指定属性。</summary>
    None = 0,
    /// <summary>最大生命值。</summary>
    MaxHp = 1,
    /// <summary>移动速度。</summary>
    MoveSpeed = 2,
    /// <summary>攻击速度。</summary>
    AttackSpeed = 3,
    /// <summary>攻击速度倍率。</summary>
    AttackSpeedMulti = 4,
    /// <summary>基础攻击伤害。</summary>
    Damage = 10,
    /// <summary>暴击概率。</summary>
    CritChance = 11,
    /// <summary>暴击倍率。</summary>
    CritPower = 12,
    /// <summary>火焰附加伤害。</summary>
    FireDamage = 13,
    /// <summary>冰霜附加伤害。</summary>
    IceDamage = 14,
    /// <summary>闪电附加伤害。</summary>
    LightningDamage = 15,
    /// <summary>护甲值。</summary>
    Armor = 20,
    /// <summary>破甲值。</summary>
    ArmorReduce = 21,
    /// <summary>技能冷却时间。</summary>
    Cooldown = 30,
    /// <summary>攻击范围半径。</summary>
    AttackRadius = 31,
    /// <summary>经验获取倍率。</summary>
    ExperienceGain = 40
}
