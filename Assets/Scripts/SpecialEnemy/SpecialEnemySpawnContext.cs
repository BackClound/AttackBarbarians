using UnityEngine;

/// <summary>
/// 当前局特殊敌人召唤/分裂用的波次与属性倍率上下文（由 <see cref="EnemySpawnerManager"/> 写入）。
/// </summary>
/// <remarks>纯静态上下文，无需挂载。</remarks>
public static class SpecialEnemySpawnContext
{
    public static int WaveIndex { get; private set; } = 1;
    public static float StatMultiplier { get; private set; } = 1f;

    public static void Set(int waveIndex, float statMultiplier)
    {
        WaveIndex = Mathf.Max(1, waveIndex);
        StatMultiplier = Mathf.Max(0.1f, statMultiplier);
    }

    public static void Reset()
    {
        WaveIndex = 1;
        StatMultiplier = 1f;
    }
}
