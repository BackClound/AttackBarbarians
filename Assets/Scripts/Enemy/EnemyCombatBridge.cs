using UnityEngine;

/// <summary>
/// 敌人动画攻击帧桥接：将 Animator 事件转发到 <see cref="EnemyController.ExecuteWallAttack"/>。
/// </summary>
[DisallowMultipleComponent]
public class EnemyCombatBridge : MonoBehaviour
{
    private EnemyController controller;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
    }

    public void PerformAttack()
    {
        controller?.ExecuteWallAttack();
    }
}
