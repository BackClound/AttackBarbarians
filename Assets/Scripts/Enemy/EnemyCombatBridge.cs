using UnityEngine;

/// <summary>
/// 敌人动画攻击帧桥接：将 Animator 事件转发到 <see cref="EnemyController.ExecuteWallAttack"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人 Prefab 根节点（与 <see cref="EnemyController"/> 同物体）。</para>
/// </remarks>
[DisallowMultipleComponent]
public class EnemyCombatBridge : MonoBehaviour
{
    private EnemyController controller;

    /// <summary>缓存敌人控制器引用。</summary>
    private void Awake()
    {
        controller = GetComponent<EnemyController>();
    }

    /// <summary>
    /// Animator 攻击帧入口，对城墙执行近战伤害。
    /// </summary>
    public void PerformAttack()
    {
        controller?.ExecuteWallAttack();
    }
}
