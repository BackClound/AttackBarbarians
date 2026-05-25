using UnityEngine;

/// <summary>
/// 运行时调试日志门控：战斗高频路径应使用本类，避免默认 <see cref="Debug.Log"/> 产生 GC 与 I/O 开销。
/// </summary>
/// <remarks>
/// <para>开关来源：<see cref="GameConfig.EnableRuntimeLogs"/>（经 <see cref="PerformanceManager"/> 或 <see cref="ConfigManager"/> 同步）。</para>
/// </remarks>
public static class GameDebug
{
    private static bool runtimeLogsEnabled;

    public static bool RuntimeLogsEnabled => runtimeLogsEnabled;

    public static void SetRuntimeLogsEnabled(bool enabled) => runtimeLogsEnabled = enabled;

    public static void SyncFromConfig()
    {
        if (ServiceLocator.TryGet(out ConfigManager configManager) && configManager.GameConfig != null)
        {
            runtimeLogsEnabled = configManager.GameConfig.EnableRuntimeLogs;
            return;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance) && performance.IsInitialized)
        {
            runtimeLogsEnabled = performance.RuntimeLogsEnabled;
        }
    }

    public static void Log(string message, Object context = null)
    {
        if (!runtimeLogsEnabled)
        {
            return;
        }

        Debug.Log(message, context);
    }

    public static void LogWarning(string message, Object context = null)
    {
        if (!runtimeLogsEnabled)
        {
            return;
        }

        Debug.LogWarning(message, context);
    }

    public static void LogError(string message, Object context = null)
    {
        Debug.LogError(message, context);
    }
}
