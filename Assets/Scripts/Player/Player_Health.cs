using UnityEngine;
using UnityEngine.UI;

public class Player_Health : Entity_Health
{
    private float currentHp;
    private bool isDead;
    private Slider healthBarSlider;

    public override void Awake()
    {
        base.Awake();
        healthBarSlider = GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        currentHp = entity_Stats.GetMaxHp();
        UpdateHealthBar();
    }

    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

    protected override void ReduceHp(float damage)
    {
        currentHp -= damage;
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
        currentHp = Mathf.Min(newHp, entity_Stats.GetMaxHp());
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        //TODO update health bar
        healthBarSlider.value = currentHp / entity_Stats.GetMaxHp();
    }

    public override void Die()
    {
        if (isDead)
        {
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
