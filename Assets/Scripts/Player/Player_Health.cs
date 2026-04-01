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
        if (currentHp < 0)
        {
            isDead = true;
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
            //show game over UI
        }
    }

}
