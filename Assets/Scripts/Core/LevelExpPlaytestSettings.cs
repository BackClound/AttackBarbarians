using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 试玩用等级经验门槛覆盖：可按等级设置固定所需经验，或对公式结果施加全局倍率。
/// 仅 Editor / Development Build 生效。
/// </summary>
public static class LevelExpPlaytestSettings
{
    /// <summary>编辑器窗口可编辑的最大等级。</summary>
    public const int MaxEditableLevel = 50;

    private static bool isEnabled;
    private static float globalMultiplier = 1f;
    private static readonly Dictionary<int, float> perLevelOverrides = new Dictionary<int, float>(16);

    /// <summary>当前构建是否允许等级经验测试（Editor / Development Build）。</summary>
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

    /// <summary>是否启用等级经验覆盖。</summary>
    public static bool IsEnabled => IsFeatureAvailable && isEnabled;

    /// <summary>全局倍率（无单级覆盖时作用于公式结果）。</summary>
    public static float GlobalMultiplier => Mathf.Max(0.01f, globalMultiplier);

    /// <summary>已配置的单级覆盖数量。</summary>
    public static int OverrideCount => perLevelOverrides.Count;

    /// <summary>启用或关闭等级经验覆盖。</summary>
    /// <param name="enabled">为 true 时覆盖生效。</param>
    public static void SetEnabled(bool enabled)
    {
        if (!IsFeatureAvailable)
        {
            return;
        }

        isEnabled = enabled;
        LogIfEnabled(enabled ? "等级经验覆盖已启用。" : "等级经验覆盖已关闭。");
    }

    /// <summary>设置全局倍率（自动钳制到 0.01~100）。</summary>
    /// <param name="multiplier">倍率；1 表示不缩放公式结果。</param>
    public static void SetGlobalMultiplier(float multiplier)
    {
        if (!IsFeatureAvailable)
        {
            return;
        }

        globalMultiplier = Mathf.Clamp(multiplier, 0.01f, 100f);
    }

    /// <summary>尝试读取指定等级的单级覆盖值。</summary>
    /// <param name="level">当前等级（升下一级所需经验对应该等级）。</param>
    /// <param name="needExp">覆盖后的所需经验。</param>
    /// <returns>存在有效覆盖时返回 true。</returns>
    public static bool TryGetLevelOverride(int level, out float needExp)
    {
        needExp = 0f;
        if (!IsFeatureAvailable || level < 1)
        {
            return false;
        }

        return perLevelOverrides.TryGetValue(level, out needExp) && needExp > 0f;
    }

    /// <summary>
    /// 设置指定等级的所需经验覆盖；传入 ≤0 则清除该级覆盖。
    /// </summary>
    /// <param name="level">当前等级。</param>
    /// <param name="needExp">所需经验；≤0 表示清除覆盖。</param>
    public static void SetLevelOverride(int level, float needExp)
    {
        if (!IsFeatureAvailable || level < 1)
        {
            return;
        }

        if (needExp <= 0f)
        {
            perLevelOverrides.Remove(level);
            return;
        }

        perLevelOverrides[level] = Mathf.Max(1f, needExp);
    }

    /// <summary>清除所有单级覆盖，保留启用状态与全局倍率。</summary>
    public static void ClearAllLevelOverrides()
    {
        if (!IsFeatureAvailable)
        {
            return;
        }

        perLevelOverrides.Clear();
        LogIfEnabled("已清除全部单级经验覆盖。");
    }

    /// <summary>遍历当前所有单级覆盖（只读）。</summary>
    public static IEnumerable<KeyValuePair<int, float>> GetAllLevelOverrides()
    {
        return perLevelOverrides;
    }

    /// <summary>
    /// 在公式计算结果之上应用试玩覆盖。
    /// </summary>
    /// <param name="calculatedNeed">公式得出的所需经验。</param>
    /// <param name="level">当前等级。</param>
    /// <returns>最终所需经验。</returns>
    public static float ResolveNeedExperience(float calculatedNeed, int level)
    {
        if (!IsEnabled)
        {
            return calculatedNeed;
        }

        level = Mathf.Max(1, level);
        if (perLevelOverrides.TryGetValue(level, out float custom) && custom > 0f)
        {
            return Mathf.Max(1f, custom);
        }

        if (!Mathf.Approximately(globalMultiplier, 1f))
        {
            return Mathf.Max(1f, calculatedNeed * GlobalMultiplier);
        }

        return calculatedNeed;
    }

    /// <summary>重置为默认（Bootstrap Shutdown 时调用）。</summary>
    public static void Reset()
    {
        isEnabled = false;
        globalMultiplier = 1f;
        perLevelOverrides.Clear();
    }

    /// <summary>在运行时日志开关开启时输出试玩信息。</summary>
    /// <param name="message">日志内容。</param>
    private static void LogIfEnabled(string message)
    {
        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            configManager.GameConfig == null ||
            !configManager.GameConfig.EnableRuntimeLogs)
        {
            return;
        }

        Debug.Log($"[LevelExpPlaytest] {message}");
    }
}
