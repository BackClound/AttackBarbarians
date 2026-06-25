using UnityEngine;

/// <summary>
/// 游戏流程编排：连接波次结算、升级三选一、UI、存档与音频的事件驱动入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 解析并初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 子物体或子物体 <c>GameFlowManager</c>。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI Canvas。</para>
/// <para><b>测试：</b>Inspector 右键组件可调用 <see cref="SimulateWaveCompletedForTest"/>；升级阶段调用 <see cref="ConfirmUpgradeSelection"/>。</para>
/// </remarks>
public class GameFlowManager : MonoSingleton<GameFlowManager>, IGameSystem
{
    private GameManager gameManager;
    private ConfigManager configManager;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>是否正在等待玩家确认升级选项。</summary>
    public bool IsAwaitingUpgradeSelection { get; private set; }

    /// <summary>解析依赖、订阅事件并完成初始化。</summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out gameManager))
        {
            Debug.LogError("[GameFlowManager] GameManager not found in ServiceLocator.");
            return;
        }

        ServiceLocator.TryGet(out configManager);
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.SubscribePlayerDied(OnPlayerDied);

        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并重置流程状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribePlayerDied(OnPlayerDied);
        IsAwaitingUpgradeSelection = false;
        isInitialized = false;
    }

    /// <summary>波次系统完成后调用；三选一仅由玩家升级触发，波次完成不再进入升级选择，仅关闭过渡 UI。</summary>
    /// <remarks>波次推进由 <see cref="WaveManager"/> 在波次完成时自行驱动，无需经过升级选择阶段。</remarks>
    public void NotifyWaveCompleted(WaveEventArgs args)
    {
        if (gameManager == null || gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        if (enableFlowLogs())
        {
            Debug.Log($"[GameFlowManager] Wave {args.WaveIndex} completed -> continue playing.");
        }

        CloseWaveTransitionPanel();
    }

    /// <summary>升级 UI 确认后调用，返回 Playing。</summary>
    public void ConfirmUpgradeSelection()
    {
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.CurrentState != GameState.UpgradeChoosing)
        {
            if (enableFlowLogs())
            {
                Debug.LogWarning(
                    $"[GameFlowManager] ConfirmUpgradeSelection ignored. State={gameManager.CurrentState}");
            }

            return;
        }

        IsAwaitingUpgradeSelection = false;
        GameEvents.RaiseUpgradeSelectionCompleted(this);
        gameManager.CompleteUpgradeAndResume();
    }

    /// <summary>无波次模块时用于验证波次结算 → UpgradeChoosing → Playing 闭环。</summary>
    [ContextMenu("Debug/Simulate Wave Completed")]
    public void SimulateWaveCompletedForTest()
    {
        NotifyWaveCompleted(new WaveEventArgs(1, GameConstants.Progression.WaveDurationSeconds, 0));
    }

    /// <summary>调试：模拟确认升级选择。</summary>
    [ContextMenu("Debug/Confirm Upgrade Selection")]
    public void DebugConfirmUpgradeSelection()
    {
        ConfirmUpgradeSelection();
    }

    /// <summary>波次完成事件回调。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            NotifyWaveCompleted(args);
        }
    }

    /// <summary>玩家死亡时触发 GameOver。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnPlayerDied(GameEventContext ctx)
    {
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.CurrentState == GameState.GameOver)
        {
            return;
        }

        gameManager.GameOver();
    }

    /// <summary>根据新状态驱动 UI、音频与流程。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        switch (change.NewState)
        {
            case GameState.UpgradeChoosing:
                OpenUpgradeFlow();
                break;

            case GameState.Playing:
                CloseFlowPanels();
                break;

            case GameState.Paused:
                GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Pause);
                GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicPaused);
                break;

            case GameState.GameOver:
                GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.GameOver);
                GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicGameOver);
                break;

            case GameState.MainMenu:
                GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.MainMenu);
                GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicMainMenu);
                break;
        }
    }

    /// <summary>打开升级三选一 UI 并广播事件。</summary>
    private void OpenUpgradeFlow()
    {
        CloseWaveTransitionPanel();
        IsAwaitingUpgradeSelection = true;
        GameEvents.RaiseUpgradeSelectionOpened(this);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Upgrade);
        GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicUpgrade);
    }

    /// <summary>关闭波次过渡横幅（兼容旧场景引用）。</summary>
    private static void CloseWaveTransitionPanel()
    {
        GameEvents.RaiseUiPanelClosed(null, GameConstants.UiPanelIds.WaveTransition);
        if (UIManager.Instance != null &&
            UIManager.Instance.TryGetPanel(GameConstants.UiPanelIds.WaveTransition, out UiPanelBase panel))
        {
            panel.ForceHideImmediate();
        }
    }

    /// <summary>返回 Playing 时关闭流程相关 UI 面板。</summary>
    private void CloseFlowPanels()
    {
        IsAwaitingUpgradeSelection = false;
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.WaveTransition);
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Upgrade);
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Pause);
    }

    /// <summary>是否启用流程调试日志。</summary>
    /// <returns>启用返回 true，否则返回 false。</returns>
    private bool enableFlowLogs()
    {
        return configManager != null
            && configManager.GameConfig != null
            && configManager.GameConfig.EnableRuntimeLogs;
    }
}
