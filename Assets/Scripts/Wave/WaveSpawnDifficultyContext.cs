using UnityEngine;

/// <summary>
/// 当前波次难度快照运行时上下文（WaveManager 写入，EnemyController 读取）。
/// </summary>
public static class WaveSpawnDifficultyContext
{
    /// <summary>当前波次战斗属性乘算。</summary>
    public static float CombatMultiplier { get; private set; } = 1f;

    /// <summary>当前波次移速乘算（档内递增、跨档重置）。</summary>
    public static float MoveSpeedMultiplier { get; private set; } = 1f;

    /// <summary>视觉染色档位。</summary>
    public static int VisualTierIndex { get; private set; }

    /// <summary>视觉染色最大档位（用于归一化）。</summary>
    public static int VisualTierMax { get; private set; } = 12;

    /// <summary>写入难度快照。</summary>
    public static void Apply(WaveDifficultySnapshot snapshot)
    {
        CombatMultiplier = Mathf.Max(0.1f, snapshot.CombatMultiplier);
        MoveSpeedMultiplier = Mathf.Max(0.1f, snapshot.MoveSpeedMultiplier);
        VisualTierIndex = Mathf.Max(0, snapshot.VisualTierIndex);
    }

    /// <summary>写入完整快照并设置视觉上限。</summary>
    public static void Apply(WaveDifficultySnapshot snapshot, int visualTierMax)
    {
        Apply(snapshot);
        VisualTierMax = Mathf.Max(1, visualTierMax);
    }

    /// <summary>重置为默认。</summary>
    public static void Reset()
    {
        CombatMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        VisualTierIndex = 0;
        VisualTierMax = 12;
    }
}
