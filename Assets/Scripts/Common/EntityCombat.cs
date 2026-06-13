using UnityEngine;

/// <summary>
/// 实体战斗检测基类：在范围内搜索敌人并驱动攻击状态与攻击执行。
/// </summary>
/// <remarks>由 Player/Enemy 战斗组件继承，挂载在实体上并配置检测半径与 Layer。</remarks>
public class EntityCombat : MonoBehaviour
{
    #region Check Enemys
    //TODO 这个应该在工具类中根据手机的屏幕尺寸获取一个屏幕高度70% - 80%的距离
    [SerializeField] protected float maxCheckDistance = 25;
    [SerializeField] protected Transform checkPosition;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected string enemyTag;
    [SerializeField] protected string enemyLayerName;

    #endregion


    /// <summary>Awake 中校验检测参数是否已正确配置。</summary>
    protected virtual void Awake()
    {
    }

    /// <summary>
    /// 在检测半径内搜索可攻击敌人，子类实现目标筛选与状态切换。
    /// </summary>
    protected virtual void CheckEnemyInRadius() { }

    /// <summary>搜索敌人并按优先级排序，子类实现。</summary>
    public virtual void CheckEnemyInRadiusWithSorted() { }

    /// <summary>在 Scene 视图中绘制检测范围 Gizmo。</summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (checkPosition == null)
        {
            checkPosition = transform;
        }
        Gizmos.DrawWireSphere(checkPosition.position, maxCheckDistance);
    }

    /// <summary>执行一次攻击，子类实现具体伤害或技能逻辑。</summary>
    public virtual void PerformAttack()
    {

    }

}
