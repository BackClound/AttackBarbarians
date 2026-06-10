using System.Collections.Generic;
using UnityEngine;

public enum BootstrapPostFlow
{
    UseGameConfig = 0,
    StartGame = 1,
    OpenMainMenu = 2,
    None = 3,
}

public enum BootstrapManagerSet
{
    AutoByPostFlow = 0,
    MainScene = 1,
    BattleScene = 2,
    All = 3,
}

/// <summary>
/// 游戏启动引导器，负责在场景加载后按固定顺序初始化各 Manager 并注册到 <see cref="ServiceLocator"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是，必须挂载到场景中的常驻物体上。</para>
/// <para><b>推荐挂载对象：</b>在首个可玩场景中创建空物体，命名为 <c>GameSystems</c>（或 <c>[Bootstrap]</c>），将本组件挂在该物体根节点上。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、Wall、UI Canvas 等玩法或表现物体上。</para>
/// <para><b>Inspector 配置：</b></para>
/// <list type="bullet">
/// <item><description>将同物体或子物体上的 <see cref="ConfigManager"/>、<see cref="SaveManager"/>、<see cref="PoolManager"/>、<see cref="GameManager"/> 拖入对应槽位；留空时会在 Awake 时自动查找或在本物体上 AddComponent。</description></item>
/// <item><description><c>Dont Destroy On Load</c> 建议开启，保证跨场景保留引导流程（单例冲突时会销毁重复实例）。</description></item>
/// </list>
/// <para><b>启动顺序：</b>ConfigManager → SaveManager → TalentManager → EquipmentManager → EventBus → PoolManager → GameManager → …</para>
/// <para><b>获取方式：</b><c>GameBootstrapper.Instance</c> 或 <c>ServiceLocator.Get&lt;GameBootstrapper&gt;()</c>（Bootstrap 完成后）。</para>
/// </remarks>
public class GameBootstrapper : MonoSingleton<GameBootstrapper>
{
    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private BootstrapPostFlow postBootstrapFlow = BootstrapPostFlow.UseGameConfig;
    [SerializeField] private BootstrapManagerSet managerSet = BootstrapManagerSet.AutoByPostFlow;

    [Header("Managers")]
    [SerializeField] private ConfigManager configManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private AdRewardService adRewardService;
    [SerializeField] private AchievementManager achievementManager;
    [SerializeField] private DailyRewardManager dailyRewardManager;
    [SerializeField] private UpgradeCardManager upgradeCardManager;
    [SerializeField] private MetaRewardService metaRewardService;
    [SerializeField] private TalentManager talentManager;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private PerformanceManager performanceManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private RunSessionTracker runSessionTracker;
    [SerializeField] private RunRewardSettlementService runRewardSettlementService;
    [SerializeField] private PlayerExperienceService playerExperienceService;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private RandomRewardManager randomRewardManager;
    [SerializeField] private EnemySpawnerManager enemySpawnerManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private BossRunStatsBridge bossRunStatsBridge;
    [SerializeField] private DamageSystem damageSystem;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private ProjectileManager projectileManager;
    [SerializeField] private SkillUnlockService skillUnlockService;
    [SerializeField] private ContentRegistry contentRegistry;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private GameplayEventManager gameplayEventManager;
    [SerializeField] private GameplayEventDebugBridge gameplayEventDebugBridge;
    [SerializeField] private AudioManager audioManager;

    private readonly List<IGameSystem> systems = new List<IGameSystem>(16);
    private EventBus eventBus;
    private bool isBootstrapped;

    protected override SingletonOptions Options =>
        dontDestroyOnLoad ? SingletonOptions.PersistentDefault : SingletonOptions.SceneDefault;

    protected override void OnSingletonAwake()
    {
        if (initializeOnAwake)
        {
            Bootstrap();
        }
    }

