using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内 Buff 三选一池过滤：按本局/局外已解锁技能数决定候选范围，并处理专属 Buff tier 递进（含局外基线）。
/// </summary>
internal static class UpgradeRunPoolRules
{
    /// <summary>本局已解锁技能数低于此值时，池内包含技能解锁卡且三选一必出一张解锁卡。</summary>
    public const int RunSkillUnlockThreshold = 3;

    /// <summary>统计本局已解锁技能数量。</summary>
    public static int CountRunUnlockedSkills(SkillManager manager)
    {
        if (manager == null)
        {
            return 0;
        }

        int count = 0;
        for (int t = 0; t <= (int)SkillType.Heal; t++)
        {
            if (manager.IsSkillUnlocked((SkillType)t))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>是否为通用属性 Buff（可重复叠加，不走 tier 递进）。</summary>
    public static bool IsStatBuffOption(UpgradeOptionSO option) =>
        option != null && option.EffectType == UpgradeEffectType.StatBuff;

    /// <summary>是否为带 tier 的 SkillBuff（含全局与技能专属）。</summary>
    public static bool IsSkillBuffTierOption(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        return (option.EffectType == UpgradeEffectType.SkillBuff ||
                option.EffectType == UpgradeEffectType.WeaponEnhance) &&
               option.SkillBuffKind != SkillBuffKind.None;
    }

    /// <summary>是否为通用 Buff（属性 Buff 或 Global SkillBuffKind）。</summary>
    public static bool IsGlobalBuffOption(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        if (IsStatBuffOption(option))
        {
            return true;
        }

        return IsSkillBuffTierOption(option) && SkillBuffCatalog.IsGlobalKind(option.SkillBuffKind);
    }

    /// <summary>是否为基础属性 Buff 卡（StatBuff 或 Global SkillBuffKind，不含技能专属 Buff）。</summary>
    public static bool IsBasicAttributeBuffOption(UpgradeOptionSO option) => IsGlobalBuffOption(option);

    /// <summary>强制属性循环池：仅 StatBuff / Global 属性 Buff，排除技能专属与解锁卡。</summary>
    public static bool IsForcedStatCycleBuffOption(UpgradeOptionSO option) =>
        IsBasicAttributeBuffOption(option) && !IsSkillExclusiveBuffOption(option) && !IsSkillUnlockOption(option);

    /// <summary>是否为技能专属 Buff（非 Global 的 SkillBuff）。</summary>
    public static bool IsSkillExclusiveBuffOption(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        return IsSkillBuffTierOption(option) && !SkillBuffCatalog.IsGlobalKind(option.SkillBuffKind);
    }

    /// <summary>是否为技能解锁卡。</summary>
    public static bool IsSkillUnlockOption(UpgradeOptionSO option) =>
        option != null && option.EffectType == UpgradeEffectType.SkillUnlock;

    /// <summary>是否属于局内 Buff 三选一主池（排除金币等资源类）。</summary>
    public static bool IsBuffPoolCandidate(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        switch (option.EffectType)
        {
            case UpgradeEffectType.StatBuff:
            case UpgradeEffectType.SkillBuff:
            case UpgradeEffectType.WeaponEnhance:
            case UpgradeEffectType.SkillUnlock:
                return true;
            default:
                return false;
        }
    }

    /// <summary>获取某 SkillBuffKind 当前已选的最高 tier（未选过为 0）。</summary>
    public static int GetHighestSelectedTier(
        SkillBuffKind kind,
        IReadOnlyDictionary<SkillBuffKind, int> skillBuffHighestTiers)
    {
        if (kind == SkillBuffKind.None || skillBuffHighestTiers == null)
        {
            return 0;
        }

        return skillBuffHighestTiers.TryGetValue(kind, out int tier) ? tier : 0;
    }

    /// <summary>
    /// 是否为当前 Kind 的下一档 tier（局外基线 + 局内已选后的下一档，例如局外 T2 则局内从 T3 起）。
    /// </summary>
    public static bool IsNextTierOption(
        UpgradeOptionSO option,
        IReadOnlyDictionary<SkillBuffKind, int> skillBuffHighestTiers)
    {
        if (!IsSkillBuffTierOption(option))
        {
            return true;
        }

        int highest = GetHighestSelectedTier(option.SkillBuffKind, skillBuffHighestTiers);
        return option.SkillBuffTier == highest + 1;
    }

    /// <summary>专属 Buff 所属技能是否满足池过滤（局外≥3 技能时按局外解锁，否则按局内解锁）。</summary>
    public static bool IsExclusiveSkillUnlockedForPool(
        SkillType target,
        SkillManager manager,
        int metaUnlockedCount,
        SkillUnlockService unlockService,
        SaveData save)
    {
        if (target == SkillType.None)
        {
            return false;
        }

        if (metaUnlockedCount >= RunSkillUnlockThreshold)
        {
            return MetaSkillBuffProgressResolver.IsSkillMetaUnlocked(target, unlockService, save);
        }

        return manager != null && manager.IsSkillUnlocked(target);
    }

    /// <summary>
    /// 判断选项是否可进入当前局内 Buff 池。
    /// </summary>
    public static bool IsEligibleForRunPool(
        UpgradeOptionSO option,
        UpgradeSelectionContext context,
        SkillManager manager,
        int runUnlockedCount,
        int metaUnlockedCount,
        SkillUnlockService unlockService,
        SaveData save,
        IReadOnlyDictionary<SkillBuffKind, int> skillBuffHighestTiers,
        System.Func<UpgradeOptionSO, UpgradeSelectionContext, bool> isBaseOptionAvailable)
    {
        if (option == null || !IsBuffPoolCandidate(option))
        {
            return false;
        }

        if (isBaseOptionAvailable != null && !isBaseOptionAvailable(option, context))
        {
            return false;
        }

        if (IsSkillUnlockOption(option))
        {
            // 局内已满 3 技能，或局外已满 3 技能：不再出现解锁卡。
            if (runUnlockedCount >= RunSkillUnlockThreshold ||
                metaUnlockedCount >= RunSkillUnlockThreshold)
            {
                return false;
            }

            return manager == null ||
                   string.IsNullOrWhiteSpace(option.SkillConfigId) ||
                   !manager.IsSkillUnlocked(option.SkillConfigId);
        }

        if (IsStatBuffOption(option))
        {
            return true;
        }

        if (IsSkillBuffTierOption(option))
        {
            if (!IsNextTierOption(option, skillBuffHighestTiers))
            {
                return false;
            }

            if (IsSkillExclusiveBuffOption(option))
            {
                SkillType target = SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind);
                return IsExclusiveSkillUnlockedForPool(
                    target,
                    manager,
                    metaUnlockedCount,
                    unlockService,
                    save);
            }

            return true;
        }

        return false;
    }
}
