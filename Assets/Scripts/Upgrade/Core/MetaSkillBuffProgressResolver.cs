using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从局外永久成长（存档 permanentUpgrades、GameConfig 开局 Buff）解析各 SkillBuffKind 已达最高 tier。
/// 用于局内三选一池：下一档可抽 tier = 局外基线 + 局内已选最高 tier。
/// </summary>
internal static class MetaSkillBuffProgressResolver
{
    /// <summary>
    /// 将局外 SkillBuff 最高 tier 合并进目标字典（取较大值）。
    /// </summary>
    public static void MergeMetaHighestTiers(
        IDictionary<SkillBuffKind, int> destination,
        SaveData save,
        ConfigManager configManager,
        GameConfig gameConfig = null)
    {
        if (destination == null)
        {
            return;
        }

        MergeFromPermanentUpgrades(destination, save, configManager);
        MergeFromStartupBuffs(destination, gameConfig, configManager);
    }

    /// <summary>统计局外已元解锁的技能数量。</summary>
    public static int CountMetaUnlockedSkills(SkillUnlockService unlockService, SaveData save)
    {
        int count = 0;
        for (int t = 0; t <= (int)SkillType.Heal; t++)
        {
            SkillType type = (SkillType)t;
            string skillId = ResolveSkillConfigId(type);
            if (string.IsNullOrWhiteSpace(skillId))
            {
                continue;
            }

            if (unlockService != null && unlockService.IsMetaUnlocked(skillId))
            {
                count++;
                continue;
            }

            if (save != null && save.IsSkillUnlocked(skillId))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>局外是否已元解锁指定技能类型。</summary>
    public static bool IsSkillMetaUnlocked(
        SkillType skillType,
        SkillUnlockService unlockService,
        SaveData save)
    {
        string skillId = ResolveSkillConfigId(skillType);
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        if (unlockService != null && unlockService.IsMetaUnlocked(skillId))
        {
            return true;
        }

        return save != null && save.IsSkillUnlocked(skillId);
    }

    /// <summary>将 SkillType 映射为技能 configId。</summary>
    public static string ResolveSkillConfigId(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Shoot:
                return GameConstants.ConfigIds.SkillShoot;
            case SkillType.Lightning:
                return GameConstants.ConfigIds.SkillLightning;
            case SkillType.Thunder:
                return GameConstants.ConfigIds.SkillThunder;
            case SkillType.FireRain:
                return GameConstants.ConfigIds.SkillFireRain;
            case SkillType.WaterWave:
                return GameConstants.ConfigIds.SkillWaterWave;
            case SkillType.Ice:
                return GameConstants.ConfigIds.SkillIce;
            case SkillType.Heal:
                return GameConstants.ConfigIds.SkillHeal;
            default:
                return string.Empty;
        }
    }

    /// <summary>从永久升级存档合并 SkillBuff 最高 tier。</summary>
    private static void MergeFromPermanentUpgrades(
        IDictionary<SkillBuffKind, int> destination,
        SaveData save,
        ConfigManager configManager)
    {
        if (save?.permanentUpgrades == null || configManager == null)
        {
            return;
        }

        for (int i = 0; i < save.permanentUpgrades.Count; i++)
        {
            ConfigIdIntPair entry = save.permanentUpgrades[i];
            if (entry.value <= 0 || string.IsNullOrWhiteSpace(entry.configId))
            {
                continue;
            }

            if (!configManager.TryGetBuff(entry.configId, out BuffDataSO buff) ||
                !buff.HasSkillBuff)
            {
                continue;
            }

            RecordHighestTier(destination, buff.SkillBuffKind, buff.SkillBuffTier);
        }
    }

    /// <summary>从 GameConfig 开局 Buff 合并 SkillBuff tier。</summary>
    private static void MergeFromStartupBuffs(
        IDictionary<SkillBuffKind, int> destination,
        GameConfig gameConfig,
        ConfigManager configManager)
    {
        if (!GameConfigPlaytestSettings.IsStartupBuffEnabled(gameConfig) || configManager == null)
        {
            return;
        }

        IReadOnlyList<GameConfigStartupBuffEntry> entries = gameConfig.StartupBuffs;
        for (int i = 0; i < entries.Count; i++)
        {
            BuffDataSO buff = ResolveStartupBuff(entries[i], configManager);
            if (buff == null || !buff.HasSkillBuff)
            {
                continue;
            }

            RecordHighestTier(destination, buff.SkillBuffKind, buff.SkillBuffTier);
        }
    }

    /// <summary>解析单条开局 Buff 配置为 BuffDataSO。</summary>
    /// <param name="entry">开局 Buff 条目。</param>
    /// <param name="configManager">配置管理器。</param>
    private static BuffDataSO ResolveStartupBuff(GameConfigStartupBuffEntry entry, ConfigManager configManager)
    {
        if (entry == null)
        {
            return null;
        }

        if (entry.Buff != null)
        {
            return entry.Buff;
        }

        string configId = entry.BuffConfigId;
        if (string.IsNullOrWhiteSpace(configId))
        {
            return null;
        }

        return configManager.TryGetBuff(configId, out BuffDataSO buff) ? buff : null;
    }

    /// <summary>记录某 SkillBuffKind 已拥有的最高 tier。</summary>
    /// <param name="destination">目标字典。</param>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">tier 等级。</param>
    private static void RecordHighestTier(
        IDictionary<SkillBuffKind, int> destination,
        SkillBuffKind kind,
        int tier)
    {
        if (kind == SkillBuffKind.None || tier <= 0)
        {
            return;
        }

        if (!destination.TryGetValue(kind, out int existing) || tier > existing)
        {
            destination[kind] = tier;
        }
    }
}
