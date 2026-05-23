/// <summary>
/// 单局难度上下文：精英模式开关与配置引用（无挂载，由 Bootstrap 或主菜单写入）。
/// </summary>
public static class RunDifficultyContext
{
    public static bool IsEliteMode { get; set; }

    public static EliteModeConfigSO EliteConfig { get; set; }

    public static void Reset()
    {
        IsEliteMode = false;
        EliteConfig = null;
    }
}
