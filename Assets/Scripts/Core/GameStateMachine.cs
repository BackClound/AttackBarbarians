using System;
using UnityEngine;

/// <summary>
/// 游戏级状态机：合法跳转校验、进入/退出回调，以及 <see cref="Time.timeScale"/> 的统一归属。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="GameManager"/> 在运行时持有。</para>
/// <para><b>时间缩放：</b>仅在 <see cref="ChangeState"/> 成功时根据目标状态写入 <c>Time.timeScale</c>，避免在 Manager 各方法中散落赋值。</para>
/// </remarks>
public sealed class GameStateMachine
{
    private readonly bool enableDebugLogs;
    private GameState currentState;
    private GameState previousState;

    public GameState CurrentState => currentState;
    public GameState PreviousState => previousState;

    public event Action<GameState, GameState> StateExited;
    public event Action<GameState, GameState> StateEntered;

    public GameStateMachine(GameState initialState, bool enableDebugLogs = false)
    {
        this.enableDebugLogs = enableDebugLogs;
        currentState = initialState;
        previousState = initialState;
        ApplyTimeScaleForState(currentState);
    }

    public bool CanTransitionTo(GameState targetState) =>
        targetState != currentState && IsTransitionAllowed(currentState, targetState);

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

    private void LogTransitionRejected(GameState target, string reason)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[GameStateMachine] Rejected {currentState} -> {target}: {reason}");
    }

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
