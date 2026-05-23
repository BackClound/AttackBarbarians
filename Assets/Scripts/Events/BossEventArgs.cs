using UnityEngine;

/// <summary>
/// Boss 出现事件负载。
/// </summary>
public readonly struct BossSpawnedEventArgs
{
    public string BossConfigId { get; }
    public GameObject BossObject { get; }
    public int PhaseCount { get; }
    public float MaxHp { get; }

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
    public string BossConfigId { get; }
    public GameObject BossObject { get; }
    public int PreviousPhaseIndex { get; }
    public int NewPhaseIndex { get; }
    public float HpRatio { get; }

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
    public string BossConfigId { get; }
    public GameObject BossObject { get; }
    public Vector3 Position { get; }
    public object Killer { get; }
    public int BonusExperience { get; }
    public string DropTableId { get; }
    public int FinalPhaseIndex { get; }

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
    public GameObject EnemyObject { get; }
    public string EnemyConfigId { get; }
    public bool EliteModeActive { get; }

    public EliteSpawnedEventArgs(GameObject enemyObject, string enemyConfigId, bool eliteModeActive)
    {
        EnemyObject = enemyObject;
        EnemyConfigId = enemyConfigId ?? string.Empty;
        EliteModeActive = eliteModeActive;
    }
}
