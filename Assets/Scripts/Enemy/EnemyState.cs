using UnityEngine;

public class EnemyState : EntityState
{
    protected Enemy enemy;
    public EnemyState(Enemy enemy, StateMachine machine, string animName) : base(machine, animName)
    {
        this.enemy = enemy;
        this.anim = enemy.anim;
        this.rb = enemy.rb;
    }
}
