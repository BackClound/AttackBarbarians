using System;
using UnityEngine;

/// <summary>
/// 可复用的基础属性块，供 Player/Enemy/Boss 等 Data SO 引用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。作为 <see cref="ConfigDataBase"/> 子类的序列化字段嵌入。</para>
/// </remarks>
[Serializable]
public class StatBlockConfig
{
    [Header("Major")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float attackSpeedMulti = 1f;

    [Header("Offense")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float critChance;
    [SerializeField] private float critPower = 0.5f;
    [SerializeField] private float fireDamage;
    [SerializeField] private float iceDamage;
    [SerializeField] private float lightningDamage;

    [Header("Defense")]
    [SerializeField] private float armor;
    [SerializeField] private float armorReduce;

    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public float AttackSpeedMulti => attackSpeedMulti;
    public float Damage => damage;
    public float CritChance => critChance;
    public float CritPower => critPower;
    public float FireDamage => fireDamage;
    public float IceDamage => iceDamage;
    public float LightningDamage => lightningDamage;
    public float Armor => armor;
    public float ArmorReduce => armorReduce;

    /// <summary>
    /// 按属性类型读取当前配置块中的基础数值。
    /// </summary>
    /// <param name="statType">要查询的属性类型。</param>
    /// <returns>对应属性的基础值；未识别类型时返回 0。</returns>
    public float GetStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.MaxHp: return maxHp;
            case StatType.MoveSpeed: return moveSpeed;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.AttackSpeedMulti: return attackSpeedMulti;
            case StatType.Damage: return damage;
            case StatType.CritChance: return critChance;
            case StatType.CritPower: return critPower;
            case StatType.FireDamage: return fireDamage;
            case StatType.IceDamage: return iceDamage;
            case StatType.LightningDamage: return lightningDamage;
            case StatType.Armor: return armor;
            case StatType.ArmorReduce: return armorReduce;
            default: return 0f;
        }
    }
}
