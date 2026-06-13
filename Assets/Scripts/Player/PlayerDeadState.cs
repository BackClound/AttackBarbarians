/// <summary>
/// 玩家死亡状态（<see cref="Player"/> 状态机）。进入后播放死亡动画并保持终态，由 <see cref="Player_Health.Die"/> 切入。
/// </summary>
public class PlayerDeadState : PlayerState
{
    /// <summary>创建死亡状态实例。</summary>
    /// <param name="player">所属玩家实体。</param>
    /// <param name="machine">玩家状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public PlayerDeadState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }

    /// <summary>进入死亡状态：播放死亡动画。</summary>
    public override void OnEnter()
    {
        base.OnEnter();
    }

    /// <summary>死亡终态，无后续转换。</summary>
    public override void OnUpdate()
    {
    }
}
