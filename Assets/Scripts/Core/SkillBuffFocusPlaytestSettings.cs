using UnityEngine;

/// <summary>
/// 试玩用技能 Buff 专项测试：开启后局内三选一仅出现指定技能的专属 Buff 与通用 Buff。
/// 仅 Editor / Development Build 生效。
/// </summary>
public static class SkillBuffFocusPlaytestSettings
{
    private static bool isEnabled;
    private static SkillType focusedSkill = SkillType.None;

    /// <summary>是否已开启技能专项测试。</summary>
    public static bool IsEnabled => IsFeatureAvailable && isEnabled;

    /// <summary>当前聚焦的技能类型；未开启时为 <see cref="SkillType.None"/>。</summary>
    public static SkillType FocusedSkillType => IsFeatureAvailable ? focusedSkill : SkillType.None;

    /// <summary>专项测试是否正在过滤升级池。</summary>
    public static bool IsActive => IsEnabled && focusedSkill != SkillType.None;

    /// <summary>当前构建是否允许专项测试（Editor / Development Build）。</summary>
    public static bool IsFeatureAvailable
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>开启专项测试并聚焦指定技能；会自动解锁该技能（不写存档）。</summary>
    /// <param name="skillType">要测试的技能类型。</param>
    public static void EnableFocus(SkillType skillType)
    {
        if (!IsFeatureAvailable || skillType == SkillType.None)
        {
            return;
        }

        isEnabled = true;
        focusedSkill = skillType;
        TryUnlockFocusedSkill();
        LogIfEnabled($"专项测试 → {GetDisplayName(skillType)}（仅该技能 Buff + 通用 Buff）");
    }

    /// <summary>关闭专项测试，恢复默认三选一池规则。</summary>
    public static void Disable()
    {
        if (!IsFeatureAvailable)
        {
            return;
        }

        isEnabled = false;
        focusedSkill = SkillType.None;
        LogIfEnabled("专项测试已关闭，恢复默认 Buff 池。");
    }

    /// <summary>重置为默认（Bootstrap Shutdown 时调用）。</summary>
    public static void Reset()
    {
        isEnabled = false;
        focusedSkill = SkillType.None;
    }

    /// <summary>判断升级选项是否可通过专项测试过滤。</summary>
    /// <param name="option">升级选项。</param>
    /// <returns>允许进入候选池时返回 true。</returns>
    public static bool PassesFocusFilter(UpgradeOptionSO option)
    {
        if (!IsActive || option == null)
        {
            return true;
        }

        if (UpgradeRunPoolRules.IsSkillUnlockOption(option))
        {
            return false;
        }

        if (UpgradeRunPoolRules.IsGlobalBuffOption(option))
        {
            return true;
        }

        if (UpgradeRunPoolRules.IsSkillExclusiveBuffOption(option))
        {
            SkillType target = SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind);
            return target == focusedSkill;
        }

        return true;
    }

    /// <summary>获取技能类型的中文展示名。</summary>
    /// <param name="skillType">技能类型。</param>
    /// <returns>展示名。</returns>
    public static string GetDisplayName(SkillType skillType) => skillType switch
    {
        SkillType.Shoot => "射击",
        SkillType.FireRain => "火雨",
        SkillType.Ice => "冰霜",
        SkillType.Lightning => "闪电",
        SkillType.Thunder => "落雷",
        SkillType.WaterWave => "水浪",
        SkillType.Heal => "治疗",
        _ => skillType.ToString(),
    };

    /// <summary>将技能类型解析为默认配置 ID。</summary>
    /// <param name="skillType">技能类型。</param>
    /// <returns>配置 ID；无效时返回 null。</returns>
    public static string ResolveSkillConfigId(SkillType skillType) => skillType switch
    {
        SkillType.Shoot => GameConstants.ConfigIds.SkillShoot,
        SkillType.FireRain => GameConstants.ConfigIds.SkillFireRain,
        SkillType.Ice => GameConstants.ConfigIds.SkillIce,
        SkillType.Lightning => GameConstants.ConfigIds.SkillLightning,
        SkillType.Thunder => GameConstants.ConfigIds.SkillThunder,
        SkillType.WaterWave => GameConstants.ConfigIds.SkillWaterWave,
        SkillType.Heal => GameConstants.ConfigIds.SkillHeal,
        _ => null,
    };

    /// <summary>确保聚焦技能在本局已解锁，便于立即测试 Buff 效果。</summary>
    private static void TryUnlockFocusedSkill()
    {
        string configId = ResolveSkillConfigId(focusedSkill);
        if (string.IsNullOrWhiteSpace(configId))
        {
            return;
        }

        SkillManager skillManager = ResolveSkillManager();
        if (skillManager == null)
        {
            Debug.LogWarning(
                $"[SkillBuffFocus] 未找到 SkillManager，无法自动解锁 {GetDisplayName(focusedSkill)}。");
            return;
        }

        if (!skillManager.IsSkillUnlocked(focusedSkill))
        {
            skillManager.UnlockSkill(configId, 1, persistToSave: false);
        }
    }

    /// <summary>解析场景中的 <see cref="SkillManager"/>。</summary>
    private static SkillManager ResolveSkillManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            return Player.Instance.skillManager.SkillManager;
        }

        PlayerSkillManager playerSkills = Player.HasInstance
            ? Player.Instance.skillManager
            : Object.FindFirstObjectByType<PlayerSkillManager>();
        return playerSkills != null ? playerSkills.SkillManager : Object.FindFirstObjectByType<SkillManager>();
    }

    /// <summary>在运行时日志开启时输出专项测试信息。</summary>
    /// <param name="message">日志内容。</param>
    private static void LogIfEnabled(string message)
    {
        if (ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.GameConfig != null &&
            configManager.GameConfig.EnableRuntimeLogs)
        {
            Debug.Log($"[SkillBuffFocus] {message}");
        }
    }
}
