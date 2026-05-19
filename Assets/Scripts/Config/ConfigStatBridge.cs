using UnityEngine;

/// <summary>
/// 将配置快照写入现有 <see cref="Entity_Stats"/>（迁移期适配器，不修改 SO）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>使用方式：</b>在确认新配置流程后，由 Enemy/Player Controller 在 Spawn/Start 时可选调用。</para>
/// </remarks>
public static class ConfigStatBridge
{
    public static void ApplyToEntityStats(StatRuntimeSnapshot snapshot, Entity_Stats entityStats)
    {
        if (snapshot == null || entityStats == null)
        {
            return;
        }

        if (entityStats.majorStats != null)
        {
            SetStat(entityStats.majorStats.maxHp, snapshot.Get(StatType.MaxHp));
            SetStat(entityStats.majorStats.moveSpeed, snapshot.Get(StatType.MoveSpeed));
            SetStat(entityStats.majorStats.attackSpeed, snapshot.Get(StatType.AttackSpeed));
            SetStat(entityStats.majorStats.attackSpeedMulti, snapshot.Get(StatType.AttackSpeedMulti));
        }

        if (entityStats.offenseStats != null)
        {
            SetStat(entityStats.offenseStats.damage, snapshot.Get(StatType.Damage));
            SetStat(entityStats.offenseStats.critChance, snapshot.Get(StatType.CritChance));
            SetStat(entityStats.offenseStats.critPower, snapshot.Get(StatType.CritPower));
            SetStat(entityStats.offenseStats.fireDamage, snapshot.Get(StatType.FireDamage));
            SetStat(entityStats.offenseStats.iceDamage, snapshot.Get(StatType.IceDamage));
            SetStat(entityStats.offenseStats.lightingDamage, snapshot.Get(StatType.LightningDamage));
        }

        if (entityStats.defenseStats != null)
        {
            SetStat(entityStats.defenseStats.armor, snapshot.Get(StatType.Armor));
            SetStat(entityStats.defenseStats.armorReduce, snapshot.Get(StatType.ArmorReduce));
        }
    }

    private static void SetStat(Stat stat, float value)
    {
        if (stat == null)
        {
            return;
        }

        stat.SetBaseValue(value);
    }
}
