using UnityEngine;

public class Enemy : Entity, IDamagable, IPoolable
{
    public Enemy_Health enemy_Health;
    public EnemyController controller { get; private set; }

    [Header("Classification")]
    [SerializeField] private bool isBoss;

    public bool IsBoss => isBoss;

    public void SetBossFlag(bool value) => isBoss = value;

    [Header("Attack probe (legacy Inspector fields)")]
    [SerializeField] protected Transform attackCheck;
    [SerializeField] protected float attackDistance;
    [SerializeField] protected LayerMask wallLayer;
    [SerializeField] public float moveSpeed;
    [SerializeField] public float cooldownThreshold;

    #region States
    public EnemyIdleState idleState;
    public EnemyMoveState moveState;
    public EnemyAttackState attackState;
    public EnemyDeadState deadState;
    #endregion

    public Transform AttackProbe => attackCheck != null ? attackCheck : transform;
    public float AttackProbeDistance => attackDistance;
    public LayerMask WallLayer => wallLayer;

    public void SetAttackProbeDistance(float distance)
    {
        attackDistance = Mathf.Max(0.05f, distance);
    }

    public override void Awake()
    {
        base.Awake();
        enemy_Health = GetComponent<Enemy_Health>();
        controller = GetComponent<EnemyController>();
    }

    public override void Start()
    {
        if (controller != null && controller.IsReady)
        {
            return;
        }

        stateMachine.InitialState(idleState);
        moveSpeed = enemy_Health.entity_Stats.GetMoveSpeed();
    }

    public bool IsWallDetected()
    {
        if (controller != null && controller.IsReady)
        {
            return controller.IsWallInAttackRange();
        }

        return Physics2D.Raycast(AttackProbe.position, Vector2.down, AttackProbeDistance, wallLayer).collider != null;
    }

    public virtual float GetDamageValue()
    {
        if (controller != null && controller.IsReady)
        {
            return controller.GetMeleeDamage();
        }

        if (enemy_Health != null && enemy_Health.entity_Stats != null)
        {
            return enemy_Health.entity_Stats.GetBaseAttackDamage();
        }

        return 10f;
    }

    public void SetVelocity(Vector2 velocity)
    {
        rb.velocity = velocity;
    }

    public override void OnAniamtorFinished()
    {
        stateMachine.currentState?.OnAnimFinished();
    }

    public void OnAnimatorAttackTrigger()
    {
        stateMachine.currentState?.OnAnimAttackTrigger();
    }

    public void Die()
    {
        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.IsManagedInstance(gameObject))
        {
            poolManager.Despawn(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    public void OnSpawn()
    {
        if (controller != null)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            return;
        }

        enemy_Health.ResetForPool();
        moveSpeed = enemy_Health.entity_Stats.GetMoveSpeed();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        stateMachine.InitialState(idleState);
    }

    public void OnDespawn()
    {
        controller?.OnPoolDespawn();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Transform probe = AttackProbe;
        Gizmos.DrawLine(probe.position, probe.position + Vector3.down * AttackProbeDistance);
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(DamageInfo.FromLegacy(damage, gameObject));
    }

    public override DamageResult TakeDamage(DamageInfo info)
    {
        DamageInfo resolved = info.Target != null ? info : info.WithTarget(gameObject);
        return DamagePipeline.Apply(resolved);
    }
}
