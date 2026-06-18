using UnityEngine;

/// <summary>
/// 从 <see cref="GameConfig"/> 初始化 <see cref="RunDifficultyContext"/>（精英模式与倍率表）。
/// </summary>
/// <remarks>纯静态工具，无需挂载。</remarks>
public static class RunDifficultyBootstrap
{
    /// <summary>
    /// 从游戏配置加载精英模式开关与配置引用。
    /// </summary>
    /// <param name="gameConfig">游戏全局配置。</param>
    /// <summary>从 GameConfig 写入单局难度上下文。</summary>
    /// <param name="gameConfig">游戏配置。</param>
    public static void ApplyFromGameConfig(GameConfig gameConfig)
    {
        RunDifficultyContext.Reset();

        if (gameConfig == null)
        {
            return;
        }

        EliteModeConfigSO eliteConfig = gameConfig.EliteModeConfig;
        if (eliteConfig == null)
        {
            eliteConfig = Resources.Load<EliteModeConfigSO>(GameConstants.ResourcePaths.EliteModeConfig);
        }

        RunDifficultyContext.EliteConfig = eliteConfig;
        RunDifficultyContext.IsEliteMode = gameConfig.StartWithEliteMode;
        RunDifficultyContext.GameDifficulty = ResolveGameDifficulty();

        if (gameConfig.EnableRuntimeLogs)
        {
            Debug.Log(
                $"[RunDifficultyBootstrap] EliteMode={RunDifficultyContext.IsEliteMode} " +
                $"config={(eliteConfig != null ? eliteConfig.name : "null")} " +
                $"difficulty={RunDifficultyContext.GameDifficulty}");
        }
    }

    private static int ResolveGameDifficulty()
    {
        if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current?.settings != null)
        {
            return UnityEngine.Mathf.Clamp(saveManager.Current.settings.gameDifficulty, 0, 2);
        }

        return 1;
    }
}
