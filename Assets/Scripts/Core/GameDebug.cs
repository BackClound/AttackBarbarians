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

    /// <summary>当前是否允许输出 Log/LogWarning。</summary>
    public static bool RuntimeLogsEnabled => runtimeLogsEnabled;

    /// <summary>手动设置运行时日志开关。</summary>
    /// <param name="enabled">为 true 时允许 Log/LogWarning。</param>
    public static void SetRuntimeLogsEnabled(bool enabled) => runtimeLogsEnabled = enabled;

    /// <summary>从 <see cref="ConfigManager"/> 或 <see cref="PerformanceManager"/> 同步日志开关。</summary>
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

    /// <summary>受门控的 Debug.Log，关闭时不产生字符串分配。</summary>
    /// <param name="message">日志内容。</param>
    /// <param name="context">可选 Unity 上下文对象。</param>
    public static void Log(string message, Object context = null)
    {
        if (!runtimeLogsEnabled)
        {
            return;
        }

        Debug.Log(message, context);
    }

    /// <summary>受门控的 Debug.LogWarning。</summary>
    /// <param name="message">警告内容。</param>
    /// <param name="context">可选 Unity 上下文对象。</param>
    public static void LogWarning(string message, Object context = null)
    {
        if (!runtimeLogsEnabled)
        {
            return;
        }

        Debug.LogWarning(message, context);
    }

    /// <summary>始终输出的 Debug.LogError（不受门控影响）。</summary>
    /// <param name="message">错误内容。</param>
    /// <param name="context">可选 Unity 上下文对象。</param>
    public static void LogError(string message, Object context = null)
    {
        Debug.LogError(message, context);
    }
}
