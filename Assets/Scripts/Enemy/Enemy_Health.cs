using UnityEngine;

public class Enemy_Health : Entity_Health
{
    private Enemy enemy;
    // 最大生命值，当前生命值，和预期被攻击后剩余的生命值，realHp用来控制下一次攻击是否可以攻击该敌人
    [SerializeField] private float currentHp;
    [SerializeField] private float realHp;
    [SerializeField] private bool isDead = false;

    public override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        currentHp = entity_Stats.GetMaxHp();
        realHp = currentHp;
    }

    public override bool CanBeDamage()
    {
        return currentHp > 0 && realHp > 0 && !isDead;
    }

    public override void ReduceHp(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            isDead = true;
            Die();
        }
    }

    public void WillReduceHp(float damage)
    {
        realHp -= damage;
    }

    public override void RaiseHp(float healing)
    {
        var newHp = currentHp += healing;
        currentHp = Mathf.Min(newHp, entity_Stats.GetMaxHp());

        realHp = Mathf.Min(realHp + healing, currentHp);
    }

    public float GetDamage()
    {
        return entity_Stats.GetTotalDamage();
    }

    public override void Die()
    {
        enemy.stateMachine.ChangeState(enemy.deadState);
    }
}
