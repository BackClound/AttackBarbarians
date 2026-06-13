using System;
using UnityEngine;

/// <summary>
/// 游戏级状态机：合法跳转校验、进入/退出回调，以及 <see cref="Time.timeScale"/> 的统一归属。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="GameManager"/> 在运行时持有。</para>
/// <para><b>时间缩放：</b>仅在 <see cref="TryChangeState"/> 成功时根据目标状态写入 <c>Time.timeScale</c>，避免在 Manager 各方法中散落赋值。</para>
/// </remarks>
public sealed class GameStateMachine
{
    private readonly bool enableDebugLogs;
    private GameState currentState;
    private GameState previousState;

    /// <summary>当前游戏状态。</summary>
    public GameState CurrentState => currentState;

    /// <summary>上一次成功切换前的状态。</summary>
    public GameState PreviousState => previousState;

    /// <summary>状态退出时触发（oldState, newState）。</summary>
    public event Action<GameState, GameState> StateExited;

    /// <summary>状态进入时触发（oldState, newState）。</summary>
    public event Action<GameState, GameState> StateEntered;

    /// <summary>创建状态机并应用初始状态的 timeScale。</summary>
    /// <param name="initialState">起始状态。</param>
    /// <param name="enableDebugLogs">为 true 时记录跳转与拒绝日志。</param>
    public GameStateMachine(GameState initialState, bool enableDebugLogs = false)
    {
        this.enableDebugLogs = enableDebugLogs;
        currentState = initialState;
        previousState = initialState;
        ApplyTimeScaleForState(currentState);
    }

    /// <summary>判断从当前状态到目标状态是否合法。</summary>
    /// <param name="targetState">目标状态。</param>
    /// <returns>合法且不同于当前状态时返回 true。</returns>
    public bool CanTransitionTo(GameState targetState) =>
        targetState != currentState && IsTransitionAllowed(currentState, targetState);

    /// <summary>尝试切换状态，失败时不修改当前状态。</summary>
    /// <param name="newState">目标状态。</param>
    /// <param name="failureReason">失败原因；成功时为 null。</param>
    /// <returns>切换成功时返回 true。</returns>
    public bool TryChangeState(GameState newState, out string failureReason)
    {
        if (newState == currentState)
        {
            failureReason = "Already in target state.";
            return false;
        }

        if (!IsTransitionAllowed(currentState, newState))
        {
            failureReason = $"Transition not allowed: {currentState} -> {newState}.";
            LogTransitionRejected(newState, failureReason);
            return false;
        }

        GameState oldState = currentState;
        previousState = oldState;
        currentState = newState;

        StateExited?.Invoke(oldState, newState);
        ApplyTimeScaleForState(newState);
        StateEntered?.Invoke(oldState, newState);

        if (enableDebugLogs)
        {
            Debug.Log($"[GameStateMachine] {oldState} -> {newState} (timeScale={Time.timeScale})");
        }

        failureReason = null;
        return true;
    }

    /// <summary>强制重置到指定状态（不触发 Exit/Enter 事件）。</summary>
    /// <param name="state">目标状态。</param>
    public void ResetTo(GameState state)
    {
        currentState = state;
        previousState = state;
        ApplyTimeScaleForState(state);
    }

    /// <summary>
    /// 关闭流程时恢复默认时间缩放。
    /// </summary>
    public void Shutdown()
    {
        Time.timeScale = 1f;
    }

    /// <summary>根据状态设置 <see cref="Time.timeScale"/>。</summary>
    /// <param name="state">当前或目标状态。</param>
    private static void ApplyTimeScaleForState(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.UpgradeChoosing:
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
            default:
                Time.timeScale = 1f;
                break;
        }
    }

    /// <summary>记录非法跳转警告。</summary>
    /// <param name="target">被拒绝的目标状态。</param>
    /// <param name="reason">拒绝原因。</param>
    private void LogTransitionRejected(GameState target, string reason)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[GameStateMachine] Rejected {currentState} -> {target}: {reason}");
    }

    /// <summary>静态跳转表：判断 from → to 是否允许。</summary>
    /// <param name="from">源状态。</param>
    /// <param name="to">目标状态。</param>
    /// <returns>允许时返回 true。</returns>
    private static bool IsTransitionAllowed(GameState from, GameState to)
    {
        switch (from)
        {
            case GameState.Bootstrapping:
                return to == GameState.MainMenu
                    || to == GameState.Loading
                    || to == GameState.Playing;

            case GameState.MainMenu:
                return to == GameState.Loading || to == GameState.Exiting;

            case GameState.Loading:
                return to == GameState.Playing || to == GameState.MainMenu;

            case GameState.Playing:
                return to == GameState.Paused
                    || to == GameState.WaveTransition
                    || to == GameState.UpgradeChoosing
                    || to == GameState.GameOver
                    || to == GameState.Exiting;

            case GameState.Paused:
                return to == GameState.Playing || to == GameState.GameOver;

            case GameState.WaveTransition:
                return to == GameState.UpgradeChoosing || to == GameState.Playing;

            case GameState.UpgradeChoosing:
                return to == GameState.Playing;

            case GameState.GameOver:
                return to == GameState.Restarting
                    || to == GameState.MainMenu
                    || to == GameState.Exiting;

            case GameState.Restarting:
                return to == GameState.Loading || to == GameState.Playing;

            case GameState.Exiting:
                return false;

            default:
                return false;
        }
    }
}
