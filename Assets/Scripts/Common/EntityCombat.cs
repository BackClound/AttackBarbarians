using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 这个类用来控制检测敌人，当检测到敌人时，获取可攻击敌人列表，进入攻击状态，当进入攻击距离的时候，进行攻击
/// </summary>
public class EntityCombat : MonoBehaviour
{
    #region Check Enemys
    //TODO 这个应该在工具类中根据手机的屏幕尺寸获取一个屏幕高度70% - 80%的距离
    [SerializeField] protected float checkEnemyDistance = 25;
    [SerializeField] protected Transform checkEnemy;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected string enemyTag;
    [SerializeField] protected string enemyLayerName;
    [SerializeField] protected List<Entity> effectiveEnemys;
    [SerializeField] protected bool canAttack;
    protected bool isAttacking;
    #endregion


    /// <summary>
    /// 在Awake方法中检查是否正确进行了初始化
    /// </summary>
    protected virtual void Awake()
    {
    }

    /// <summary>
    /// 这个可以放在FixedUpdate中吗
    /// TODO 获得可攻击列表之后，攻击最近的敌人，获取敌人的MaxHP，然后分配对应的攻击数量
    /// 当enemy已经分配了攻击之后，标记该敌人，其他的魔法攻击优先攻击未标记的敌人，
    /// 当所有敌人都已被标记之后，进行随机
    /// </summary>
    protected virtual void CheckEnemyInRadius()
    {

    }

    protected virtual void AttackEnemyWithWeapon(GameObject bullet) { }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (checkEnemy == null)
        {
            checkEnemy = transform;
        }
        Gizmos.DrawWireSphere(checkEnemy.position, checkEnemyDistance);
    }

}
