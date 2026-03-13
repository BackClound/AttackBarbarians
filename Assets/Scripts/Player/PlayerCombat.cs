using System.Collections;
using UnityEngine;

/// <summary>
/// TODO是否需要重命名为PlayerCombatManager
/// 管理Player的基础攻击能力和方式
/// 这里只分配对应的enemy
/// </summary>
public class PlayerCombat : EntityCombat
{
    private Player player;

    protected override void Awake()
    {
        player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        //在攻击完成之后，再次检测攻击
        if (!isAttacking)
        {
            CheckEnemyInRadiusWithSorted();
        }

        if (canAttack && !isAttacking)
        {
            //切换player状态为PlayerShootState
            player.stateMachine.ChangeState(player.shootState);
        }
        else
        {
            player.stateMachine.ChangeState(player.idleState);
        }
    }

    /// <summary>
    /// 每次敌人之后，扫描最近的敌人
    /// 检测敌人，根据距离对enemy进行排序
    /// 获取enemy的血量信息，将该enemy作为Target分配给Bullet作为待攻击目标
    /// </summary>
    protected override void CheckEnemyInRadiusWithSorted()
    {
        effectiveEnemys.Clear();
        canAttack = false;
        //获取离得最近的Enemy
        Vector2 startPosition = checkPosition.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(startPosition, maxCheckDistance, enemyLayer);
        // Debug.Log("CheckEnemyInRadius colliders count = " + colliders.Length);
        //对检测到的敌人进行排序
        System.Array.Sort(colliders, (a, b) =>
       {
           float sqrDistanceA = (startPosition - (Vector2)a.transform.position).sqrMagnitude;
           float sqrDistanceB = (startPosition - (Vector2)b.transform.position).sqrMagnitude;
           return sqrDistanceA.CompareTo(sqrDistanceB);

       });
        foreach (var coll in colliders)
        {
            if (coll != null && coll.gameObject.CompareTag(enemyTag))
            {
                Enemy enemy = coll.GetComponent<Enemy>();
                effectiveEnemys.Add(enemy);
                canAttack = true;
            }
        }
    }
}
