/// <summary>
/// 单局难度上下文：精英模式开关与配置引用（无挂载，由 Bootstrap 或主菜单写入）。
/// </summary>
public static class RunDifficultyContext
{
    /// <summary>当前局是否启用精英模式。</summary>
    public static bool IsEliteMode { get; set; }

    /// <summary>精英模式配置引用。</summary>
    public static EliteModeConfigSO EliteConfig { get; set; }

    /// <summary>存档难度档位（0 简单 / 1 普通 / 2 困难）。</summary>
    public static int GameDifficulty { get; set; } = 1;

    /// <summary>敌人属性难度乘算（不含精英模式，精英由 EliteModeConfigSO 叠加）。</summary>
    public static float EnemyStatDifficultyMult =>
        GameDifficulty switch
        {
            0 => 0.90f,
            2 => 1.12f,
            _ => 1f
        };

    /// <summary>升级所需经验难度乘算。</summary>
    public static float ExpNeedDifficultyMult
    {
        get
        {
            float mult = GameDifficulty switch
            {
                0 => 0.95f,
                2 => 1.05f,
                _ => 1f
            };

            if (IsEliteMode)
            {
                mult *= 1.05f;
            }

            return mult;
        }
    }

    /// <summary>击杀经验获得难度乘算。</summary>
    public static float ExpGainDifficultyMult
    {
        get
        {
            float mult = GameDifficulty switch
            {
                0 => 1.10f,
                2 => 0.95f,
                _ => 1f
            };

            if (IsEliteMode)
            {
                mult *= 1.05f;
            }

            return mult;
        }
    }

    /// <summary>重置为默认非精英状态。</summary>
    public static void Reset()
    {
        IsEliteMode = false;
        EliteConfig = null;
        GameDifficulty = 1;
    }
}
