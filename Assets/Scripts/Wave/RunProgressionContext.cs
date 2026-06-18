using UnityEngine;

/// <summary>
/// 局内波次成长运行时上下文（由 Bootstrap / WaveManager 写入）。
/// </summary>
public static class RunProgressionContext
{
    /// <summary>当前波次序号。</summary>
    public static int CurrentWave { get; private set; } = 1;

    /// <summary>波次成长曲线配置。</summary>
    public static WaveProgressionConfigSO Config { get; private set; }

    /// <summary>是否启用 V2 波次成长（否则回退线性 statScalePerWave）。</summary>
    public static bool UseWaveProgressionV2 { get; private set; }

    /// <summary>V2 曲线是否可用。</summary>
    public static bool IsActive => UseWaveProgressionV2 && Config != null;

    /// <summary>应用 Bootstrap 设置。</summary>
    public static void ApplySettings(bool useV2, WaveProgressionConfigSO config)
    {
        UseWaveProgressionV2 = useV2;
        Config = config;
    }

    /// <summary>由 WaveManager 在每波开始时更新。</summary>
    public static void SetCurrentWave(int wave)
    {
        CurrentWave = Mathf.Max(1, wave);
    }

    /// <summary>重置为默认状态。</summary>
    public static void Reset()
    {
        CurrentWave = 1;
        Config = null;
        UseWaveProgressionV2 = false;
    }
}
