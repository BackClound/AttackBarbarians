/// <summary>
/// 单局难度上下文：精英模式开关与配置引用（无挂载，由 Bootstrap 或主菜单写入）。
/// </summary>
public static class RunDifficultyContext
{
    /// <summary>当前局是否启用精英模式。</summary>
    public static bool IsEliteMode { get; set; }

    /// <summary>精英模式配置引用。</summary>
    public static EliteModeConfigSO EliteConfig { get; set; }

    /// <summary>重置为默认非精英状态。</summary>
    public static void Reset()
    {
        IsEliteMode = false;
        EliteConfig = null;
    }
}
