using UnityEngine;
using UnityEngine.UI;

public class Player_Health : Entity_Health
{
    private float currentHp;
    private float lastKnownMaxHp;
    private bool isDead;
    private Slider healthBarSlider;

    public override void Awake()
    {
        base.Awake();
        healthBarSlider = GetComponentInChildren<Slider>();
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
            UpdateHealthBar();
        }
    }

    /// <summary>按当前 Entity_Stats 满血初始化并发布血量事件。</summary>
    public void InitializeHpFromStats()
    {
        float maxHp = entity_Stats.GetMaxHp();
        currentHp = maxHp;
        lastKnownMaxHp = maxHp;
        isDead = false;
        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, 0f, null));
        UpdateHealthBar();
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
        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, -damage, null));

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            currentHp = 0;
            Die();
        }
        UpdateHealthBar();
    }

    public override void RaiseHp(float healing)
    {
        var newHp = currentHp + healing;
        float maxHp = entity_Stats.GetMaxHp();
        currentHp = Mathf.Min(newHp, maxHp);
        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, healing, null));
        UpdateHealthBar();
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

        GameEvents.RaisePlayerHealthChanged(this, new PlayerHealthEventArgs(currentHp, maxHp, 0f, null));
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        // if (healthBarSlider == null)
        // {
        //     return;
        // }

        // float maxHp = entity_Stats.GetMaxHp();
        // healthBarSlider.value = maxHp > 0f ? currentHp / maxHp : 0f;
    }

    public override void Die()
    {
        if (isDead)
        {
            GameEvents.RaisePlayerDied(this);
            Player.sInstance.Die();
            if (ServiceLocator.TryGet(out GameManager gameManager))
            {
                gameManager.GameOver();
            }
            //show game over UI
        }
    }

    ///TODO:是否只有enemy在局内随时间生命上限进行提升， Player的生命上限受增益buff控制。
    public void ApplyMaxHpMultiplierFromBuff()
    {
        if (entity_Stats == null) return;
        var max = entity_Stats.GetMaxHp();
        if (max <= 0) return;
        var ratio = healthBarSlider != null ? healthBarSlider.value : currentHp / max;
        currentHp = Mathf.Clamp(ratio * max, 1f, max);
        UpdateHealthBar();
    }

}
