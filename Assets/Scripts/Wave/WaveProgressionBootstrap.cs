using UnityEngine;

/// <summary>
/// 从 <see cref="GameConfig"/> 与 <see cref="ConfigDatabaseSO"/> 初始化 <see cref="RunProgressionContext"/>。
/// </summary>
public static class WaveProgressionBootstrap
{
    /// <summary>从游戏配置加载波次成长开关与曲线资产。</summary>
    public static void ApplyFromGameConfig(GameConfig gameConfig, ConfigDatabaseSO database = null)
    {
        RunProgressionContext.Reset();

        if (gameConfig == null)
        {
            return;
        }

        WaveProgressionConfigSO config = gameConfig.WaveProgressionConfig;
        if (config == null && database != null)
        {
            config = database.WaveProgression;
        }

        if (config == null)
        {
            config = Resources.Load<WaveProgressionConfigSO>(GameConstants.ResourcePaths.WaveProgression);
        }

        RunProgressionContext.ApplySettings(gameConfig.UseWaveProgressionV2, config);

        if (gameConfig.EnableRuntimeLogs)
        {
            Debug.Log(
                $"[WaveProgressionBootstrap] V2={RunProgressionContext.UseWaveProgressionV2} " +
                $"config={(config != null ? config.name : "null")}");
        }
    }
}
