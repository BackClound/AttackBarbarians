using UnityEngine;

/// <summary>
/// 玩家血量：仅负责数值与 <see cref="GameEvents"/> 发布，UI 由事件订阅方刷新。
/// </summary>
public class Player_Health : Entity_Health
{
    [SerializeField] private float currentHp;
    private float lastKnownMaxHp;
    private bool isDead;

    /// <summary>当前血量。</summary>
    public float CurrentHp => currentHp;

    /// <summary>最大血量（来自 Entity_Stats）。</summary>
    [SerializeField] public float MaxHp => entity_Stats != null ? entity_Stats.GetMaxHp() : 100f;

    /// <summary>缓存组件引用。</summary>
    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>启动时初始化血量并发布事件。</summary>
    private void Start()
    {
        if (currentHp <= 0f)
        {
            InitializeHpFromStats();
        }
        else
        {
            lastKnownMaxHp = entity_Stats.GetMaxHp();
            PublishHealthChanged(0f);
        }
    }

    /// <summary>按当前 Entity_Stats 满血初始化并发布血量事件。</summary>
    public void InitializeHpFromStats()
    {
        float maxHp = entity_Stats.GetMaxHp();
        currentHp = maxHp;
        lastKnownMaxHp = maxHp;
        isDead = false;
        PublishHealthChanged(0f);
    }

    /// <summary>是否仍可受到伤害。</summary>
    /// <returns>存活且未标记死亡时为 <c>true</c>。</returns>
    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

    /// <summary>扣减血量并在归零时触发死亡。</summary>
    /// <param name="damage">伤害量。</param>
    protected override void ReduceHp(float damage)
    {
        currentHp -= damage;
        float maxHp = entity_Stats.GetMaxHp();
        GameEvents.RaisePlayerDamaged(this, new PlayerHealthEventArgs(currentHp, maxHp, -damage, null));
        PublishHealthChanged(-damage);

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            currentHp = 0;
            Die();
        }
    }

    /// <summary>回复血量并发布变更事件。</summary>
    /// <param name="healing">治疗量。</param>
    public override void RaiseHp(float healing)
    {
        float maxHp = entity_Stats.GetMaxHp();
        currentHp = Mathf.Min(currentHp + healing, maxHp);
        PublishHealthChanged(healing);
    }

    /// <summary>技能/ Buff 治疗入口。</summary>
    public void Heal(float amount) => RaiseHp(amount);

    /// <summary>一次性回满当前血量。</summary>
    public void RestoreToFull()
    {
        if (entity_Stats == null)
        {
            return;
        }

        float maxHp = entity_Stats.GetMaxHp();
        float delta = maxHp - currentHp;
        currentHp = maxHp;
        isDead = false;
        PublishHealthChanged(delta);
    }

    /// <summary>最大生命等属性变更后按比例保持当前血量。</summary>
    public void OnMaxHpStatsChanged()
    {
        float maxHp = entity_Stats.GetMaxHp();
        if (maxHp <= 0f)
        {
            return;
        }

        float ratio = lastKnownMaxHp > 0f ? Mathf.Clamp01(currentHp / lastKnownMaxHp) : 1f;
        lastKnownMaxHp = maxHp;
        currentHp = isDead ? 0f : Mathf.Clamp(ratio * maxHp, 1f, maxHp);
        PublishHealthChanged(0f);
    }

    /// <summary>玩家死亡：发布事件并切换至死亡状态。</summary>
    public override void Die()
    {
        GameEvents.RaisePlayerDied(this);
        Player.sInstance.Die();
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.GameOver();
        }
    }

    /// <summary>最大生命 Buff 变更后按血量比例重算当前值。</summary>
    public void ApplyMaxHpMultiplierFromBuff()
    {
        if (entity_Stats == null)
        {
            return;
        }

        float max = entity_Stats.GetMaxHp();
        if (max <= 0)
        {
            return;
        }

        float ratio = lastKnownMaxHp > 0f ? Mathf.Clamp01(currentHp / lastKnownMaxHp) : 1f;
        currentHp = Mathf.Clamp(ratio * max, 1f, max);
        lastKnownMaxHp = max;
        PublishHealthChanged(0f);
    }

    /// <summary>发布玩家血量变更事件。</summary>
    /// <param name="delta">本次变化量（正为治疗，负为伤害）。</param>
    private void PublishHealthChanged(float delta)
    {
        float maxHp = entity_Stats.GetMaxHp();
        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, delta, null));
    }
}
