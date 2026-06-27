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
    /// <summary>
    /// 校验配置总表中的引用完整性、重复 ID 与数值合法性。
    /// </summary>
    /// <param name="database">待校验的配置总表资产。</param>
    /// <returns>聚合后的校验结果，包含错误与警告列表。</returns>
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
        ValidateUniqueIds(database.Maps, result);
        ValidateUniqueIds(database.GameplayEvents, result);
        ValidateUniqueIds(database.Waves, result);
        ValidateUniqueIds(database.WaveSchedules, result);
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
        ValidateEntries(database.Maps, result);
        ValidateEntries(database.GameplayEvents, result);
        ValidateEntries(database.Waves, result);
        ValidateEntries(database.WaveSchedules, result);
        ValidateEntries(database.Bosses, result);
        ValidateEntries(database.BossSkills, result);
        ValidateEntries(database.SpecialEnemyAbilities, result);
        ValidateEntries(database.DropTables, result);

        ValidateWaveReferences(database, result);
        ValidateWaveScheduleReferences(database, result);
        ValidateBossReferences(database, result);
        ValidateBossSkillReferences(database, result);
        ValidateSpecialEnemyAbilityReferences(database, result);
        ValidateEnemySpecialAbilityReferences(database, result);
        ValidateDropReferences(database, result);
        ValidateSkillUnlockTable(database, result);
        ValidateGameplayEventReferences(database, result);
        ValidateMapReferences(database, result);

        return result;
    }

    /// <summary>
    /// 校验地图配置中的局内事件与偏好敌人引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
    private static void ValidateMapReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.Maps == null)
        {
            return;
        }

        for (int i = 0; i < database.Maps.Count; i++)
        {
            MapDataSO map = database.Maps[i];
            if (map == null)
            {
                continue;
            }

            IReadOnlyList<string> linkedEvents = map.LinkedGameplayEventIds;
            if (linkedEvents != null)
            {
                for (int j = 0; j < linkedEvents.Count; j++)
                {
                    string eventId = linkedEvents[j];
                    if (string.IsNullOrWhiteSpace(eventId))
                    {
                        result.AddWarning(map.name, $"linkedGameplayEventIds[{j}] 为空。");
                        continue;
                    }

                    if (!database.TryGetGameplayEvent(eventId, out _))
                    {
                        result.AddError(map.name, $"引用了不存在的局内事件: {eventId}");
                    }
                }
            }

            IReadOnlyList<string> preferredEnemies = map.PreferredEnemyConfigIds;
            if (preferredEnemies == null)
            {
                continue;
            }

            for (int j = 0; j < preferredEnemies.Count; j++)
            {
                string enemyId = preferredEnemies[j];
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    continue;
                }

                if (!database.TryGetEnemy(enemyId, out _))
                {
                    result.AddError(map.name, $"preferredEnemyConfigIds 引用了不存在的敌人: {enemyId}");
                }
            }
        }
    }

    /// <summary>
    /// 校验局内事件效果中的 Buff 引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
    private static void ValidateGameplayEventReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.GameplayEvents == null)
        {
            return;
        }

        for (int i = 0; i < database.GameplayEvents.Count; i++)
        {
            GameplayEventDataSO eventData = database.GameplayEvents[i];
            if (eventData == null)
            {
                continue;
            }

            IReadOnlyList<GameplayEventEffectConfig> effects = eventData.Effects;
            if (effects == null)
            {
                continue;
            }

            for (int j = 0; j < effects.Count; j++)
            {
                GameplayEventEffectConfig effect = effects[j];
                if (effect.EffectType != GameplayEventEffectType.ApplyPlayerBuff)
                {
                    continue;
                }

                string buffId = effect.StringParam;
                if (string.IsNullOrWhiteSpace(buffId))
                {
                    result.AddWarning(eventData.name, $"effects[{j}] ApplyPlayerBuff 未配置 buffId。");
                    continue;
                }

                if (!database.TryGetBuff(buffId, out _))
                {
                    result.AddError(eventData.name, $"effects[{j}] 引用了不存在的 Buff: {buffId}");
                }
            }
        }
    }

    /// <summary>
    /// 校验技能解锁表及其引用的技能配置。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验配置列表中是否存在重复的 configId。
    /// </summary>
    /// <typeparam name="T">配置资产类型。</typeparam>
    /// <param name="entries">待校验的配置列表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 遍历配置列表，校验空引用并调用各条目的自身校验逻辑。
    /// </summary>
    /// <typeparam name="T">配置资产类型。</typeparam>
    /// <param name="entries">待校验的配置列表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验波次配置中的敌人、Boss 与特殊敌人引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验波次特殊敌人 ID 列表的引用与能力标签。
    /// </summary>
    /// <param name="wave">波次配置。</param>
    /// <param name="enemyIds">特殊敌人 configId 列表。</param>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验波次普通敌人 ID 列表的引用。
    /// </summary>
    /// <param name="wave">波次配置。</param>
    /// <param name="enemyIds">敌人 configId 列表。</param>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验波次表段定义中的敌人、Boss 与特殊敌人引用。
    /// </summary>
    private static void ValidateWaveScheduleReferences(ConfigDatabaseSO database, ConfigValidationResult result)
    {
        if (database.WaveSchedules == null)
        {
            return;
        }

        for (int i = 0; i < database.WaveSchedules.Count; i++)
        {
            WaveScheduleSO schedule = database.WaveSchedules[i];
            if (schedule == null)
            {
                continue;
            }

            ValidateWaveSegmentReferences(schedule.name, schedule.DefaultSegment, database, result);

            IReadOnlyList<WaveSegmentDefinition> segments = schedule.Segments;
            if (segments != null)
            {
                for (int s = 0; s < segments.Count; s++)
                {
                    ValidateWaveSegmentReferences($"{schedule.name}/segments[{s}]", segments[s], database, result);
                }
            }

            IReadOnlyList<WaveExactOverride> exacts = schedule.ExactOverrides;
            if (exacts != null)
            {
                for (int e = 0; e < exacts.Count; e++)
                {
                    WaveExactOverride exact = exacts[e];
                    if (exact?.Patch == null)
                    {
                        continue;
                    }

                    ValidateWaveSegmentReferences(
                        $"{schedule.name}/exact[{exact.WaveIndex}]",
                        exact.Patch,
                        database,
                        result);
                }
            }
        }
    }

    /// <summary>校验单个波次段内的引用。</summary>
    private static void ValidateWaveSegmentReferences(
        string context,
        WaveSegmentDefinition segment,
        ConfigDatabaseSO database,
        ConfigValidationResult result)
    {
        if (segment == null)
        {
            return;
        }

        IReadOnlyList<string> enemyIds = segment.ResolveEnemyConfigIds();
        if (enemyIds != null)
        {
            for (int j = 0; j < enemyIds.Count; j++)
            {
                string enemyId = enemyIds[j];
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    result.AddWarning(context, $"EnemyConfigIds[{j}] 为空。");
                    continue;
                }

                if (!database.TryGetEnemy(enemyId, out _))
                {
                    result.AddError(context, $"引用了不存在的敌人 configId: {enemyId}");
                }
            }
        }

        IReadOnlyList<WaveEnemyEntry> entries = segment.ResolveEnemyEntries();
        if (entries != null)
        {
            for (int j = 0; j < entries.Count; j++)
            {
                WaveEnemyEntry entry = entries[j];
                if (entry == null || string.IsNullOrWhiteSpace(entry.EnemyConfigId))
                {
                    result.AddWarning(context, $"enemyEntries[{j}] 为空。");
                    continue;
                }

                if (!database.TryGetEnemy(entry.EnemyConfigId, out _))
                {
                    result.AddError(context, $"enemyEntries[{j}] 引用了不存在的敌人: {entry.EnemyConfigId}");
                }
            }
        }

        int probeWave = Mathf.Max(1, segment.StartWave);
        if (segment.ResolveHasBoss(probeWave))
        {
            string bossId = segment.ResolveBossConfigId(probeWave);
            if (!string.IsNullOrWhiteSpace(bossId) && !database.TryGetBoss(bossId, out _))
            {
                result.AddError(context, $"引用了不存在的 Boss configId: {bossId}");
            }
        }

        IReadOnlyList<string> specialIds = segment.SpecialEnemyConfigIds;
        if (specialIds != null)
        {
            for (int j = 0; j < specialIds.Count; j++)
            {
                string enemyId = specialIds[j];
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    result.AddWarning(context, $"SpecialEnemyConfigIds[{j}] 为空。");
                    continue;
                }

                if (!database.TryGetEnemy(enemyId, out EnemyDataSO enemyData))
                {
                    result.AddError(context, $"引用了不存在的特殊敌人 configId: {enemyId}");
                    continue;
                }

                if (!SpecialEnemyRules.HasMechanics(enemyData.AbilityTags))
                {
                    result.AddWarning(context, $"SpecialEnemyConfigIds[{j}]={enemyId} 未配置特殊能力标签。");
                }
            }
        }
    }

    /// <summary>
    /// 校验 Boss 配置中的基础敌人与技能引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验 Boss 技能配置条目的自身规则。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验特殊敌人能力配置及其召唤敌人引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验敌人配置中特殊能力绑定的引用。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 校验掉落表配置是否包含有效条目。
    /// </summary>
    /// <param name="database">配置总表。</param>
    /// <param name="result">校验结果容器。</param>
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

    /// <summary>
    /// 按修正类型顺序将指定属性的修正列表应用到基础值。
    /// </summary>
    /// <param name="baseValue">属性的原始基础值。</param>
    /// <param name="modifiers">修正配置列表；为 null 或空时直接返回基础值。</param>
    /// <param name="targetStat">要应用修正的目标属性类型。</param>
    /// <returns>叠加所有匹配修正后的最终数值。</returns>
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
