using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时解析后的单波刷怪快照，与 ScriptableObject 资产解耦。
/// </summary>
public sealed class WaveSpawnProfile
{
    private static readonly IReadOnlyList<WaveEnemyEntry> EmptyEntries = new List<WaveEnemyEntry>(0);
    private static readonly IReadOnlyList<string> EmptyIds = new List<string>(0);

    /// <summary>当前波次序号。</summary>
    public int WaveIndex { get; private set; } = 1;

    /// <summary>加权敌人条目（优先于 EnemyConfigIds）。</summary>
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries { get; private set; } = EmptyEntries;

    /// <summary>旧式敌人 configId 列表（按 SpawnWeight 构建池）。</summary>
    public IReadOnlyList<string> EnemyConfigIds { get; private set; } = EmptyIds;

    private static readonly IReadOnlyList<WaveBossSpawnPlan> EmptyBossPlans = new List<WaveBossSpawnPlan>(0);

    /// <summary>本波 Boss 生成计划（程序化多 Boss）。</summary>
    public IReadOnlyList<WaveBossSpawnPlan> BossSpawnPlans { get; private set; } = EmptyBossPlans;

    /// <summary>本波是否生成 Boss。</summary>
    public bool HasBoss { get; private set; }

    /// <summary>本波 Boss configId。</summary>
    public string BossConfigId { get; private set; }

    /// <summary>Boss 出现时间点（秒）。</summary>
    public float BossSpawnAtElapsed { get; private set; }

    /// <summary>是否必须击败 Boss 才能完成波次。</summary>
    public bool RequireBossDefeatToComplete { get; private set; } = true;

    /// <summary>Boss 存活时是否暂停普通刷怪。</summary>
    public bool PauseNormalSpawnsWhileBossAlive { get; private set; }

    /// <summary>精英怪基础概率（0~1）。</summary>
    public float EliteSpawnChance { get; private set; }

    /// <summary>特殊怪基础概率（0~1）。</summary>
    public float SpecialSpawnChance { get; private set; }

    /// <summary>特殊敌人 configId 池。</summary>
    public IReadOnlyList<string> SpecialEnemyConfigIds { get; private set; } = EmptyIds;

    /// <summary>波次奖励表 Id。</summary>
    public string RewardTableId { get; private set; }

    /// <summary>难度修正（叠加在全局成长曲线之上）。</summary>
    public WaveDifficultyOverride Difficulty { get; private set; } = WaveDifficultyOverride.Identity;

    /// <summary>程序化难度快照（战斗/移速/视觉分轨）。</summary>
    public WaveDifficultySnapshot DifficultySnapshot { get; private set; } = WaveDifficultySnapshot.Identity;

    /// <summary>Legacy：静态波次时长（V2 未启用时使用）。</summary>
    public float LegacyWaveDuration { get; private set; } = GameConstants.Progression.WaveDurationSeconds;

    /// <summary>Legacy：静态刷怪间隔。</summary>
    public float LegacySpawnInterval { get; private set; } = 1.5f;

    /// <summary>Legacy：静态单波最大刷怪数。</summary>
    public int LegacyMaxSpawnCount { get; private set; } = 20;

    /// <summary>Legacy：每波线性属性缩放系数。</summary>
    public float LegacyStatScalePerWave { get; private set; } = 0.08f;

    /// <summary>从 Legacy <see cref="WaveDataSO"/> 构建快照。</summary>
    public static WaveSpawnProfile FromWaveData(WaveDataSO data, int waveIndex)
    {
        WaveSpawnProfile profile = new WaveSpawnProfile();
        if (data == null)
        {
            profile.WaveIndex = Mathf.Max(1, waveIndex);
            return profile;
        }

        profile.WaveIndex = Mathf.Max(1, waveIndex);
        profile.EnemyEntries = data.EnemyEntries ?? EmptyEntries;
        profile.EnemyConfigIds = data.EnemyConfigIds ?? EmptyIds;
        profile.HasBoss = data.HasBoss;
        profile.BossConfigId = data.BossConfigId;
        profile.BossSpawnAtElapsed = data.BossSpawnAtElapsed;
        profile.RequireBossDefeatToComplete = data.RequireBossDefeatToComplete;
        profile.PauseNormalSpawnsWhileBossAlive = data.PauseNormalSpawnsWhileBossAlive;
        profile.EliteSpawnChance = data.EliteSpawnChance;
        profile.SpecialSpawnChance = data.SpecialSpawnChance;
        profile.SpecialEnemyConfigIds = data.SpecialEnemyConfigIds ?? EmptyIds;
        profile.RewardTableId = data.RewardTableId;
        profile.LegacyWaveDuration = data.WaveDuration;
        profile.LegacySpawnInterval = data.SpawnInterval;
        profile.LegacyMaxSpawnCount = data.MaxSpawnCount;
        profile.LegacyStatScalePerWave = data.StatScalePerWave;
        profile.BossSpawnPlans = EmptyBossPlans;
        return profile;
    }

    /// <summary>由 <see cref="WaveDefinitionResolver"/> 填充字段。</summary>
    internal void ApplyResolvedData(
        int waveIndex,
        IReadOnlyList<WaveEnemyEntry> enemyEntries,
        IReadOnlyList<string> enemyConfigIds,
        bool hasBoss,
        string bossConfigId,
        float bossSpawnAtElapsed,
        bool requireBossDefeatToComplete,
        bool pauseNormalSpawnsWhileBossAlive,
        float eliteSpawnChance,
        float specialSpawnChance,
        IReadOnlyList<string> specialEnemyConfigIds,
        string rewardTableId,
        WaveDifficultyOverride difficulty,
        float legacyWaveDuration,
        float legacySpawnInterval,
        int legacyMaxSpawnCount,
        float legacyStatScalePerWave,
        IReadOnlyList<WaveBossSpawnPlan> bossSpawnPlans = null,
        WaveDifficultySnapshot difficultySnapshot = default)
    {
        WaveIndex = Mathf.Max(1, waveIndex);
        EnemyEntries = enemyEntries ?? EmptyEntries;
        EnemyConfigIds = enemyConfigIds ?? EmptyIds;
        BossSpawnPlans = bossSpawnPlans != null && bossSpawnPlans.Count > 0
            ? bossSpawnPlans
            : EmptyBossPlans;
        HasBoss = hasBoss || BossSpawnPlans.Count > 0;
        BossConfigId = bossConfigId;
        BossSpawnAtElapsed = bossSpawnAtElapsed;
        RequireBossDefeatToComplete = requireBossDefeatToComplete;
        PauseNormalSpawnsWhileBossAlive = pauseNormalSpawnsWhileBossAlive;
        EliteSpawnChance = Mathf.Clamp01(eliteSpawnChance);
        SpecialSpawnChance = Mathf.Clamp01(specialSpawnChance);
        SpecialEnemyConfigIds = specialEnemyConfigIds ?? EmptyIds;
        RewardTableId = rewardTableId;
        Difficulty = difficulty;
        DifficultySnapshot = difficultySnapshot.CombatMultiplier <= 0f
            ? WaveDifficultySnapshot.Identity
            : difficultySnapshot;
        LegacyWaveDuration = legacyWaveDuration;
        LegacySpawnInterval = legacySpawnInterval;
        LegacyMaxSpawnCount = legacyMaxSpawnCount;
        LegacyStatScalePerWave = legacyStatScalePerWave;
    }
}
