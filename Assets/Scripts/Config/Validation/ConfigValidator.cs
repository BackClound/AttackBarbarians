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
        ValidateUniqueIds(database.Talents, result);
        ValidateUniqueIds(database.Equipment, result);
        ValidateUniqueIds(database.Waves, result);
        ValidateUniqueIds(database.Bosses, result);
        ValidateUniqueIds(database.BossSkills, result);
        ValidateUniqueIds(database.SpecialEnemyAbilities, result);
        ValidateUniqueIds(database.DropTables, result);

        ValidateEntries(database.Players, result);
        ValidateEntries(database.Enemies, result);
        ValidateEntries(database.Skills, result);
        ValidateEntries(database.Buffs, result);
        ValidateEntries(database.Talents, result);
        ValidateEntries(database.Equipment, result);
        ValidateEntries(database.Waves, result);
        ValidateEntries(database.Bosses, result);
        ValidateEntries(database.BossSkills, result);
        ValidateEntries(database.SpecialEnemyAbilities, result);
        ValidateEntries(database.DropTables, result);

        ValidateWaveReferences(database, result);
        ValidateBossReferences(database, result);
        ValidateBossSkillReferences(database, result);
        ValidateSpecialEnemyAbilityReferences(database, result);
        ValidateEnemySpecialAbilityReferences(database, result);
        ValidateDropReferences(database, result);
        ValidateSkillUnlockTable(database, result);

        return result;
    }

    private static void ValidateSkillUnlockTable(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        SkillUnlockTableSO table = database.SkillUnlockTable;
        if (table == null)
        {
            result.AddWarning("ConfigDatabase", "未配置 SkillUnlockTable，运行时将使用内置默认解锁阈值。");
            return;
        }

        table.CollectValidationErrors(result);
        if (database.Skills == null)
        {
            return;
        }

        for (int i = 0; i < table.Entries.Count; i++)
        {
            SkillUnlockEntryConfig entry = table.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.SkillConfigId))
            {
                continue;
            }

            if (!database.TryGetSkill(entry.SkillConfigId, out _))
            {
                result.AddError(table.name, $"解锁表引用了不存在的技能: {entry.SkillConfigId}");
            }
        }
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

            ValidateWaveEnemyIdList(wave, wave.EnemyConfigIds, database, result);

            IReadOnlyList<WaveEnemyEntry> entries = wave.EnemyEntries;
            if (entries == null)
            {
                continue;
            }

            for (int j = 0; j < entries.Count; j++)
            {
                WaveEnemyEntry entry = entries[j];
                if (entry == null || string.IsNullOrWhiteSpace(entry.EnemyConfigId))
                {
                    result.AddWarning(wave.name, $"enemyEntries[{j}] 为空。");
                    continue;
                }

                if (!database.TryGetEnemy(entry.EnemyConfigId, out _))
                {
                    result.AddError(wave.name, $"enemyEntries[{j}] 引用了不存在的敌人: {entry.EnemyConfigId}");
                }
            }

            if (wave.HasBoss && !string.IsNullOrWhiteSpace(wave.BossConfigId) &&
                !database.TryGetBoss(wave.BossConfigId, out _))
            {
                result.AddError(wave.name, $"引用了不存在的 Boss configId: {wave.BossConfigId}");
            }

            ValidateWaveSpecialEnemyIdList(wave, wave.SpecialEnemyConfigIds, database, result);
        }
    }

    private static void ValidateWaveSpecialEnemyIdList(
        WaveDataSO wave,
        IReadOnlyList<string> enemyIds,
        ConfigDatabaseSO database,
        ConfigValidationResult result)
    {
        if (enemyIds == null)
        {
            return;
        }

        for (int j = 0; j < enemyIds.Count; j++)
        {
            string enemyId = enemyIds[j];
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                result.AddWarning(wave.name, $"SpecialEnemyConfigIds[{j}] 为空。");
                continue;
            }

            if (!database.TryGetEnemy(enemyId, out EnemyDataSO enemyData))
            {
                result.AddError(wave.name, $"引用了不存在的特殊敌人 configId: {enemyId}");
                continue;
            }

            if (!SpecialEnemyRules.HasMechanics(enemyData.AbilityTags))
            {
                result.AddWarning(wave.name, $"SpecialEnemyConfigIds[{j}]={enemyId} 未配置特殊能力标签。");
            }
        }
    }

    private static void ValidateWaveEnemyIdList(
        WaveDataSO wave,
        IReadOnlyList<string> enemyIds,
        ConfigDatabaseSO database,
        ConfigValidationResult result)
    {
        if (enemyIds == null)
        {
            return;
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

            IReadOnlyList<string> skills = boss.SkillConfigIds;
            if (skills == null)
            {
                continue;
            }

            for (int s = 0; s < skills.Count; s++)
            {
                string skillId = skills[s];
                if (!string.IsNullOrWhiteSpace(skillId) && !database.TryGetBossSkill(skillId, out _))
                {
                    result.AddError(boss.name, $"引用了不存在的 Boss 技能 configId: {skillId}");
                }
            }
        }
    }

    private static void ValidateBossSkillReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.BossSkills == null)
        {
            return;
        }

        for (int i = 0; i < database.BossSkills.Count; i++)
        {
            BossSkillDataSO skill = database.BossSkills[i];
            if (skill == null)
            {
                continue;
            }

            skill.CollectValidationErrors(result);
        }
    }

    private static void ValidateSpecialEnemyAbilityReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.SpecialEnemyAbilities == null)
        {
            return;
        }

        for (int i = 0; i < database.SpecialEnemyAbilities.Count; i++)
        {
            SpecialEnemyAbilityDataSO ability = database.SpecialEnemyAbilities[i];
            if (ability == null)
            {
                continue;
            }

            ability.CollectValidationErrors(result);

            if ((ability.AbilityTag == EnemyAbilityTag.Summon || ability.AbilityTag == EnemyAbilityTag.Split) &&
                !string.IsNullOrWhiteSpace(ability.SummonEnemyConfigId) &&
                !database.TryGetEnemy(ability.SummonEnemyConfigId, out _))
            {
                result.AddError(ability.name, $"summonEnemyConfigId 不存在: {ability.SummonEnemyConfigId}");
            }
        }
    }

    private static void ValidateEnemySpecialAbilityReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.Enemies == null)
        {
            return;
        }

        for (int i = 0; i < database.Enemies.Count; i++)
        {
            EnemyDataSO enemy = database.Enemies[i];
            if (enemy == null || !SpecialEnemyRules.HasMechanics(enemy.AbilityTags))
            {
                continue;
            }

            IReadOnlyList<SpecialEnemyAbilityBinding> bindings = enemy.AbilityBindings;
            if (bindings == null)
            {
                continue;
            }

            for (int b = 0; b < bindings.Count; b++)
            {
                SpecialEnemyAbilityBinding binding = bindings[b];
                if (binding == null || string.IsNullOrWhiteSpace(binding.AbilityConfigId))
                {
                    continue;
                }

                if (!database.TryGetSpecialEnemyAbility(binding.AbilityConfigId, out _))
                {
                    result.AddError(enemy.name, $"abilityBindings[{b}] 引用了不存在的能力: {binding.AbilityConfigId}");
                }
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
