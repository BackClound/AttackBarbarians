/// <summary>
/// 由 Controller 驱动实体级 <see cref="StateMachine"/> 的宿主接口（Core Framework 约定）。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。由 <see cref="PlayerController"/> / <see cref="EnemyController"/> 实现。</remarks>
public interface IEntityStateMachineHost
{
    /// <summary>每帧驱动状态机 Update。</summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    void TickStateMachine(float deltaTime);

    /// <summary>固定时间步驱动状态机 FixedUpdate。</summary>
    /// <param name="fixedDeltaTime">FixedUpdate 间隔秒数。</param>
    void TickStateMachineFixed(float fixedDeltaTime);
}
