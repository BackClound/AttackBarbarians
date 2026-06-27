using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外 Skill / 永久 Buff 存档测试工具：读写磁盘存档、重置与批量编辑 Meta 成长字段。
/// Editor 窗口与调试菜单共用；Play 模式下可同步到 <see cref="SaveManager"/>。
/// </summary>
public static class SaveMetaProgressTestUtility
{
    /// <summary>可编辑的技能 Meta 条目。</summary>
    public readonly struct SkillMetaEntry
    {
        public SkillMetaEntry(string configId, string label, bool alwaysUnlocked)
        {
            ConfigId = configId;
            Label = label;
            AlwaysUnlocked = alwaysUnlocked;
        }

        public string ConfigId { get; }
        public string Label { get; }
        public bool AlwaysUnlocked { get; }
    }

    private static readonly SkillMetaEntry[] SkillEntries =
    {
        new(GameConstants.ConfigIds.SkillShoot, "射击 Shoot", true),
        new(GameConstants.ConfigIds.SkillLightning, "闪电 Lightning", false),
        new(GameConstants.ConfigIds.SkillThunder, "落雷 Thunder", false),
        new(GameConstants.ConfigIds.SkillFireRain, "火雨 FireRain", false),
        new(GameConstants.ConfigIds.SkillWaterWave, "水浪 WaterWave", false),
        new(GameConstants.ConfigIds.SkillIce, "冰霜 Ice", false),
        new(GameConstants.ConfigIds.SkillHeal, "治疗 Heal", false),
    };

    /// <summary>全部可编辑技能条目。</summary>
    public static IReadOnlyList<SkillMetaEntry> AllSkillEntries => SkillEntries;

    /// <summary>默认存档文件名。</summary>
    public static string DefaultSaveFileName => SaveConstants.DefaultSaveFileName;

    /// <summary>读取磁盘存档（不存在时返回默认档）。</summary>
    public static bool TryLoadFromDisk(string fileName, out SaveData data, out string path)
    {
        fileName = string.IsNullOrWhiteSpace(fileName) ? DefaultSaveFileName : fileName;
        path = SaveFileIO.GetSaveFilePath(fileName);

        if (!SaveFileIO.TryReadText(path, out string json) || string.IsNullOrWhiteSpace(json))
        {
            data = SaveData.CreateDefault();
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveMetaProgressTest] 存档解析失败: {ex.Message}");
            data = SaveData.CreateDefault();
            return false;
        }

        if (data == null)
        {
            data = SaveData.CreateDefault();
            return false;
        }

