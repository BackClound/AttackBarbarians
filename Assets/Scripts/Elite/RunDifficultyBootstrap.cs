using UnityEngine;

/// <summary>
/// 从 <see cref="GameConfig"/> 初始化 <see cref="RunDifficultyContext"/>（精英模式与倍率表）。
/// </summary>
/// <remarks>纯静态工具，无需挂载。</remarks>
public static class RunDifficultyBootstrap
{
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

        if (gameConfig.EnableRuntimeLogs)
        {
            Debug.Log(
                $"[RunDifficultyBootstrap] EliteMode={RunDifficultyContext.IsEliteMode} " +
                $"config={(eliteConfig != null ? eliteConfig.name : "null")}");
        }
    }
}
