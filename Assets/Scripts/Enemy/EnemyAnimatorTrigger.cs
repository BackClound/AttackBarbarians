/// <summary>
/// 敌人 Animator 事件触发器，继承通用 <see cref="EntityAnimatorTrigger"/> 桥接逻辑。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人子物体（挂 Animator 的节点）上，由动画事件回调。</para>
/// </remarks>
public class EnemyAnimatorTrigger : EntityAnimatorTrigger
{
}
