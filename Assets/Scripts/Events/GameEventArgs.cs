using UnityEngine;

/// <summary>
/// 伤害事件负载，通过 <see cref="GameConstants.EventKeys.DamageApplied"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct DamageEventArgs
{
    /// <summary>最终伤害数值。</summary>
    public float Amount { get; }
    /// <summary>受击世界坐标。</summary>
    public Vector3 WorldPosition { get; }
    /// <summary>伤害来源对象。</summary>
    public object Source { get; }
    /// <summary>受击目标 GameObject。</summary>
    public GameObject Target { get; }
    /// <summary>是否暴击。</summary>
    public bool IsCritical { get; }
    /// <summary>关联技能 ID。</summary>
    public string SkillId { get; }
    /// <summary>元素类型。</summary>
    public ElementType Element { get; }

    /// <summary>构造 <see cref="DamageEventArgs"/>。</summary>
    /// <param name="amount">最终伤害数值。</param>
    /// <param name="worldPosition">受击世界坐标。</param>
    /// <param name="source">伤害来源对象。</param>
    /// <param name="target">受击目标 GameObject。</param>
    /// <param name="isCritical">是否暴击。</param>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="element">元素类型。</param>
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
    /// <summary>投射物 GameObject。</summary>
    public GameObject ProjectileObject { get; }
    /// <summary>受击目标 GameObject。</summary>
    public GameObject Target { get; }
    /// <summary>实际造成的伤害量。</summary>
    public float DamageDealt { get; }
    /// <summary>是否暴击。</summary>
    public bool IsCritical { get; }
    /// <summary>关联技能 ID。</summary>
    public string SkillId { get; }
    /// <summary>投射物运动类型。</summary>
    public ProjectileMotionType MotionType { get; }

    /// <summary>构造 <see cref="ProjectileHitEventArgs"/>。</summary>
    /// <param name="projectileObject">投射物 GameObject。</param>
    /// <param name="target">受击目标 GameObject。</param>
    /// <param name="damageDealt">实际造成的伤害量。</param>
    /// <param name="isCritical">是否暴击。</param>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="motionType">投射物运动类型。</param>
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
    /// <summary>敌人 GameObject。</summary>
    public GameObject EnemyObject { get; }
    /// <summary>世界坐标位置。</summary>
    public Vector3 Position { get; }
    /// <summary>击杀者对象。</summary>
    public object Killer { get; }
    /// <summary>敌人类型配置 ID。</summary>
    public string EnemyTypeId { get; }
    /// <summary>击杀授予玩家的经验（不含局末金币/钻石结算）。</summary>
    public int ExperienceReward { get; }

    /// <summary>构造 <see cref="EnemyEventArgs"/>。</summary>
    /// <param name="enemyObject">敌人 GameObject。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="killer">击杀者对象。</param>
    /// <param name="enemyTypeId">敌人类型配置 ID。</param>
    /// <param name="experienceReward">击杀授予玩家的经验。</param>
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
    /// <summary>本局游玩时长（秒）。</summary>
    public float SessionDurationSeconds { get; }
    /// <summary>奖励档位。</summary>
    public int RewardTier { get; }
    /// <summary>难度等级。</summary>
    public int DifficultyLevel { get; }
    /// <summary>结算授予的金币。</summary>
    public long GoldGranted { get; }
    /// <summary>结算授予的钻石。</summary>
    public long DiamondsGranted { get; }

    /// <summary>构造 <see cref="RunRewardSettledEventArgs"/>。</summary>
    /// <param name="sessionDurationSeconds">本局游玩时长（秒）。</param>
    /// <param name="rewardTier">奖励档位。</param>
    /// <param name="difficultyLevel">难度等级。</param>
    /// <param name="goldGranted">结算授予的金币。</param>
    /// <param name="diamondsGranted">结算授予的钻石。</param>
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
    /// <summary>波次序号（从 1 起）。</summary>
    public int WaveIndex { get; }
    /// <summary>持续时长（秒）。</summary>
    public float DurationSeconds { get; }
    /// <summary>敌人数量。</summary>
    public int EnemyCount { get; }

    /// <summary>构造 <see cref="WaveEventArgs"/>。</summary>
    /// <param name="waveIndex">波次序号（从 1 起）。</param>
    /// <param name="durationSeconds">持续时长（秒）。</param>
    /// <param name="enemyCount">敌人数量。</param>
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
    /// <summary>Buff 配置 ID。</summary>
    public string BuffId { get; }
    /// <summary>当前层数。</summary>
    public int Stacks { get; }
    /// <summary>剩余持续时间（秒）。</summary>
    public float RemainingSeconds { get; }
    /// <summary>受击目标 GameObject。</summary>
    public object Target { get; }

    /// <summary>构造 <see cref="BuffEventArgs"/>。</summary>
    /// <param name="buffId">Buff 配置 ID。</param>
    /// <param name="stacks">当前层数。</param>
    /// <param name="remainingSeconds">剩余持续时间（秒）。</param>
    /// <param name="target">受击目标 GameObject。</param>
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
    /// <summary>当前生命值。</summary>
    public float CurrentHp { get; }
    /// <summary>最大生命值。</summary>
    public float MaxHp { get; }
    /// <summary>本次变化量（负数为受伤）。</summary>
    public float Delta { get; }
    /// <summary>伤害来源对象。</summary>
    public object Source { get; }

    /// <summary>构造 <see cref="PlayerHealthEventArgs"/>。</summary>
    /// <param name="currentHp">当前生命值。</param>
    /// <param name="maxHp">最大生命值。</param>
    /// <param name="delta">本次变化量（负数为受伤）。</param>
    /// <param name="source">伤害来源对象。</param>
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
    /// <summary>属性运行时快照。</summary>
    public StatRuntimeSnapshot Snapshot { get; }

    /// <summary>构造 <see cref="PlayerStatsChangedEventArgs"/>。</summary>
    /// <param name="snapshot">属性运行时快照。</param>
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
    /// <summary>主要攻击目标。</summary>
    public Enemy PrimaryTarget { get; }
    /// <summary>关联技能 ID。</summary>
    public string SkillId { get; }

    /// <summary>构造 <see cref="PlayerAttackEventArgs"/>。</summary>
    /// <param name="primaryTarget">主要攻击目标。</param>
    /// <param name="skillId">技能 ID。</param>
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
    /// <summary>地图配置 ID。</summary>
    public string MapConfigId { get; }
    /// <summary>推荐难度等级。</summary>
    public int RecommendedDifficulty { get; }

    /// <summary>构造 <see cref="MapLoadedEventArgs"/>。</summary>
    /// <param name="mapConfigId">地图配置 ID。</param>
    /// <param name="recommendedDifficulty">推荐难度等级。</param>
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
    /// <summary>局内事件配置 ID。</summary>
    public string EventConfigId { get; }
    /// <summary>波次序号（从 1 起）。</summary>
    public int WaveIndex { get; }
    /// <summary>持续时长（秒）。</summary>
    public float DurationSeconds { get; }

    /// <summary>构造 <see cref="GameplayEventArgs"/>。</summary>
    /// <param name="eventConfigId">局内事件配置 ID。</param>
    /// <param name="waveIndex">波次序号（从 1 起）。</param>
    /// <param name="durationSeconds">持续时长（秒）。</param>
    public GameplayEventArgs(string eventConfigId, int waveIndex, float durationSeconds)
    {
        EventConfigId = eventConfigId ?? string.Empty;
        WaveIndex = waveIndex;
        DurationSeconds = durationSeconds;
    }
}
