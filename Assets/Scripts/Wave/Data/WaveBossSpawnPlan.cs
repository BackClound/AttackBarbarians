using UnityEngine;

/// <summary>
/// 单波内一只 Boss 的生成计划（支持同波多 Boss、独立难度系数）。
/// </summary>
public sealed class WaveBossSpawnPlan
{
    /// <summary>Boss 配置 Id。</summary>
    public string BossConfigId { get; }

    /// <summary>相对波次全局属性的额外乘算。</summary>
    public float StatMultiplier { get; }

    /// <summary>相对波次开始的生成时间（秒）。</summary>
    public float SpawnAtElapsed { get; }

    /// <summary>构造单条 Boss 生成计划。</summary>
    public WaveBossSpawnPlan(string bossConfigId, float statMultiplier, float spawnAtElapsed)
    {
        BossConfigId = bossConfigId;
        StatMultiplier = Mathf.Max(0.1f, statMultiplier);
        SpawnAtElapsed = Mathf.Max(0f, spawnAtElapsed);
    }
}
