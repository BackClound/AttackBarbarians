using UnityEngine;

public class Enemy_Health : Entity_Health
{
    private Enemy enemy;
    private EnemyController controller;
    [SerializeField] private float currentHp;
    [SerializeField] private float realHp;
    [SerializeField] private bool isDead;

    private object lastDamageSource;

    public float CurrentHp => currentHp;
    public float MaxHp => entity_Stats != null ? entity_Stats.GetMaxHp() : currentHp;

    public override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>();
        controller = GetComponent<EnemyController>();
    }

    private void Start()
    {
        if (controller != null && controller.IsReady)
        {
            return;
        }

        currentHp = entity_Stats.GetMaxHp();
        realHp = currentHp;
    }

    public void SetLastDamageSource(object source)
    {
        lastDamageSource = source;
    }

    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

    protected override void OnBeforeDamageApplied(DamageInfo info, DamageResult result)
    {
        if (info.Source != null)
        {
            lastDamageSource = info.Source;
        }
    }

    public override void ApplyResolvedDamage(DamageResult result, DamageInfo info)
    {
        if (result.FinalDamage <= 0f || !CanBeDamage())
        {
            return;
        }

        float finalDamage = result.FinalDamage;
        float mitigated = result.MitigatedAmount;
        if (TryGetComponent(out EnemyDamageShield shield) && shield.IsActive)
        {
            float absorbed = finalDamage - shield.AbsorbDamage(finalDamage);
            mitigated += absorbed;
        }

        var adjusted = new DamageResult(finalDamage, mitigated, result.IsCritical, result.IsKill, result.TriggeredTags);
        OnBeforeDamageApplied(info, adjusted);
        if (finalDamage > 0f)
        {
            ReduceHp(finalDamage);
        }
    }

    protected override void ReduceHp(float damage)
    {
        currentHp -= damage;

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            Die();
            lastDamageSource = null;
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
        controller?.NotifyDeath();

        int experienceReward = controller != null ? controller.GetExperienceReward() : 0;
        GameEvents.RaiseEnemyKilled(enemy, new EnemyEventArgs(
            enemy.gameObject,
            enemy.transform.position,
            lastDamageSource,
            controller != null ? controller.ConfigId : string.Empty,
            experienceReward));

        enemy.stateMachine.ChangeState(enemy.deadState);
    }

    public void ResetForPool()
    {
        isDead = false;
        lastDamageSource = null;
        currentHp = entity_Stats.GetMaxHp();
        realHp = currentHp;
    }
}
