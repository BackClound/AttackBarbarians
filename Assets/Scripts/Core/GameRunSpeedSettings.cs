using UnityEngine;

/// <summary>
/// 测试用运行倍速：通过 <see cref="Time.timeScale"/> 快进，不改变数值策略本身。
/// </summary>
public static class GameRunSpeedSettings
{
    /// <summary>最小倍速（正常）。</summary>
    public const float MinMultiplier = 1f;

    /// <summary>最大倍速（测试快进上限）。</summary>
    public const float MaxMultiplier = 5f;

    private static float playSpeedMultiplier = MinMultiplier;

    /// <summary>当前运行倍速（1~5）。</summary>
    public static float PlaySpeedMultiplier => playSpeedMultiplier;

    /// <summary>设置运行倍速并立即应用到 Unity 时间（若当前状态允许）。</summary>
    /// <param name="multiplier">目标倍速，自动钳制到 1~5。</param>
    public static void SetPlaySpeedMultiplier(float multiplier)
    {
        float clamped = Mathf.Clamp(multiplier, MinMultiplier, MaxMultiplier);
        if (Mathf.Approximately(playSpeedMultiplier, clamped))
        {
            return;
        }

        playSpeedMultiplier = clamped;
        ApplyToUnityTimeScale(ResolveCurrentState());
        LogIfEnabled($"运行倍速 → {playSpeedMultiplier:0.#}x (timeScale={Time.timeScale:0.##})");
    }

    /// <summary>根据游戏状态解析应写入的 <see cref="Time.timeScale"/>。</summary>
    public static float ResolveTimeScale(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.UpgradeChoosing:
            case GameState.GameOver:
                return 0f;
            default:
                return playSpeedMultiplier;
        }
    }

    /// <summary>将倍速应用到 Unity 全局时间缩放。</summary>
    public static void ApplyToUnityTimeScale(GameState state)
    {
        Time.timeScale = ResolveTimeScale(state);
    }

    /// <summary>重置为 1 倍并恢复 timeScale（Shutdown 时调用）。</summary>
    public static void Reset()
    {
        playSpeedMultiplier = MinMultiplier;
        Time.timeScale = 1f;
    }

    /// <summary>从 GameManager 解析当前游戏状态；未就绪时返回 Bootstrapping。</summary>
    /// <returns>当前 <see cref="GameState"/>。</returns>
    private static GameState ResolveCurrentState()
    {
        if (ServiceLocator.TryGet(out GameManager gameManager) && gameManager.IsInitialized)
        {
            return gameManager.CurrentState;
        }

        return GameState.Bootstrapping;
    }

    /// <summary>在运行时日志开关开启时输出倍速变更信息。</summary>
    /// <param name="message">日志正文。</param>
    private static void LogIfEnabled(string message)
    {
        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            configManager.GameConfig == null ||
            !configManager.GameConfig.EnableRuntimeLogs)
        {
            return;
        }

        Debug.Log($"[GameRunSpeed] {message}");
    }
}
