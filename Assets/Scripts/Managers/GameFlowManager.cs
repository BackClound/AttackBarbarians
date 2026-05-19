using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏流程编排：连接波次结算、升级三选一、UI、存档与音频的事件驱动入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 解析并初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体 <c>GameFlowManager</c>。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI Canvas。</para>
/// <para><b>测试：</b>Inspector 右键组件可调用 <see cref="SimulateWaveCompletedForTest"/>；升级阶段调用 <see cref="ConfirmUpgradeSelection"/>。</para>
/// </remarks>
public class GameFlowManager : MonoSingleton<GameFlowManager>, IGameSystem
{
    [Header("Flow")]
    [Tooltip("≤0 时使用 GameConfig.DefaultWaveTransitionSeconds。")]
    [SerializeField] private float waveTransitionSeconds;
    [SerializeField] private bool skipUpgradeChoosingInEditor;

    private GameManager gameManager;
    private ConfigManager configManager;
    private Coroutine waveTransitionRoutine;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public bool IsAwaitingUpgradeSelection { get; private set; }

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        StopWaveTransitionRoutine();
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribePlayerDied(OnPlayerDied);
        IsAwaitingUpgradeSelection = false;
        isInitialized = false;
    }

    /// <summary>波次系统完成后调用；若当前为 Playing 则进入 WaveTransition。</summary>
    public void NotifyWaveCompleted(WaveEventArgs args)
    {
        if (gameManager == null || gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        if (enableFlowLogs())
        {
            Debug.Log($"[GameFlowManager] Wave {args.WaveIndex} completed -> WaveTransition.");
        }

        gameManager.BeginWaveTransition();
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

    /// <summary>无波次模块时用于验证 WaveTransition → UpgradeChoosing → Playing 闭环。</summary>
    [ContextMenu("Debug/Simulate Wave Completed")]
    public void SimulateWaveCompletedForTest()
    {
        NotifyWaveCompleted(new WaveEventArgs(1, waveTransitionSeconds, 0));
    }

    [ContextMenu("Debug/Confirm Upgrade Selection")]
    public void DebugConfirmUpgradeSelection()
    {
        ConfirmUpgradeSelection();
    }

    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            NotifyWaveCompleted(args);
        }
    }

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

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        switch (change.NewState)
        {
            case GameState.WaveTransition:
                BeginWaveTransitionSequence();
                break;

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

    private void BeginWaveTransitionSequence()
    {
        StopWaveTransitionRoutine();
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.WaveTransition);
        waveTransitionRoutine = StartCoroutine(WaveTransitionRoutine());
    }

    private IEnumerator WaveTransitionRoutine()
    {
        float duration = ResolveWaveTransitionSeconds();
        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        waveTransitionRoutine = null;

        if (gameManager == null || gameManager.CurrentState != GameState.WaveTransition)
        {
            yield break;
        }

        if (ShouldSkipUpgradeChoosing())
        {
            if (enableFlowLogs())
            {
                Debug.Log("[GameFlowManager] Skipping upgrade -> Playing.");
            }

            gameManager.CompleteUpgradeAndResume();
            yield break;
        }

        gameManager.BeginUpgradeChoosing();
    }

    private void OpenUpgradeFlow()
    {
        IsAwaitingUpgradeSelection = true;
        GameEvents.RaiseUpgradeSelectionOpened(this);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Upgrade);
        GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicUpgrade);
    }

    private void CloseFlowPanels()
    {
        IsAwaitingUpgradeSelection = false;
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.WaveTransition);
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Upgrade);
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Pause);
    }

    private void StopWaveTransitionRoutine()
    {
        if (waveTransitionRoutine == null)
        {
            return;
        }

        StopCoroutine(waveTransitionRoutine);
        waveTransitionRoutine = null;
    }

    private float ResolveWaveTransitionSeconds()
    {
        if (waveTransitionSeconds > 0f)
        {
            return waveTransitionSeconds;
        }

        if (configManager != null && configManager.GameConfig != null)
        {
            return configManager.GameConfig.DefaultWaveTransitionSeconds;
        }

        return 1.5f;
    }

    private bool ShouldSkipUpgradeChoosing()
    {
#if UNITY_EDITOR
        if (skipUpgradeChoosingInEditor)
        {
            return true;
        }
#endif
        if (configManager != null && configManager.GameConfig != null)
        {
            return configManager.GameConfig.SkipUpgradeChoosingOnBootstrap;
        }

        return false;
    }

    private bool enableFlowLogs()
    {
        return configManager != null
            && configManager.GameConfig != null
            && configManager.GameConfig.EnableRuntimeLogs;
    }
}