    private void Update()
    {
        if (!isBootstrapped)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < systems.Count; i++)
        {
            systems[i].Tick(deltaTime);
        }
    }

    protected override void OnSingletonDestroy()
    {
        for (int i = systems.Count - 1; i >= 0; i--)
        {
            systems[i].Shutdown();
        }

        systems.Clear();
        ServiceLocator.Clear();
    }

    /// <summary>手动触发引导流程；若已在 Awake 中初始化则不会重复执行。</summary>
    public void Bootstrap()
    {
        if (isBootstrapped)
        {
            return;
        }

        ResolveManagers();
        RegisterServices();
        InitializeSystems();

        isBootstrapped = true;

        ApplyPostBootstrapFlow();
    }

    private void ApplyPostBootstrapFlow()
    {
        switch (postBootstrapFlow)
        {
            case BootstrapPostFlow.OpenMainMenu:
                gameManager.OpenMainMenu();
                return;

            case BootstrapPostFlow.StartGame:
                gameManager.StartGame();
                return;

            case BootstrapPostFlow.None:
                return;

            case BootstrapPostFlow.UseGameConfig:
            default:
                break;
        }

        if (configManager.GameConfig == null || configManager.GameConfig.StartGameOnBootstrap)
        {
            gameManager.StartGame();
        }
    }

    private void ResolveManagers()
    {
        bool includeBattleManagers = IncludesBattleManagers();

        configManager = ResolveOrCreate(configManager);
        saveManager = ResolveOrCreate(saveManager);
        resourceManager = ResolveOrCreate(resourceManager);
        shopManager = ResolveOrCreate(shopManager);
        adRewardService = ResolveOrCreate(adRewardService);
        achievementManager = ResolveOrCreate(achievementManager);
        dailyRewardManager = ResolveOrCreate(dailyRewardManager);
        upgradeCardManager = ResolveOrCreate(upgradeCardManager);
        metaRewardService = ResolveOrCreate(metaRewardService);
        talentManager = ResolveOrCreate(talentManager);
        equipmentManager = ResolveOrCreate(equipmentManager);
        performanceManager = ResolveOrCreate(performanceManager);
        poolManager = ResolveOrCreate(poolManager);
        gameManager = ResolveOrCreate(gameManager);
        skillUnlockService = ResolveOrCreate(skillUnlockService);
        contentRegistry = ResolveOrCreate(contentRegistry);
        audioManager = ResolveOrCreate(audioManager);

        if (includeBattleManagers)
        {
            gameFlowManager = ResolveOrCreate(gameFlowManager);
            runSessionTracker = ResolveOrCreate(runSessionTracker);
            runRewardSettlementService = ResolveOrCreate(runRewardSettlementService);
            playerExperienceService = ResolveOrCreate(playerExperienceService);
            upgradeManager = ResolveOrCreate(upgradeManager);
            randomRewardManager = ResolveOrCreate(randomRewardManager);
            enemySpawnerManager = ResolveOrCreate(enemySpawnerManager);
            waveManager = ResolveOrCreate(waveManager);
            bossRunStatsBridge = ResolveOrCreate(bossRunStatsBridge);
            damageSystem = ResolveOrCreate(damageSystem);
            collisionManager = ResolveOrCreate(collisionManager);
            projectileManager = ResolveOrCreate(projectileManager);
            mapManager = ResolveOrCreate(mapManager);
            gameplayEventManager = ResolveOrCreate(gameplayEventManager);
            gameplayEventDebugBridge = ResolveOrCreate(gameplayEventDebugBridge);
        }

        eventBus = new EventBus();
    }

    private void RegisterServices()
    {
        systems.Clear();

        ServiceLocator.Register(this);
        RegisterSystem(configManager);
        RegisterSystem(saveManager);
        RegisterSystem(resourceManager);
        RegisterSystem(shopManager);
        RegisterSystem(adRewardService);
        RegisterSystem(achievementManager);
        RegisterSystem(dailyRewardManager);
        RegisterSystem(upgradeCardManager);
        RegisterSystem(metaRewardService);
        RegisterSystem(talentManager);
        RegisterSystem(equipmentManager);
        RegisterSystem(eventBus);
        RegisterSystem(performanceManager);
        RegisterSystem(poolManager);
        RegisterSystem(gameManager);

        if (IncludesBattleManagers())
        {
            RegisterSystem(gameFlowManager);
            RegisterSystem(runSessionTracker);
            RegisterSystem(runRewardSettlementService);
            RegisterSystem(playerExperienceService);
            RegisterSystem(upgradeManager);
            RegisterSystem(randomRewardManager);
            RegisterSystem(damageSystem);
            RegisterSystem(collisionManager);
            RegisterSystem(projectileManager);
        }

        RegisterSystem(skillUnlockService);
        RegisterSystem(contentRegistry);

        if (IncludesBattleManagers())
        {
            RegisterSystem(mapManager);
            RegisterSystem(gameplayEventManager);
        }

        RegisterSystem(audioManager);

        if (IncludesBattleManagers())
        {
            RegisterSystem(gameplayEventDebugBridge);
            RegisterSystem(enemySpawnerManager);
            RegisterSystem(waveManager);
            RegisterSystem(bossRunStatsBridge);
        }
    }

    private void InitializeSystems()
    {
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i] is PoolManager)
            {
                ApplyPoolRuntimePolicy();
            }

            systems[i].Initialize();
        }

        RunDifficultyBootstrap.ApplyFromGameConfig(configManager != null ? configManager.GameConfig : null);
    }

    private void ApplyPoolRuntimePolicy()
    {
        if (poolManager == null || configManager == null)
        {
            return;
        }

        GameConfig gameConfig = configManager.GameConfig;
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>(GameConstants.ResourcePaths.GameConfig);
        }

        if (gameConfig == null)
        {
            return;
        }

        poolManager.ConfigureRuntimePolicy(
            gameConfig.DefaultPoolPrewarmCount,
            gameConfig.AllowPoolGrowth);
    }

    private bool IncludesBattleManagers()
    {
        BootstrapManagerSet effectiveSet = managerSet;
        if (effectiveSet == BootstrapManagerSet.AutoByPostFlow)
        {
            effectiveSet = postBootstrapFlow == BootstrapPostFlow.OpenMainMenu
                ? BootstrapManagerSet.MainScene
                : BootstrapManagerSet.BattleScene;
        }

        return effectiveSet == BootstrapManagerSet.BattleScene || effectiveSet == BootstrapManagerSet.All;
    }

    private void RegisterSystem<T>(T system) where T : class, IGameSystem
    {
        if (system == null)
        {
            return;
        }

        ServiceLocator.Register(system);
        systems.Add(system);
    }

    private T ResolveOrCreate<T>(T current) where T : Component
    {
        if (current != null)
        {
            return current;
        }

        T childComponent = GetComponentInChildren<T>(true);
        if (childComponent != null)
        {
            return childComponent;
        }

        T sceneComponent = FindFirstObjectByType<T>();
        return sceneComponent != null ? sceneComponent : gameObject.AddComponent<T>();
    }
}
