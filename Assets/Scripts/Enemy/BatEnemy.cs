using UnityEngine;

public class BatEnemy : Enemy
{

    public override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();
        idleState = new EnemyIdleState(this, stateMachine, "isMove");
        moveState = new EnemyMoveState(this, stateMachine, "isMove");
        attackState = new EnemyAttackState(this, stateMachine, "isAttack");
        deadState = new EnemyDeadState(this, stateMachine, "isDead");
    }
    private void Update()
    {
        stateMachine.currentState.OnUpdate();
    }

    public override float GetDamageValue()
    {
        //TODO 是否需要添加其他的增加伤害的项
        if (enemy_Health != null && enemy_Health.entity_Stats != null)
            return enemy_Health.entity_Stats.GetTotalDamage();
        return 10;
    }
}
