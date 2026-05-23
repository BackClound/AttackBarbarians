using System.Collections.Generic;

/// <summary>
/// 运行时属性快照，从配置复制后可叠加 Buff 修正，不修改 ScriptableObject。
/// </summary>
public sealed class StatRuntimeSnapshot
{
    private readonly Dictionary<StatType, float> values = new Dictionary<StatType, float>(24);

    public StatRuntimeSnapshot() { }

    public StatRuntimeSnapshot(StatBlockConfig source)
    {
        if (source == null)
        {
            return;
        }

        CopyFromBlock(source);
    }

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

    public float Get(StatType statType)
    {
        return values.TryGetValue(statType, out float value) ? value : 0f;
    }

    public void Set(StatType statType, float value)
    {
        values[statType] = value;
    }

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

    /// <summary>从场景内 <see cref="Entity_Stats"/> 读取当前基础值。</summary>
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
