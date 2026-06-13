using UnityEngine;

/// <summary>
/// 敌人 Animator 事件触发器基类扩展，预留与 <see cref="Enemy"/> 的绑定。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人子物体（挂 Animator 的节点）上，由动画事件回调。</para>
/// </remarks>
public class EnemyAnimatorTrigger : EntityAnimatorTrigger
{
    private Enemy enemy;
}
