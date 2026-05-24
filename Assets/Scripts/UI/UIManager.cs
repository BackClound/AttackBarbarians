using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 总线：面板注册、游戏状态驱动显隐、HUD 数值刷新入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 UI 根 Canvas 或 <c>UIRoot</c> 物体上。</para>
/// <para><b>推荐结构：</b></para>
/// <code>
/// UICanvas [UIManager, UiCanvasScalerSetup, Canvas]
/// ├── GameplayHUD [GameplayHudPresenter]
/// ├── MainMenuPanel [MainMenuPanelUI]
/// ├── PausePanel [PausePanelUI]
/// ├── GameOverPanel [GameOverPanelUI]
/// ├── UpgradePanel [UpgradePanelUI]
/// └── WaveTransitionPanel [WaveTransitionPanelUI]
/// </code>
/// <para><b>规则：</b>仅订阅事件与调用 GameManager，不直接操控战斗逻辑。</para>
/// </remarks>
public class UIManager : GameEventSubscriberBase
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private MainMenuPanelUI mainMenuPanel;
    [SerializeField] private GameplayHudPresenter gameplayHud;
    [SerializeField] private PausePanelUI pausePanel;
    [SerializeField] private GameOverPanelUI gameOverPanel;
    [SerializeField] private UpgradePanelUI upgradePanel;
    [SerializeField] private WaveTransitionPanelUI waveTransitionPanel;

    private readonly Dictionary<string, UiPanelBase> panelById = new Dictionary<string, UiPanelBase>(8);
    private GameManager gameManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIManager] Duplicate instance destroyed.");
            Destroy(this);
            return;
        }

        Instance = this;
        BuildRegistry();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        ServiceLocator.TryGet(out gameManager);
        SyncPanelsToCurrentState();
    }

    protected override void RegisterHandlers()
    {
        GameEvents.SubscribeOnGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeUiPanelOpened(OnUiPanelOpened);
        GameEvents.SubscribeUiPanelClosed(OnUiPanelClosed);
        GameEvents.SubscribeWaveStarted(OnWaveStarted);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.SubscribeGameStarted(OnGameStarted);
    }

    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribeOnGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeUiPanelOpened(OnUiPanelOpened);
        GameEvents.UnsubscribeUiPanelClosed(OnUiPanelClosed);
        GameEvents.UnsubscribeWaveStarted(OnWaveStarted);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
    }

    public void UpdateHealthDisplay(float current, float max)
    {
        gameplayHud?.RefreshAll();
    }

    public void UpdateWaveDisplay(int waveIndex)
    {
        waveTransitionPanel?.SetWaveIndex(waveIndex);
        gameplayHud?.RefreshAll();
    }

    public void UpdateGoldDisplay(long gold)
    {
        gameplayHud?.RefreshAll();
    }

    public void UpdateScoreDisplay(int score)
    {
        gameplayHud?.RefreshAll();
    }

    public bool TryGetPanel(string panelId, out UiPanelBase panel) => panelById.TryGetValue(panelId, out panel);

    public void ShowPanel(string panelId)
    {
        if (panelById.TryGetValue(panelId, out UiPanelBase panel))
        {
            panel.Show();
        }
    }

    public void HidePanel(string panelId)
    {
        if (panelById.TryGetValue(panelId, out UiPanelBase panel))
        {
            panel.Hide();
        }
    }

    private void BuildRegistry()
    {
        panelById.Clear();
        RegisterPanel(GameConstants.UiPanelIds.MainMenu, mainMenuPanel);
        RegisterPanel(GameConstants.UiPanelIds.Pause, pausePanel);
        RegisterPanel(GameConstants.UiPanelIds.GameOver, gameOverPanel);
        RegisterPanel(GameConstants.UiPanelIds.Upgrade, upgradePanel);
        RegisterPanel(GameConstants.UiPanelIds.WaveTransition, waveTransitionPanel);
    }

    private void RegisterPanel(string panelId, UiPanelBase panel)
    {
        if (panel == null || string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        panelById[panelId] = panel;
    }

    private void SyncPanelsToCurrentState()
    {
        if (gameManager == null)
        {
            ServiceLocator.TryGet(out gameManager);
        }

        if (gameManager == null)
        {
            return;
        }

        ApplyState(gameManager.CurrentState, gameManager.PreviousState);
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        gameplayHud?.RefreshAll();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetSessionKillCount(0);
        }
    }

    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            waveTransitionPanel?.SetWaveIndex(args.WaveIndex);
            UpdateWaveDisplay(args.WaveIndex);
        }
    }

    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            waveTransitionPanel?.SetWaveIndex(args.WaveIndex + 1);
        }
    }

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        ApplyState(change.NewState, change.OldState);
    }

    private void OnUiPanelOpened(GameEventContext ctx)
    {
        if (ctx.Payload is string panelId)
        {
            ShowPanel(panelId);
        }
    }

    private void OnUiPanelClosed(GameEventContext ctx)
    {
        if (ctx.Payload is string panelId)
        {
            HidePanel(panelId);
        }
    }

    private void ApplyState(GameState newState, GameState oldState)
    {
        bool showHud = newState == GameState.Playing
            || newState == GameState.Paused
            || newState == GameState.WaveTransition
            || newState == GameState.UpgradeChoosing;

        gameplayHud?.SetVisible(showHud);

        switch (newState)
        {
            case GameState.MainMenu:
                HideAllGameplayPanels();
                mainMenuPanel?.Show();
                break;

            case GameState.Playing:
                mainMenuPanel?.Hide();
                pausePanel?.Hide();
                gameOverPanel?.Hide();
                upgradePanel?.Hide();
                waveTransitionPanel?.Hide();
                gameplayHud?.RefreshAll();
                break;

            case GameState.Paused:
                pausePanel?.Show();
                break;

            case GameState.WaveTransition:
                waveTransitionPanel?.Show();
                break;

            case GameState.UpgradeChoosing:
                upgradePanel?.Show();
                break;

            case GameState.GameOver:
                HideAllGameplayPanels();
                if (gameplayHud != null)
                {
                    gameOverPanel?.SetSessionKillCount(gameplayHud.SessionKillCount);
                }

                gameOverPanel?.Show();
                break;

            case GameState.Loading:
            case GameState.Bootstrapping:
                HideAllGameplayPanels();
                break;
        }
    }

    private void HideAllGameplayPanels()
    {
        mainMenuPanel?.Hide();
        pausePanel?.Hide();
        gameOverPanel?.Hide();
        upgradePanel?.Hide();
        waveTransitionPanel?.Hide();
    }
}
