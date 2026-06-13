using UnityEngine;

/// <summary>
/// 玩家动画攻击帧桥接：将 Animator 事件转发到状态机，不再负责目标扫描或伤害结算。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 根物体（替代旧 <c>PlayerCombat</c> 扫描职责）。</para>
/// </remarks>
[DisallowMultipleComponent]
public class PlayerCombatBridge : MonoBehaviour
{
    private Player player;

    /// <summary>缓存玩家引用。</summary>
    private void Awake()
    {
        player = GetComponent<Player>();
    }

    /// <summary>Animator 攻击帧入口，转发至状态机。</summary>
    public void PerformAttack()
    {
        player?.OnAnimatorAttackTrigger();
    }
}
