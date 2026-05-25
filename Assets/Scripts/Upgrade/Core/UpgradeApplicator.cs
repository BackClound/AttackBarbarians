using UnityEngine;

/// <summary>
/// 将 <see cref="UpgradeOptionSO"/> 效果应用到 Buff / Skill / 资源等系统。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="UpgradeManager"/> 调用。</para>
/// </remarks>
public static class UpgradeApplicator
{
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

    private static bool ApplySkillBuff(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager == null || option.SkillBuffKind == SkillBuffKind.None)
        {
            return false;
        }

        skillManager.ApplySkillBuff(option.SkillBuffKind, option.SkillBuffTier);
        return true;
    }

    private static bool ApplySkillUnlock(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager?.SkillManager == null || string.IsNullOrWhiteSpace(option.SkillConfigId))
        {
            return false;
        }

        skillManager.SkillManager.UnlockSkill(option.SkillConfigId, 1);
        return true;
    }

    private static bool ApplySkillLevelUp(UpgradeOptionSO option, PlayerSkillManager skillManager)
    {
        if (skillManager?.SkillManager == null || string.IsNullOrWhiteSpace(option.SkillConfigId))
        {
            return false;
        }

        return skillManager.SkillManager.UpgradeSkillLevel(option.SkillConfigId, option.SkillLevelDelta);
    }

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
