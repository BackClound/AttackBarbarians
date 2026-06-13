using UnityEngine;

/// <summary>
/// 城墙空闲状态：检测到受击标记时切换至受击状态。
/// </summary>
public class WallIdleState : WallState
{
    /// <summary>
    /// 初始化城墙空闲状态。
    /// </summary>
    /// <param name="wallControl">城墙控制管理器。</param>
    /// <param name="machine">状态机实例。</param>
    /// <param name="animName">对应动画参数名。</param>
    public WallIdleState(WallControlManager wallControl, StateMachine machine, string animName) : base(wallControl, machine, animName)
    {
    }

    /// <summary>
    /// 每帧更新：若城墙被标记为受击则切换至受击状态。
    /// </summary>
    public override void OnUpdate()
    {
        base.OnUpdate();
        if (wallControl.beDamaged)
        {
            stateMachine.ChangeState(wallControl.damageState);
        }
    }
}
