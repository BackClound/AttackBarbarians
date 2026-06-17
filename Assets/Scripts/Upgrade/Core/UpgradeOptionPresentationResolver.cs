using UnityEngine;

/// <summary>
/// 三选一卡片展示数据：技能名、选项描述、图标。
/// </summary>
public readonly struct UpgradeOptionPresentation
{
    /// <summary>关联技能的 DisplayName（BuffNameText）。</summary>
    public string Name { get; }
    /// <summary>升级选项的 description（BuffDescriptionText）。</summary>
    public string Description { get; }
    public Sprite Icon { get; }

    public UpgradeOptionPresentation(string name, string description, Sprite icon)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Icon = icon;
    }
}

/// <summary>
/// 从 <see cref="UpgradeOptionSO"/> 及其关联技能配置解析三选一卡片展示字段。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>名称：<see cref="SkillDataSO.DisplayName"/>（无关联技能时回退选项名）。</description></item>
/// <item><description>描述：<see cref="UpgradeOptionSO.Description"/>。</description></item>
/// </list>
/// </remarks>
public static class UpgradeOptionPresentationResolver
{
    /// <summary>解析升级选项的展示名称、描述与图标。</summary>
    public static UpgradeOptionPresentation Resolve(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return new UpgradeOptionPresentation(string.Empty, string.Empty, null);
        }

        return new UpgradeOptionPresentation(
            ResolveSkillDisplayName(option),
            ResolveOptionDescription(option),
            ResolveIcon(option));
    }

    /// <summary>BuffNameText：优先显示关联技能的 DisplayName。</summary>
    private static string ResolveSkillDisplayName(UpgradeOptionSO option)
    {
        if (TryResolveSkillData(option, out SkillDataSO skill) && !string.IsNullOrWhiteSpace(skill.DisplayName))
        {
            return skill.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(option.DisplayName))
        {
            return option.DisplayName;
        }

        return option.ConfigId ?? string.Empty;
    }

    /// <summary>BuffDescriptionText：仅显示升级选项配置的 description。</summary>
    private static string ResolveOptionDescription(UpgradeOptionSO option)
    {
        return option.Description ?? string.Empty;
    }

    private static Sprite ResolveIcon(UpgradeOptionSO option)
    {
        if (option.Icon != null)
        {
            return option.Icon;
        }

        if (TryResolveSkillData(option, out SkillDataSO skill) && skill.Icon != null)
        {
            return skill.Icon;
        }

        if (TryResolveBuffData(option, out BuffDataSO buff) && buff.Icon != null)
        {
            return buff.Icon;
        }

        return null;
    }

    private static bool TryResolveSkillData(UpgradeOptionSO option, out SkillDataSO skill)
    {
        skill = null;
        if (!ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(option.SkillConfigId) &&
            configManager.TryGetSkill(option.SkillConfigId, out skill))
        {
            return true;
        }

        if (option.SkillBuffKind != SkillBuffKind.None)
        {
            SkillType target = SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind);
            if (target != SkillType.None &&
                TryGetSkillConfigId(target, out string configId) &&
                configManager.TryGetSkill(configId, out skill))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveBuffData(UpgradeOptionSO option, out BuffDataSO buff)
    {
        buff = null;
        if (string.IsNullOrWhiteSpace(option.BuffConfigId) ||
            !ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return false;
        }

        return configManager.TryGetBuff(option.BuffConfigId, out buff);
    }

    private static bool TryGetSkillConfigId(SkillType skillType, out string configId)
    {
        switch (skillType)
        {
            case SkillType.Shoot:
                configId = GameConstants.ConfigIds.SkillShoot;
                return true;
            case SkillType.Lightning:
                configId = GameConstants.ConfigIds.SkillLightning;
                return true;
            case SkillType.Thunder:
                configId = GameConstants.ConfigIds.SkillThunder;
                return true;
            case SkillType.FireRain:
                configId = GameConstants.ConfigIds.SkillFireRain;
                return true;
            case SkillType.WaterWave:
                configId = GameConstants.ConfigIds.SkillWaterWave;
                return true;
            case SkillType.Ice:
                configId = GameConstants.ConfigIds.SkillIce;
                return true;
            case SkillType.Heal:
                configId = GameConstants.ConfigIds.SkillHeal;
                return true;
            default:
                configId = null;
                return false;
        }
    }
}
