using UnityEngine;

/// <summary>
/// 动画事件桥接：将 Animator 事件转发到实体或战斗桥接组件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在含 Animator 的子物体上，通过 <c>GetComponentInParent</c> 查找宿主。</para>
/// </remarks>
public class EntityAnimatorTrigger : MonoBehaviour
{
    private Entity entity;
    private PlayerCombatBridge playerCombatBridge;
    private EnemyCombatBridge enemyCombatBridge;

    /// <summary>缓存父级实体与战斗桥接引用。</summary>
    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        playerCombatBridge = GetComponentInParent<PlayerCombatBridge>();
        enemyCombatBridge = GetComponentInParent<EnemyCombatBridge>();
    }

    /// <summary>动画结束事件：通知实体 <see cref="Entity.OnAniamtorFinished"/>。</summary>
    public virtual void OnAnimationFinished()
    {
        entity?.OnAniamtorFinished();
    }

    /// <summary>攻击帧事件：优先走 CombatBridge，否则回退到 Player/Enemy 子类。</summary>
    public virtual void OnAttackTrigger()
    {
        if (playerCombatBridge != null)
        {
            playerCombatBridge.PerformAttack();
            return;
        }


        if (enemyCombatBridge != null)
        {
            enemyCombatBridge.PerformAttack();
            return;
        }

        if (entity is Player player)
        {
            player.OnAnimatorAttackTrigger();
            return;
        }

        if (entity is Enemy enemy)
        {
            enemy.OnAnimatorAttackTrigger();
        }
    }
}
