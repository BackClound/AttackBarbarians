using System;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : Entity
{
    /// <summary>
    /// 这三个值分别是最大生命值，当前生命值，和预期被攻击后剩余的生命值，expectHp用来控制下一次攻击是否可以攻击该敌人
    /// TODO 可以增加
    /// </summary>
    public float maxHp;
    public float currentHp;
    public float expectHp;
    [SerializeField] private bool canBeStillAlive = true;
    [SerializeField] public float cooldownThreshold;

    [Header("Attack info")]
    [SerializeField] public float moveSpeed;
    [SerializeField] protected Transform attackCheck;
    [SerializeField] protected float attackDistance;
    [SerializeField] protected LayerMask wallLayer;

    #region 
    public EnemyIdleState idleState;
    public EnemyMoveState moveState;
    public EnemyAttackState attackState;
    public EnemyDeadState deadState;
    #endregion

    public override void Start()
    {
        stateMachine.InitialState(idleState);
    }

    /// <summary>
    /// 攻击已发出在攻击生效前，更新是否可被攻击
    /// </summary>
    public void UpdateStateBeforeAttack(float damage)
    {
        canBeStillAlive = currentHp - damage > 0;
    }

    public virtual bool isWallDetected() => Physics2D.Raycast(attackCheck.position, Vector2.down, attackDistance, wallLayer);

    public virtual float GetDamageValue() => 10;
    public bool CanBeDamage()
    {
        return currentHp > 0 && canBeStillAlive;
    }

    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    public override void OnAniamtorFinished()
    {
        stateMachine.currentState.OnAnimFinished();
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

}
