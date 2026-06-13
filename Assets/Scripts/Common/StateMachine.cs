using UnityEngine;

/// <summary>
/// 实体状态机：管理 <see cref="EntityState"/> 的进入、退出与每帧更新。
/// </summary>
/// <remarks>纯 C# 类，无需挂载。由 <see cref="Entity"/> 持有并由 Controller 驱动。</remarks>
public class StateMachine
{
    /// <summary>当前激活的状态。</summary>
    public EntityState currentState { get; private set; }

    /// <summary>设置初始状态并触发 <see cref="EntityState.OnEnter"/>。</summary>
    /// <param name="state">首个状态实例。</param>
    public void InitialState(EntityState state)
    {
        if (currentState == null)
        {
            currentState = state;
            currentState.OnEnter();
        }
        else
        {
            Debug.Log("You should Change state to the New EntityState!!!");
        }
    }

    /// <summary>切换到新状态：先 Exit 旧状态再 Enter 新状态。</summary>
    /// <param name="newState">目标状态。</param>
    public void ChangeState(EntityState newState)
    {
        if (currentState == newState) return;
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    /// <summary>驱动当前状态的 <see cref="EntityState.OnUpdate"/>。</summary>
    public void UpdateState()
    {
        currentState?.OnUpdate();
    }

    /// <summary>驱动当前状态的 <see cref="EntityState.OnFixedUpdate"/>。</summary>
    public void FixedUpdateState()
    {
        currentState?.OnFixedUpdate();
    }
}
