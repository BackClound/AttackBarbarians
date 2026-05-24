using UnityEngine;

/// <summary>
/// 伤害事件负载，通过 <see cref="GameConstants.EventKeys.DamageApplied"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct DamageEventArgs
{
    public float Amount { get; }
    public Vector3 WorldPosition { get; }
    public object Source { get; }
    public GameObject Target { get; }
    public bool IsCritical { get; }
    public string SkillId { get; }
    public ElementType Element { get; }

    public DamageEventArgs(
        float amount,
        Vector3 worldPosition,
        object source,
        GameObject target,
        bool isCritical = false,
        string skillId = null,
        ElementType element = ElementType.None)
    {
        Amount = amount;
        WorldPosition = worldPosition;
        Source = source;
        Target = target;
        IsCritical = isCritical;
        SkillId = skillId ?? string.Empty;
        Element = element;
    }
}

/// <summary>
/// 投射物命中事件负载，通过 <see cref="GameConstants.EventKeys.ProjectileHit"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。伤害结算仍由 <see cref="DamageSystem"/> 完成，本事件用于特效/音效等表现层。</remarks>
public readonly struct ProjectileHitEventArgs
{
    public GameObject ProjectileObject { get; }
    public GameObject Target { get; }
    public float DamageDealt { get; }
    public bool IsCritical { get; }
    public string SkillId { get; }
    public ProjectileMotionType MotionType { get; }

    public ProjectileHitEventArgs(
        GameObject projectileObject,
        GameObject target,
        float damageDealt,
        bool isCritical,
        string skillId,
        ProjectileMotionType motionType)
    {
        ProjectileObject = projectileObject;
        Target = target;
        DamageDealt = damageDealt;
        IsCritical = isCritical;
        SkillId = skillId ?? string.Empty;
        MotionType = motionType;
    }
}

/// <summary>
/// 敌人相关事件负载（击杀、生成等）。
/// </summary>
/// <remarks>纯数据结构，无需挂载。订阅方优先使用 <see cref="EnemyObject"/> 与 <see cref="Position"/>，避免依赖具体 Enemy 子类。</remarks>
public readonly struct EnemyEventArgs
{
    public GameObject EnemyObject { get; }
    public Vector3 Position { get; }
    public object Killer { get; }
    public string EnemyTypeId { get; }
    /// <summary>击杀授予玩家的经验（不含局末金币/钻石结算）。</summary>
    public int ExperienceReward { get; }

    public EnemyEventArgs(
        GameObject enemyObject,
        Vector3 position,
        object killer,
        string enemyTypeId = null,
        int experienceReward = 0)
    {
        EnemyObject = enemyObject;
        Position = position;
        Killer = killer;
        EnemyTypeId = enemyTypeId ?? string.Empty;
        ExperienceReward = Mathf.Max(0, experienceReward);
    }
}

/// <summary>
/// 单局结束奖励结算事件负载（金币/钻石，按游玩时长与难度档位计算）。
/// </summary>
public readonly struct RunRewardSettledEventArgs
{
    public float SessionDurationSeconds { get; }
    public int RewardTier { get; }
    public int DifficultyLevel { get; }
    public long GoldGranted { get; }
    public long DiamondsGranted { get; }

    public RunRewardSettledEventArgs(
        float sessionDurationSeconds,
        int rewardTier,
        int difficultyLevel,
        long goldGranted,
        long diamondsGranted)
    {
        SessionDurationSeconds = sessionDurationSeconds;
        RewardTier = rewardTier;
        DifficultyLevel = difficultyLevel;
        GoldGranted = goldGranted;
        DiamondsGranted = diamondsGranted;
    }
}

/// <summary>
/// 波次事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct WaveEventArgs
{
    public int WaveIndex { get; }
    public float DurationSeconds { get; }
    public int EnemyCount { get; }

    public WaveEventArgs(int waveIndex, float durationSeconds = 0f, int enemyCount = 0)
    {
        WaveIndex = waveIndex;
        DurationSeconds = durationSeconds;
        EnemyCount = enemyCount;
    }
}

/// <summary>
/// Buff 变更事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct BuffEventArgs
{
    public string BuffId { get; }
    public int Stacks { get; }
    public float RemainingSeconds { get; }
    public object Target { get; }

    public BuffEventArgs(string buffId, int stacks, float remainingSeconds, object target)
    {
        BuffId = buffId ?? string.Empty;
        Stacks = stacks;
        RemainingSeconds = remainingSeconds;
        Target = target;
    }
}

/// <summary>
/// 玩家受伤/血量变更事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct PlayerHealthEventArgs
{
    public float CurrentHp { get; }
    public float MaxHp { get; }
    public float Delta { get; }
    public object Source { get; }

    public PlayerHealthEventArgs(float currentHp, float maxHp, float delta, object source)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
        Delta = delta;
        Source = source;
    }
}

/// <summary>
/// 玩家属性快照变更事件负载。
/// </summary>
public readonly struct PlayerStatsChangedEventArgs
{
    public StatRuntimeSnapshot Snapshot { get; }

    public PlayerStatsChangedEventArgs(StatRuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

/// <summary>
/// 玩家开始攻击（动画/开火帧）事件负载。
/// </summary>
public readonly struct PlayerAttackEventArgs
{
    public Enemy PrimaryTarget { get; }
    public string SkillId { get; }

    public PlayerAttackEventArgs(Enemy primaryTarget, string skillId = null)
    {
        PrimaryTarget = primaryTarget;
        SkillId = skillId ?? string.Empty;
    }
}

/// <summary>
/// 地图加载完成事件负载。
/// </summary>
public readonly struct MapLoadedEventArgs
{
    public string MapConfigId { get; }
    public int RecommendedDifficulty { get; }

    public MapLoadedEventArgs(string mapConfigId, int recommendedDifficulty)
    {
        MapConfigId = mapConfigId ?? string.Empty;
        RecommendedDifficulty = recommendedDifficulty;
    }
}

/// <summary>
/// 局内随机事件负载。
/// </summary>
public readonly struct GameplayEventArgs
{
    public string EventConfigId { get; }
    public int WaveIndex { get; }
    public float DurationSeconds { get; }

    public GameplayEventArgs(string eventConfigId, int waveIndex, float durationSeconds)
    {
        EventConfigId = eventConfigId ?? string.Empty;
        WaveIndex = waveIndex;
        DurationSeconds = durationSeconds;
    }
}
