using UnityEngine;

/// <summary>
/// 当前局特殊敌人召唤/分裂用的波次与属性倍率上下文（由 <see cref="EnemySpawnerManager"/> 写入）。
/// </summary>
/// <remarks>纯静态上下文，无需挂载。</remarks>
public static class SpecialEnemySpawnContext
{
    /// <summary>当前波次序号。</summary>
    public static int WaveIndex { get; private set; } = 1;
    /// <summary>当前属性倍率。</summary>
    public static float StatMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 设置召唤/分裂上下文。
    /// </summary>
    /// <param name="waveIndex">波次序号。</param>
    /// <param name="statMultiplier">属性倍率。</param>
    public static void Set(int waveIndex, float statMultiplier)
    {
        WaveIndex = Mathf.Max(1, waveIndex);
        StatMultiplier = Mathf.Max(0.1f, statMultiplier);
    }

    /// <summary>重置为默认值。</summary>
    /// <summary>重置为默认波次与倍率。</summary>
    public static void Reset()
    {
        WaveIndex = 1;
        StatMultiplier = 1f;
    }
}
