using UnityEngine;

/// <summary>
/// 城墙受击状态：受击动画播放完毕后返回空闲状态。
/// </summary>
public class WallDamageState : WallState
{
    /// <summary>
    /// 初始化城墙受击状态。
    /// </summary>
    /// <param name="wallControl">城墙控制管理器。</param>
    /// <param name="machine">状态机实例。</param>
    /// <param name="animName">对应动画参数名。</param>
    public WallDamageState(WallControlManager wallControl, StateMachine machine, string animName) : base(wallControl, machine, animName)
    {
    }

    /// <summary>
    /// 每帧更新：受击动画结束后重置标记并切回空闲状态。
    /// </summary>
    public override void OnUpdate()
    {
        base.OnUpdate();
        if (isAnimFinished)
        {
            wallControl.ResetDamageState(false);
            stateMachine.ChangeState(wallControl.idleState);
        }
    }
}
