/// <summary>
/// 敌人运行时数据：从 <see cref="EnemyDataSO"/> 复制战斗与奖励字段。
/// </summary>
public sealed class EnemyRuntimeData
{
    public string ConfigId { get; private set; }
    public float CurrentHp { get; set; }
    public StatRuntimeSnapshot Stats { get; } = new StatRuntimeSnapshot();
    public float AttackDistance { get; private set; }
    public float ContactDamage { get; private set; }
    public float AttackCooldown { get; private set; }
    public int ExperienceReward { get; private set; }
    public string DropTableId { get; private set; }
    public string PoolKey { get; private set; }

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
        DropTableId = source.DropTableId;
        PoolKey = source.PoolKey;
    }

    public void ResetHp()
    {
        CurrentHp = Stats.Get(StatType.MaxHp);
    }
}
