using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置校验工具：空引用、负数、重复 ID、缺失图标、非法等级等。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="ConfigManager"/> 在 Initialize 时调用。</para>
/// </remarks>
public static class ConfigValidator
{
    public static ConfigValidationResult ValidateDatabase(ConfigDatabaseSO database)
    {
        var result = new ConfigValidationResult();
        if (database == null)
        {
            result.AddError("ConfigDatabase", "未分配 ConfigDatabaseSO。");
            return result;
        }

        ValidateUniqueIds(database.Players, result);
        ValidateUniqueIds(database.Enemies, result);
        ValidateUniqueIds(database.Skills, result);
        ValidateUniqueIds(database.Buffs, result);
        ValidateUniqueIds(database.Waves, result);
        ValidateUniqueIds(database.Bosses, result);
        ValidateUniqueIds(database.DropTables, result);

        ValidateEntries(database.Players, result);
        ValidateEntries(database.Enemies, result);
        ValidateEntries(database.Skills, result);
        ValidateEntries(database.Buffs, result);
        ValidateEntries(database.Waves, result);
        ValidateEntries(database.Bosses, result);
        ValidateEntries(database.DropTables, result);

        ValidateWaveReferences(database, result);
        ValidateBossReferences(database, result);
        ValidateDropReferences(database, result);

        return result;
    }

    private static void ValidateUniqueIds<T>(IReadOnlyList<T> entries, ConfigValidationResult result) where T : ConfigDataBase
    {
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        var seen = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            T entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            string id = entry.ConfigId;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!seen.Add(id))
            {
                result.AddError(entry.name, $"重复的 configId: {id}");
            }
        }
    }

    private static void ValidateEntries<T>(IReadOnlyList<T> entries, ConfigValidationResult result) where T : ConfigDataBase
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            T entry = entries[i];
            if (entry == null)
            {
                result.AddError("ConfigDatabase", $"列表 {typeof(T).Name} 第 {i} 项为空引用。");
                continue;
            }

            entry.CollectValidationErrors(result);
        }
    }

    private static void ValidateWaveReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.Waves == null)
        {
            return;
        }

        for (int i = 0; i < database.Waves.Count; i++)
        {
            WaveDataSO wave = database.Waves[i];
            if (wave == null)
            {
                continue;
            }

            IReadOnlyList<string> enemyIds = wave.EnemyConfigIds;
            if (enemyIds == null)
            {
                continue;
            }

            for (int j = 0; j < enemyIds.Count; j++)
            {
                string enemyId = enemyIds[j];
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    result.AddWarning(wave.name, $"EnemyConfigIds[{j}] 为空。");
                    continue;
                }

                if (!database.TryGetEnemy(enemyId, out _))
                {
                    result.AddError(wave.name, $"引用了不存在的敌人 configId: {enemyId}");
                }
            }
        }
    }

    private static void ValidateBossReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.Bosses == null)
        {
            return;
        }

        for (int i = 0; i < database.Bosses.Count; i++)
        {
            BossDataSO boss = database.Bosses[i];
            if (boss == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(boss.BaseEnemyConfigId) &&
                !database.TryGetEnemy(boss.BaseEnemyConfigId, out _))
            {
                result.AddError(boss.name, $"BaseEnemyConfigId 不存在: {boss.BaseEnemyConfigId}");
            }
        }
    }

    private static void ValidateDropReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.DropTables == null)
        {
            return;
        }

        for (int i = 0; i < database.DropTables.Count; i++)
        {
            DropTableSO table = database.DropTables[i];
            if (table == null)
            {
                continue;
            }

            IReadOnlyList<DropEntryConfig> entries = table.Entries;
            if (entries == null || entries.Count == 0)
            {
                result.AddWarning(table.name, "掉落表为空。");
            }
        }
    }

    public static float ApplyModifiers(float baseValue, IReadOnlyList<StatModifierConfig> modifiers, StatType targetStat)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return baseValue;
        }

        float flatSum = 0f;
        float percentAddSum = 0f;
        float percentMultiply = 1f;
        float finalMultiplier = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifierConfig mod = modifiers[i];
            if (mod == null || mod.StatType != targetStat)
            {
                continue;
            }

            switch (mod.ModifierType)
            {
                case ConfigModifierType.Flat:
                    flatSum += mod.Value;
                    break;
                case ConfigModifierType.PercentAdd:
                    percentAddSum += mod.Value;
                    break;
                case ConfigModifierType.PercentMultiply:
                    percentMultiply *= mod.Value;
                    break;
                case ConfigModifierType.FinalMultiplier:
                    finalMultiplier *= mod.Value;
                    break;
            }
        }

        float value = baseValue + flatSum;
        value += baseValue * percentAddSum;
        value *= percentMultiply;
        value *= finalMultiplier;
        return value;
    }
}
