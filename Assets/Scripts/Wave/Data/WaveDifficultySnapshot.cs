using UnityEngine;

/// <summary>
/// 单波难度快照：战斗属性乘算、档内移速乘算与视觉档位。
/// </summary>
public struct WaveDifficultySnapshot
{
    /// <summary>0-based 难度档（每 20 波一档）。</summary>
    public int TierIndex;

    /// <summary>档内位置 0..tierSize-1。</summary>
    public int PositionInTier;

    /// <summary>HP / 攻击 / 护甲等战斗属性乘算。</summary>
    public float CombatMultiplier;

    /// <summary>移速乘算（档内递增，跨档重置为初始值）。</summary>
    public float MoveSpeedMultiplier;

    /// <summary>用于外观染色的档位索引。</summary>
    public int VisualTierIndex;

    /// <summary>无修正。</summary>
    public static WaveDifficultySnapshot Identity => new WaveDifficultySnapshot
    {
        TierIndex = 0,
        PositionInTier = 0,
        CombatMultiplier = 1f,
        MoveSpeedMultiplier = 1f,
        VisualTierIndex = 0
    };
}
