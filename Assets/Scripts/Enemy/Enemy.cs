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

    [Header("Attack info")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackCheck;
    [SerializeField] private float attackDistance;

    /// <summary>
    /// 攻击已发出在攻击生效前，更新是否可被攻击
    /// </summary>
    public void UpdateStateBeforeAttack(float damage)
    {
        canBeStillAlive = currentHp - damage > 0;
    }

    public bool CanBeDamage()
    {
        return currentHp > 0 && canBeStillAlive;
    }
}