        data = SaveVersionMigrator.Migrate(data);
        return true;
    }

    /// <summary>写入磁盘存档（含 .bak 备份）。</summary>
    public static bool WriteToDisk(SaveData data, string fileName = null, bool createBackup = true)
    {
        if (data == null)
        {
            return false;
        }

        fileName = string.IsNullOrWhiteSpace(fileName) ? DefaultSaveFileName : fileName;
        string path = SaveFileIO.GetSaveFilePath(fileName);
        data.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        return SaveFileIO.TryWriteText(path, json, createBackup);
    }

    /// <summary>
    /// 重置技能与永久 Buff：仅保留射击 Lv.1，清空局内进度与 permanentUpgrades。
    /// </summary>
    public static void ResetSkillsAndBuffs(
        SaveData data,
        bool resetPlayTime = false,
        bool clearUpgradeCardInventory = false)
    {
        if (data == null)
        {
            return;
        }

        data.ResetUnlockedSkillsAndBuffsKeepShootOnly();

        if (resetPlayTime && data.statistics != null)
        {
            data.statistics.totalPlayTimeSeconds = 0;
        }

        if (clearUpgradeCardInventory)
        {
            data.upgradeCardInventory?.Clear();
        }
    }

    /// <summary>技能是否已在存档中解锁。</summary>
    public static bool IsSkillUnlocked(SaveData save, string skillConfigId)
    {
        if (save == null || string.IsNullOrWhiteSpace(skillConfigId))
        {
            return false;
        }

        return save.IsSkillUnlocked(skillConfigId);
    }

    /// <summary>读取技能等级（未解锁为 0）。</summary>
    public static int GetSkillLevel(SaveData save, string skillConfigId) =>
        save != null ? save.GetSkillLevel(skillConfigId) : 0;

    /// <summary>设置技能解锁状态与等级。</summary>
    public static void SetSkillUnlocked(SaveData save, string skillConfigId, bool unlocked, int level = 1)
    {
        if (save == null || string.IsNullOrWhiteSpace(skillConfigId))
        {
            return;
        }

        if (skillConfigId == GameConstants.ConfigIds.SkillShoot)
        {
            save.SetSkillLevel(skillConfigId, Mathf.Max(1, level));
            return;
        }

        if (!unlocked)
        {
            save.SetSkillLevel(skillConfigId, 0);
            return;
        }

        save.SetSkillLevel(skillConfigId, Mathf.Max(1, level));
    }

    /// <summary>Play 模式下将内存存档替换为测试数据并写盘。</summary>
    public static bool TryApplyToRunningSaveManager(SaveData data)
    {
        if (data == null || !ServiceLocator.TryGet(out SaveManager saveManager))
        {
            return false;
        }

        string json = JsonUtility.ToJson(data);
        if (!saveManager.ImportFromJson(json))
        {
            return false;
        }

        saveManager.SaveImmediate();

        if (ServiceLocator.TryGet(out SkillUnlockService unlockService))
        {
            unlockService.RefreshMetaUnlocks();
            unlockService.SyncPlayerSkillManagerToMeta();
        }

        return true;
    }

    /// <summary>按技能分组返回专属 SkillBuffKind 列表。</summary>
    public static IReadOnlyList<SkillBuffKind> GetExclusiveKindsForSkill(
        BuffMetaProgressCatalog catalog,
        SkillType skillType)
    {
        var result = new List<SkillBuffKind>(16);
        if (catalog == null)
        {
            return result;
        }

        IReadOnlyList<SkillBuffKind> all = catalog.SkillBuffKinds;
        for (int i = 0; i < all.Count; i++)
        {
            SkillBuffKind kind = all[i];
            if (BuffMetaProgressCatalog.IsGlobalKind(kind))
            {
                continue;
            }

            if (BuffMetaProgressCatalog.GetTargetSkill(kind) == skillType)
            {
                result.Add(kind);
            }
        }

        return result;
    }

    /// <summary>返回全部全局 SkillBuffKind。</summary>
    public static IReadOnlyList<SkillBuffKind> GetGlobalSkillBuffKinds(BuffMetaProgressCatalog catalog)
    {
        var result = new List<SkillBuffKind>(8);
        if (catalog == null)
        {
            return result;
        }

        IReadOnlyList<SkillBuffKind> all = catalog.SkillBuffKinds;
        for (int i = 0; i < all.Count; i++)
        {
            SkillBuffKind kind = all[i];
            if (BuffMetaProgressCatalog.IsGlobalKind(kind))
            {
                result.Add(kind);
            }
        }

        return result;
    }

    /// <summary>SkillType 显示名。</summary>
    public static string GetSkillTypeLabel(SkillType skillType) => skillType switch
    {
        SkillType.Shoot => "射击 Shoot",
        SkillType.Lightning => "闪电 Lightning",
        SkillType.Thunder => "落雷 Thunder",
        SkillType.FireRain => "火雨 FireRain",
        SkillType.WaterWave => "水浪 WaterWave",
        SkillType.Ice => "冰霜 Ice",
        SkillType.Heal => "治疗 Heal",
        _ => skillType.ToString(),
    };

    /// <summary>SkillBuffKind 显示名。</summary>
    public static string GetSkillBuffKindLabel(SkillBuffKind kind) => kind switch
    {
        SkillBuffKind.GlobalAttackSpeed => "全局攻速",
        SkillBuffKind.GlobalBaseDamage => "全局伤害",
        SkillBuffKind.GlobalCritChance => "全局暴击率",
        SkillBuffKind.GlobalCritDamage => "全局暴击伤害",
        SkillBuffKind.GlobalCooldownReduction => "全局冷却缩减",
        _ => kind.ToString(),
    };
}
