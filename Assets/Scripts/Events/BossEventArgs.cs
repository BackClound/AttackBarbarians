using UnityEngine;

/// <summary>
/// Boss 出现事件负载。
/// </summary>
public readonly struct BossSpawnedEventArgs
{
    /// <summary>Boss 配置 ID。</summary>
    public string BossConfigId { get; }
    /// <summary>Boss GameObject。</summary>
    public GameObject BossObject { get; }
    /// <summary>Boss 阶段总数。</summary>
    public int PhaseCount { get; }
    /// <summary>最大生命值。</summary>
    public float MaxHp { get; }

    /// <summary>构造 <see cref="BossSpawnedEventArgs"/>。</summary>
    /// <param name="bossConfigId">Boss 配置 ID。</param>
    /// <param name="bossObject">Boss GameObject。</param>
    /// <param name="phaseCount">Boss 阶段总数。</param>
    /// <param name="maxHp">最大生命值。</param>
    public BossSpawnedEventArgs(string bossConfigId, GameObject bossObject, int phaseCount, float maxHp)
    {
        BossConfigId = bossConfigId ?? string.Empty;
        BossObject = bossObject;
        PhaseCount = Mathf.Max(1, phaseCount);
        MaxHp = Mathf.Max(1f, maxHp);
    }
}

/// <summary>
/// Boss 阶段切换事件负载。
/// </summary>
public readonly struct BossPhaseChangedEventArgs
{
    /// <summary>Boss 配置 ID。</summary>
    public string BossConfigId { get; }
    /// <summary>Boss GameObject。</summary>
    public GameObject BossObject { get; }
    /// <summary>切换前的阶段索引。</summary>
    public int PreviousPhaseIndex { get; }
    /// <summary>切换后的阶段索引。</summary>
    public int NewPhaseIndex { get; }
    /// <summary>当前 HP 比例（0~1）。</summary>
    public float HpRatio { get; }

    /// <summary>构造 <see cref="BossPhaseChangedEventArgs"/>。</summary>
    /// <param name="bossConfigId">Boss 配置 ID。</param>
    /// <param name="bossObject">Boss GameObject。</param>
    /// <param name="previousPhaseIndex">切换前的阶段索引。</param>
    /// <param name="newPhaseIndex">切换后的阶段索引。</param>
    /// <param name="hpRatio">当前 HP 比例（0~1）。</param>
    public BossPhaseChangedEventArgs(
        string bossConfigId,
        GameObject bossObject,
        int previousPhaseIndex,
        int newPhaseIndex,
        float hpRatio)
    {
        BossConfigId = bossConfigId ?? string.Empty;
        BossObject = bossObject;
        PreviousPhaseIndex = previousPhaseIndex;
        NewPhaseIndex = newPhaseIndex;
        HpRatio = Mathf.Clamp01(hpRatio);
    }
}

/// <summary>
/// Boss 被击败事件负载（奖励与统计入口）。
/// </summary>
public readonly struct BossDefeatedEventArgs
{
    /// <summary>Boss 配置 ID。</summary>
    public string BossConfigId { get; }
    /// <summary>Boss GameObject。</summary>
    public GameObject BossObject { get; }
    /// <summary>世界坐标位置。</summary>
    public Vector3 Position { get; }
    /// <summary>击杀者对象。</summary>
    public object Killer { get; }
    /// <summary>击败奖励经验。</summary>
    public int BonusExperience { get; }
    /// <summary>掉落表 ID。</summary>
    public string DropTableId { get; }
    /// <summary>最终阶段索引。</summary>
    public int FinalPhaseIndex { get; }

    /// <summary>构造 <see cref="BossDefeatedEventArgs"/>。</summary>
    /// <param name="bossConfigId">Boss 配置 ID。</param>
    /// <param name="bossObject">Boss GameObject。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="killer">击杀者对象。</param>
    /// <param name="bonusExperience">击败奖励经验。</param>
    /// <param name="dropTableId">掉落表 ID。</param>
    /// <param name="finalPhaseIndex">最终阶段索引。</param>
    public BossDefeatedEventArgs(
        string bossConfigId,
        GameObject bossObject,
        Vector3 position,
        object killer,
        int bonusExperience,
        string dropTableId,
        int finalPhaseIndex)
    {
        BossConfigId = bossConfigId ?? string.Empty;
        BossObject = bossObject;
        Position = position;
        Killer = killer;
        BonusExperience = Mathf.Max(0, bonusExperience);
        DropTableId = dropTableId ?? string.Empty;
        FinalPhaseIndex = finalPhaseIndex;
    }
}

/// <summary>
/// 精英敌人出现事件负载。
/// </summary>
public readonly struct EliteSpawnedEventArgs
{
    /// <summary>敌人 GameObject。</summary>
    public GameObject EnemyObject { get; }
    /// <summary>敌人配置 ID。</summary>
    public string EnemyConfigId { get; }
    /// <summary>精英模式是否激活。</summary>
    public bool EliteModeActive { get; }

    /// <summary>构造 <see cref="EliteSpawnedEventArgs"/>。</summary>
    /// <param name="enemyObject">敌人 GameObject。</param>
    /// <param name="enemyConfigId">敌人配置 ID。</param>
    /// <param name="eliteModeActive">精英模式是否激活。</param>
    public EliteSpawnedEventArgs(GameObject enemyObject, string enemyConfigId, bool eliteModeActive)
    {
        EnemyObject = enemyObject;
        EnemyConfigId = enemyConfigId ?? string.Empty;
        EliteModeActive = eliteModeActive;
    }
}
