using System.Collections.Generic;

/// <summary>
/// 运行时属性快照，从配置复制后可叠加 Buff 修正，不修改 ScriptableObject。
/// </summary>
public sealed class StatRuntimeSnapshot
{
    private readonly Dictionary<StatType, float> values = new Dictionary<StatType, float>(24);

    /// <summary>
    /// 创建空的属性快照实例。
    /// </summary>
    public StatRuntimeSnapshot() { }

    /// <summary>
    /// 从属性配置块创建快照并复制所有基础值。
    /// </summary>
    /// <param name="source">来源属性配置块；为 null 时不复制任何值。</param>
    public StatRuntimeSnapshot(StatBlockConfig source)
    {
        if (source == null)
        {
            return;
        }

        CopyFromBlock(source);
    }

    /// <summary>
    /// 从属性配置块复制所有基础值到当前快照。
    /// </summary>
    /// <param name="source">来源属性配置块；为 null 时清空快照。</param>
    /// <summary>
    /// 从属性配置块复制所有基础值，覆盖当前快照内容。
    /// </summary>
    /// <param name="source">来源属性配置块；为 null 时清空快照。</param>
    public void CopyFromBlock(StatBlockConfig source)
    {
        values.Clear();
        if (source == null)
        {
            return;
        }

        values[StatType.MaxHp] = source.MaxHp;
        values[StatType.MoveSpeed] = source.MoveSpeed;
        values[StatType.AttackSpeed] = source.AttackSpeed;
        values[StatType.AttackSpeedMulti] = source.AttackSpeedMulti;
        values[StatType.Damage] = source.Damage;
        values[StatType.CritChance] = source.CritChance;
        values[StatType.CritPower] = source.CritPower;
        values[StatType.FireDamage] = source.FireDamage;
        values[StatType.IceDamage] = source.IceDamage;
        values[StatType.LightningDamage] = source.LightningDamage;
        values[StatType.Armor] = source.Armor;
        values[StatType.ArmorReduce] = source.ArmorReduce;
    }

    /// <summary>
    /// 读取指定属性的当前快照值。
    /// </summary>
    /// <param name="statType">目标属性类型。</param>
    /// <returns>属性值；未设置时返回 0。</returns>
    /// <summary>
    /// 读取指定属性的当前快照值。
    /// </summary>
    /// <param name="statType">要查询的属性类型。</param>
    /// <returns>对应属性值；未设置时返回 0。</returns>
    public float Get(StatType statType)
    {
        return values.TryGetValue(statType, out float value) ? value : 0f;
    }

    /// <summary>
    /// 设置指定属性的快照值。
    /// </summary>
    /// <param name="statType">目标属性类型。</param>
    /// <param name="value">要写入的数值。</param>
    /// <summary>
    /// 设置指定属性的快照值。
    /// </summary>
    /// <param name="statType">目标属性类型。</param>
    /// <param name="value">要写入的数值。</param>
    public void Set(StatType statType, float value)
    {
        values[statType] = value;
    }

    /// <summary>
    /// 将修正列表应用到快照中所有已有属性。
    /// </summary>
    /// <param name="modifiers">属性修正配置列表；为 null 时不执行任何操作。</param>
    /// <summary>
    /// 将修正列表应用到快照中所有已存在的属性。
    /// </summary>
    /// <param name="modifiers">属性修正配置列表；为 null 时不执行任何操作。</param>
    public void ApplyModifiers(IReadOnlyList<StatModifierConfig> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }

        var keys = new List<StatType>(values.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            StatType statType = keys[i];
            float baseValue = values[statType];
            values[statType] = ConfigValidator.ApplyModifiers(baseValue, modifiers, statType);
        }
    }

    /// <summary>
    /// 从另一个快照复制所有属性值。
    /// </summary>
    /// <param name="other">来源快照；为 null 时清空当前快照。</param>
    /// <summary>
    /// 从另一个属性快照深拷贝所有数值。
    /// </summary>
    /// <param name="other">来源快照；为 null 时清空当前快照。</param>
    public void CopyFrom(StatRuntimeSnapshot other)
    {
        values.Clear();
        if (other == null)
        {
            return;
        }

        foreach (var pair in other.values)
        {
            values[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// 从场景内 <see cref="Entity_Stats"/> 读取当前基础值并写入快照。
    /// </summary>
    /// <param name="entityStats">来源实体属性组件；为 null 时清空快照。</param>
    public void CopyFromEntityStats(Entity_Stats entityStats)
    {
        values.Clear();
        if (entityStats == null)
        {
            return;
        }

        if (entityStats.majorStats != null)
        {
            Set(StatType.MaxHp, entityStats.majorStats.maxHp.GetValue());
            Set(StatType.MoveSpeed, entityStats.majorStats.moveSpeed.GetValue());
            Set(StatType.AttackSpeed, entityStats.majorStats.attackSpeed.GetValue());
            Set(StatType.AttackSpeedMulti, entityStats.majorStats.attackSpeedMulti.GetValue());
        }

        if (entityStats.offenseStats != null)
        {
            Set(StatType.Damage, entityStats.offenseStats.damage.GetValue());
            Set(StatType.CritChance, entityStats.offenseStats.critChance.GetValue());
            Set(StatType.CritPower, entityStats.offenseStats.critPower.GetValue());
            Set(StatType.FireDamage, entityStats.offenseStats.fireDamage.GetValue());
            Set(StatType.IceDamage, entityStats.offenseStats.iceDamage.GetValue());
            Set(StatType.LightningDamage, entityStats.offenseStats.lightingDamage.GetValue());
        }

        if (entityStats.defenseStats != null)
        {
            Set(StatType.Armor, entityStats.defenseStats.armor.GetValue());
            Set(StatType.ArmorReduce, entityStats.defenseStats.armorReduce.GetValue());
        }
    }
}
