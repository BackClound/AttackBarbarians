/// <summary>
/// 由 Controller 驱动实体级 <see cref="StateMachine"/> 的宿主接口（Core Framework 约定）。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。由 <see cref="PlayerController"/> / <see cref="EnemyController"/> 实现。</remarks>
public interface IEntityStateMachineHost
{
    void TickStateMachine(float deltaTime);
    void TickStateMachineFixed(float fixedDeltaTime);
}
