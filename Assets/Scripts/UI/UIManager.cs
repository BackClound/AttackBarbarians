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
    /// <summary>全局 UI 管理器单例。</summary>
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private MainMenuPanelUI mainMenuPanel;
    [SerializeField] private GameplayHudPresenter gameplayHud;
    [SerializeField] private PausePanelUI pausePanel;
    [SerializeField] private GameOverPanelUI gameOverPanel;
    [SerializeField] private UpgradePanelUI upgradePanel;
    [SerializeField] private WaveTransitionPanelUI waveTransitionPanel;
    [SerializeField] private ShopPanelUI shopPanel;
    [SerializeField] private DailyRewardPanelUI dailyRewardPanel;
    [SerializeField] private AchievementPanelUI achievementPanel;

    private readonly Dictionary<string, UiPanelBase> panelById = new Dictionary<string, UiPanelBase>(16);
    private GameManager gameManager;

    /// <summary>初始化单例并构建面板注册表。</summary>
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

    /// <summary>清空单例引用。</summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>获取 GameManager、重试事件订阅并按当前状态同步面板。</summary>
    protected override void Start()
    {
        base.Start();
        ServiceLocator.TryGet(out gameManager);
        SyncPanelsToCurrentState();
    }

    /// <summary>订阅游戏状态、面板开关与资源变化等事件。</summary>
    protected override void RegisterHandlers()
    {
        GameEvents.SubscribeOnGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeUiPanelOpened(OnUiPanelOpened);
        GameEvents.SubscribeUiPanelClosed(OnUiPanelClosed);
        GameEvents.SubscribeWaveStarted(OnWaveStarted);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>取消所有 UI 相关事件订阅。</summary>
    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribeOnGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeUiPanelOpened(OnUiPanelOpened);
        GameEvents.UnsubscribeUiPanelClosed(OnUiPanelClosed);
        GameEvents.UnsubscribeWaveStarted(OnWaveStarted);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>生命值变化时刷新 HUD（兼容旧 API）。</summary>
    /// <param name="current">当前生命。</param>
    /// <param name="max">最大生命。</param>
    public void UpdateHealthDisplay(float current, float max)
    {
        gameplayHud?.RefreshAll();
    }

    /// <summary>波次变化时刷新 HUD 与波次过渡面板。</summary>
    /// <param name="waveIndex">当前波次序号。</param>
    public void UpdateWaveDisplay(int waveIndex)
    {
        waveTransitionPanel?.SetWaveIndex(waveIndex);
        gameplayHud?.RefreshAll();
    }

    /// <summary>金币变化时刷新 HUD。</summary>
    /// <param name="gold">金币数量。</param>
    public void UpdateGoldDisplay(long gold)
    {
        gameplayHud?.RefreshAll();
    }

    /// <summary>分数变化时刷新 HUD（兼容旧 API）。</summary>
    /// <param name="score">当前分数。</param>
    public void UpdateScoreDisplay(int score)
    {
        gameplayHud?.RefreshAll();
    }

    /// <summary>按 PanelId 查找已注册面板。</summary>
    /// <param name="panelId">面板标识。</param>
    /// <param name="panel">输出的面板实例。</param>
    /// <returns>找到返回 <c>true</c>。</returns>
    public bool TryGetPanel(string panelId, out UiPanelBase panel) => panelById.TryGetValue(panelId, out panel);

    /// <summary>按 PanelId 显示面板。</summary>
    /// <param name="panelId">面板标识。</param>
    public void ShowPanel(string panelId)
    {
        if (panelById.TryGetValue(panelId, out UiPanelBase panel))
        {
            panel.Show();
        }
    }

    /// <summary>按 PanelId 隐藏面板。</summary>
    /// <param name="panelId">面板标识。</param>
    public void HidePanel(string panelId)
    {
        if (panelById.TryGetValue(panelId, out UiPanelBase panel))
        {
            panel.Hide();
        }
    }

    /// <summary>将所有面板注册到 PanelId 字典。</summary>
    private void BuildRegistry()
    {
        panelById.Clear();
        RegisterPanel(GameConstants.UiPanelIds.MainMenu, mainMenuPanel);
        RegisterPanel(GameConstants.UiPanelIds.Pause, pausePanel);
        RegisterPanel(GameConstants.UiPanelIds.GameOver, gameOverPanel);
        RegisterPanel(GameConstants.UiPanelIds.Upgrade, upgradePanel);
        RegisterPanel(GameConstants.UiPanelIds.WaveTransition, waveTransitionPanel);
        RegisterPanel(GameConstants.UiPanelIds.Shop, shopPanel);
        RegisterPanel(GameConstants.UiPanelIds.SignIn, dailyRewardPanel);
        RegisterPanel(GameConstants.UiPanelIds.Achievement, achievementPanel);
    }

    /// <summary>注册单个面板到字典。</summary>
    private void RegisterPanel(string panelId, UiPanelBase panel)
    {
        if (panel == null || string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        panelById[panelId] = panel;
    }

    /// <summary>启动时按 GameManager 当前状态同步面板。</summary>
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

    /// <summary>资源变化时刷新 HUD 与主菜单 Meta 展示。</summary>
    private void OnResourceChanged(GameEventContext ctx)
    {
        gameplayHud?.RefreshAll();
        mainMenuPanel?.RefreshMetaDisplay();
    }

    /// <summary>开局时刷新 HUD 并重置结算击杀数。</summary>
    private void OnGameStarted(GameEventContext ctx)
    {
        gameplayHud?.RefreshAll();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetSessionKillCount(0);
        }
    }

    /// <summary>波次开始时更新波次显示。</summary>
    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            waveTransitionPanel?.SetWaveIndex(args.WaveIndex);
            UpdateWaveDisplay(args.WaveIndex);
        }
    }

    /// <summary>波次完成时预置下一波编号。</summary>
    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            waveTransitionPanel?.SetWaveIndex(args.WaveIndex + 1);
        }
    }

    /// <summary>根据游戏状态机切换各面板显隐。</summary>
    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        ApplyState(change.NewState, change.OldState);
    }

    /// <summary>响应面板打开事件。</summary>
    private void OnUiPanelOpened(GameEventContext ctx)
    {
        if (ctx.Payload is string panelId)
        {
            ShowPanel(panelId);
        }
    }

    /// <summary>响应面板关闭事件。</summary>
    private void OnUiPanelClosed(GameEventContext ctx)
    {
        if (ctx.Payload is string panelId)
        {
            HidePanel(panelId);
        }
    }

    /// <summary>按新游戏状态显示/隐藏对应面板组合。</summary>
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
                shopPanel?.Hide();
                dailyRewardPanel?.Hide();
                achievementPanel?.Hide();
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
                if (upgradePanel == null)
                {
                    Debug.LogError("[UIManager] UpgradePanel 未绑定，三选一 UI 无法显示。请执行 Attack Barbarians/UI/Build BattleScene UI。");
                }

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

    /// <summary>隐藏所有战斗流程相关面板。</summary>
    private void HideAllGameplayPanels()
    {
        mainMenuPanel?.Hide();
        shopPanel?.Hide();
        dailyRewardPanel?.Hide();
        achievementPanel?.Hide();
        pausePanel?.Hide();
        gameOverPanel?.Hide();
        upgradePanel?.Hide();
        waveTransitionPanel?.Hide();
    }
}
