using UnityEngine;

/// <summary>
/// 城墙状态基类，持有城墙控制器引用并绑定动画器。
/// </summary>
public class WallState : EntityState
{
    /// <summary>关联的城墙控制管理器。</summary>
    protected WallControlManager wallControl;

    /// <summary>
    /// 初始化城墙状态。
    /// </summary>
    /// <param name="wallControl">城墙控制管理器。</param>
    /// <param name="machine">状态机实例。</param>
    /// <param name="animName">对应动画参数名。</param>
    public WallState(WallControlManager wallControl, StateMachine machine, string animName) : base(machine, animName)
    {
        this.wallControl = wallControl;
        anim = wallControl.anim;
    }
}
