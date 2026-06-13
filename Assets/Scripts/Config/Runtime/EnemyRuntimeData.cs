/// <summary>
/// 敌人运行时数据：从 <see cref="EnemyDataSO"/> 复制战斗与奖励字段。
/// </summary>
public sealed class EnemyRuntimeData
{
    /// <summary>敌人配置唯一标识。</summary>
    public string ConfigId { get; private set; }

    /// <summary>当前生命值。</summary>
    public float CurrentHp { get; set; }

    /// <summary>运行时属性快照。</summary>
    public StatRuntimeSnapshot Stats { get; } = new StatRuntimeSnapshot();

    /// <summary>攻击距离。</summary>
    public float AttackDistance { get; private set; }

    /// <summary>接触伤害。</summary>
    public float ContactDamage { get; private set; }

    /// <summary>攻击冷却时间（秒）。</summary>
    public float AttackCooldown { get; private set; }

    /// <summary>击杀经验奖励。</summary>
    public int ExperienceReward { get; private set; }

    /// <summary>特殊能力标签组合。</summary>
    public EnemyAbilityTag AbilityTags { get; private set; }

    /// <summary>对象池 Key。</summary>
    public string PoolKey { get; private set; }

    /// <summary>
    /// 从敌人配置资产初始化运行时数据，并将当前生命值设为最大生命值。
    /// </summary>
    /// <param name="source">来源敌人配置；为 null 时不执行任何操作。</param>
    public void Initialize(EnemyDataSO source)
    {
        if (source == null)
        {
            return;
        }

        ConfigId = source.ConfigId;
        Stats.CopyFromBlock(source.BaseStats);
        CurrentHp = Stats.Get(StatType.MaxHp);
        AttackDistance = source.AttackDistance;
        ContactDamage = source.ContactDamage;
        AttackCooldown = source.AttackCooldown;
        ExperienceReward = source.ExperienceReward;
        AbilityTags = source.AbilityTags;
        PoolKey = source.PoolKey;
    }

    /// <summary>
    /// 将当前生命值重置为属性快照中的最大生命值。
    /// </summary>
    public void ResetHp()
    {
        CurrentHp = Stats.Get(StatType.MaxHp);
    }
}
