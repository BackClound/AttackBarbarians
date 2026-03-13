using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }
}
