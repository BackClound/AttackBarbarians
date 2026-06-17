using UnityEngine;

/// <summary>
/// 将 <see cref="UpgradeOptionSO"/> 效果应用到 Buff / Skill / 资源等系统。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="UpgradeManager"/> 调用。</para>
/// </remarks>
public static class UpgradeApplicator
{
    /// <summary>尝试将升级选项效果应用到对应系统。</summary>
    /// <param name="option">升级选项配置。</param>
    /// <param name="skillManager">玩家技能管理器。</param>
    /// <param name="saveManager">存档管理器。</param>
    /// <returns>应用成功返回 <c>true</c>。</returns>
    public static bool TryApply(UpgradeOptionSO option, PlayerSkillManager skillManager, SaveManager saveManager)
    {
        if (option == null)
        {
            return false;
        }

        switch (option.EffectType)
        {
            case UpgradeEffectType.StatBuff:
                return ApplyStatBuff(option, skillManager);

            case UpgradeEffectType.SkillBuff:
            case UpgradeEffectType.WeaponEnhance:
                return ApplySkillBuff(option, skillManager);

            case UpgradeEffectType.SkillUnlock:
                return ApplySkillUnlock(option, skillManager);

            case UpgradeEffectType.SkillLevelUp:
                return ApplySkillLevelUp(option, skillManager);

            case UpgradeEffectType.ResourceGold:
                return ApplyResource(saveManager, gold: option.ResourceAmount, diamonds: 0);

            case UpgradeEffectType.ResourceDiamond:
                return ApplyResource(saveManager, gold: 0, diamonds: option.ResourceAmount);

            default:
                Debug.LogWarning($"[UpgradeApplicator] 未支持的效果类型: {option.EffectType}");
                return false;
        }
    }

    /// <summary>应用属性 Buff 或直接属性修正。</summary>
    /// <param name="option">升级选项配置。</param>
    /// <param name="playerSkillManager">玩家技能管理器。</param>
    /// <returns>至少应用一项效果时返回 <c>true</c>。</returns>
    private static bool ApplyStatBuff(UpgradeOptionSO option, PlayerSkillManager playerSkillManager)
    {
        if (playerSkillManager == null)
        {
            return false;
        }

        bool applied = false;

        if (!string.IsNullOrWhiteSpace(option.BuffConfigId) &&
            ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.TryGetBuff(option.BuffConfigId, out BuffDataSO buff))
        {
            playerSkillManager.ApplyBuff(buff, option.BuffStacks);
            applied = true;
        }

        PlayerController controller = playerSkillManager.GetComponent<PlayerController>();
        if (controller != null && option.DirectModifiers != null)
        {
            for (int i = 0; i < option.DirectModifiers.Count; i++)
            {
                StatModifierConfig modifier = option.DirectModifiers[i];
                for (int s = 0; s < option.BuffStacks; s++)
                {
                    controller.ApplyModifier(modifier);
                }
            }

            applied = true;
        }

        return applied;
    }

    /// <summary>应用技能 Buff 或武器强化效果。</summary>
    /// <param name="option">升级选项配置。</param>
    /// <param name="skillManager">玩家技能管理器。</param>
    /// <returns>应用成功返回 <c>true</c>。</returns>
    private static bool ApplySkillBuff(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager == null || skillManager.SkillManager == null || option.SkillBuffKind == SkillBuffKind.None)
        {
            return false;
        }

        SkillType target = SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind);
        if (target != SkillType.None && !skillManager.SkillManager.IsSkillUnlocked(target))
        {
            Debug.LogWarning($"[UpgradeApplicator] 技能未解锁，无法应用专属 Buff: {option.SkillBuffKind} -> {target}");
            return false;
        }

        skillManager.ApplySkillBuff(option.SkillBuffKind, option.SkillBuffTier);
        return true;
    }

    /// <summary>解锁指定技能。</summary>
    /// <param name="option">升级选项配置。</param>
    /// <param name="skillManager">玩家技能管理器。</param>
    /// <returns>解锁成功返回 <c>true</c>。</returns>
    private static bool ApplySkillUnlock(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager?.SkillManager == null || string.IsNullOrWhiteSpace(option.SkillConfigId))
        {
            return false;
        }

        // 本系统的三选一为本局成长：技能解锁不写入元进度存档。
        skillManager.SkillManager.UnlockSkill(option.SkillConfigId, 1, persistToSave: false);
        return true;
    }

    /// <summary>提升指定技能等级。</summary>
    /// <param name="option">升级选项配置。</param>
    /// <param name="skillManager">玩家技能管理器。</param>
    /// <returns>升级成功返回 <c>true</c>。</returns>
    private static bool ApplySkillLevelUp(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager?.SkillManager == null || string.IsNullOrWhiteSpace(option.SkillConfigId))
        {
            return false;
        }

        return skillManager.SkillManager.UpgradeSkillLevel(option.SkillConfigId, option.SkillLevelDelta);
    }

    /// <summary>发放金币或钻石资源奖励。</summary>
    /// <param name="saveManager">存档管理器。</param>
    /// <param name="gold">金币数量。</param>
    /// <param name="diamonds">钻石数量。</param>
    /// <returns>至少发放一项资源时返回 <c>true</c>。</returns>
    private static bool ApplyResource(SaveManager saveManager, long gold, long diamonds)
    {
        if (saveManager?.Current == null)
        {
            return false;
        }

        bool applied = false;
        if (ServiceLocator.TryGet(out ResourceManager resourceManager))
        {
            if (gold > 0)
            {
                applied |= resourceManager.TryAdd(
                    CurrencyType.Gold,
                    gold,
                    ResourceChangeReason.UpgradeReward,
                    out _);
            }

            if (diamonds > 0)
            {
                applied |= resourceManager.TryAdd(
                    CurrencyType.Diamond,
                    diamonds,
                    ResourceChangeReason.UpgradeReward,
                    out _);
            }
        }
        else
        {
            if (gold > 0)
            {
                saveManager.Current.gold += gold;
                applied = true;
            }

            if (diamonds > 0)
            {
                saveManager.Current.diamonds += diamonds;
                applied = true;
            }
        }

        if (applied)
        {
            saveManager.MarkDirty();
        }

        return applied;
    }
}
