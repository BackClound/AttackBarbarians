using UnityEngine;

/// <summary>
/// 玩家血量：仅负责数值与 <see cref="GameEvents"/> 发布，UI 由事件订阅方刷新。
/// </summary>
public class Player_Health : Entity_Health
{
    private float currentHp;
    private float lastKnownMaxHp;
    private bool isDead;

    public override void Awake()
    {
        base.Awake();
    }

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

    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

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

    public override void RaiseHp(float healing)
    {
        float maxHp = entity_Stats.GetMaxHp();
        currentHp = Mathf.Min(currentHp + healing, maxHp);
        PublishHealthChanged(healing);
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

    public override void Die()
    {
        GameEvents.RaisePlayerDied(this);
        Player.sInstance.Die();
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.GameOver();
        }
    }

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

    private void PublishHealthChanged(float delta)
    {
        float maxHp = entity_Stats.GetMaxHp();
        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, delta, null));
    }
}
