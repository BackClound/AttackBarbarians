using UnityEngine;

public class Enemy : Entity, IDamagable
{
    public Enemy_Health enemy_Health;

    [Header("Attack info")]
    [SerializeField] public float moveSpeed;
    [SerializeField] protected Transform attackCheck;
    [SerializeField] protected float attackDistance;
    [SerializeField] protected LayerMask wallLayer;
    [SerializeField] public float cooldownThreshold;

    #region 
    public EnemyIdleState idleState;
    public EnemyMoveState moveState;
    public EnemyAttackState attackState;
    public EnemyDeadState deadState;
    #endregion

    public override void Awake()
    {
        base.Awake();
        enemy_Health = GetComponent<Enemy_Health>();
    }

    public override void Start()
    {
        stateMachine.InitialState(idleState);
    }

    public virtual bool isWallDetected() => Physics2D.Raycast(attackCheck.position, Vector2.down, attackDistance, wallLayer);

    public virtual float GetDamageValue() => 10;

    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    public override void OnAniamtorFinished()
    {
        stateMachine.currentState.OnAnimFinished();
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (attackCheck == null)
        {
            attackCheck = transform;
        }
        Gizmos.DrawLine(attackCheck.position, attackCheck.position + Vector3.down * attackDistance);
    }

    public override void TakeDamage(float damage)
    {
        enemy_Health.TakeDamage(damage);
        DamageNumberController.numberControllerInstance.ShowDamageNumber(damage, transform.position);
    }
}
