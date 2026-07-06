using System.Collections.Generic;
using UnityEngine;

/// <summary>Bootstrap 完成后要执行的游戏流程分支。</summary>
public enum BootstrapPostFlow
{
    /// <summary>读取 <see cref="GameConfig.StartGameOnBootstrap"/> 决定是否开局。</summary>
    UseGameConfig = 0,
    /// <summary>直接调用 <see cref="GameManager.StartGame"/>。</summary>
    StartGame = 1,
    /// <summary>打开主菜单。</summary>
    OpenMainMenu = 2,
    /// <summary>不执行任何后续流程。</summary>
    None = 3,
}

/// <summary>Bootstrap 时要注册并初始化的 Manager 集合范围。</summary>
public enum BootstrapManagerSet
{
    /// <summary>根据 <see cref="BootstrapPostFlow"/> 自动选择主场景或战斗场景集合。</summary>
    AutoByPostFlow = 0,
    /// <summary>仅注册主场景 Meta/UI 相关 Manager。</summary>
    MainScene = 1,
    /// <summary>注册战斗场景所需的完整 Manager 集合。</summary>
    BattleScene = 2,
    /// <summary>注册全部 Manager。</summary>
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
/// <para><b>成员作用域：</b></para>
/// <list type="bullet">
/// <item><description><b>主场景</b> — 由 <see cref="MainSceneBuilder"/> 写入的 Lifecycle 默认值，或 <see cref="BootstrapManagerSet.MainScene"/> 下不会解析/注册的 Manager。</description></item>
/// <item><description><b>战斗场景</b> — <see cref="IncludesBattleManagers"/> 为 true 时才会解析、注册并初始化（BattleScene / All / AutoByPostFlow 且非 OpenMainMenu）。</description></item>
/// <item><description><b>全局</b> — 任意 Bootstrap 模式均会解析、注册并初始化。</description></item>
/// </list>
/// <para><b>初始化三阶段：</b>ResolveManagers（解析/创建组件）→ RegisterServices（写入 ServiceLocator 并确定 Initialize 顺序）→ InitializeSystems（依次调用 <see cref="IGameSystem.Initialize"/>）。</para>
/// <para><b>获取方式：</b><c>GameBootstrapper.Instance</c> 或 <c>ServiceLocator.Get&lt;GameBootstrapper&gt;()</c>（Bootstrap 完成后）。</para>
/// </remarks>
public class GameBootstrapper : MonoSingleton<GameBootstrapper>
{
    // ── Lifecycle（Inspector 配置；MainSceneBuilder 与 BattleScene 默认值不同）──

    /// <summary>
    /// [作用域: 全局]
    /// Awake 认领单例后是否立即调用 <see cref="Bootstrap"/>。
    /// 初始化顺序：Bootstrap 总入口（先于 Resolve #1）。
    /// MainSceneBuilder / BattleScene 均设为 true。
    /// </summary>
    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnAwake = true;

    /// <summary>
    /// [作用域: 主场景配置项]
    /// 单例是否跨场景保留（映射到 <see cref="Options"/>）。
    /// 初始化顺序：MonoSingleton Awake（Bootstrap 之前）。
    /// MainSceneBuilder 设为 false；BattleScene 设为 true。
    /// </summary>
    [SerializeField] private bool dontDestroyOnLoad = true;

    /// <summary>
    /// [作用域: 主场景 / 战斗场景（分支不同）]
    /// Bootstrap 完成后触发的游戏流程。
    /// 初始化顺序：Bootstrap 最后一步 <see cref="ApplyPostBootstrapFlow"/>（全部 Manager Initialize 之后）。
    /// MainSceneBuilder 设为 <see cref="BootstrapPostFlow.OpenMainMenu"/>；
    /// BattleScene 设为 <see cref="BootstrapPostFlow.UseGameConfig"/>（可按 GameConfig 开局）。
    /// </summary>
    [SerializeField] private BootstrapPostFlow postBootstrapFlow = BootstrapPostFlow.UseGameConfig;

    /// <summary>
    /// [作用域: 主场景 / 战斗场景（决定 Manager 子集）]
    /// 要注册并初始化的 Manager 集合范围；配合 <see cref="IncludesBattleManagers"/> 过滤战斗 Manager。
    /// 初始化顺序：Bootstrap 开始时读取（Resolve / Register 之前）。
    /// MainSceneBuilder 设为 <see cref="BootstrapManagerSet.MainScene"/>；
    /// BattleScene 设为 <see cref="BootstrapManagerSet.AutoByPostFlow"/>（非 OpenMainMenu 时等价 BattleScene 全集）。
    /// </summary>
    [SerializeField] private BootstrapManagerSet managerSet = BootstrapManagerSet.AutoByPostFlow;

    // ── Managers — 全局（任意 Bootstrap 模式均 Resolve / Register / Initialize）──

    /// <summary>
    /// [作用域: 全局] 游戏配置加载与访问。
    /// 初始化顺序：Resolve #1 → Register #1 → Initialize #1。
    /// </summary>
    [Header("Managers")]
    [SerializeField] private ConfigManager configManager;

    /// <summary>
    /// [作用域: 全局] 存档读写。
    /// 初始化顺序：Resolve #2 → Register #2 → Initialize #2。
    /// </summary>
    [SerializeField] private SaveManager saveManager;

    /// <summary>
    /// [作用域: 全局] 货币与资源经济。
    /// 初始化顺序：Resolve #3 → Register #3 → Initialize #3。
    /// </summary>
    [SerializeField] private ResourceManager resourceManager;

    /// <summary>
    /// [作用域: 全局] 商城与内购逻辑。
    /// 初始化顺序：Resolve #4 → Register #4 → Initialize #4。
    /// </summary>
    [SerializeField] private ShopManager shopManager;

    /// <summary>
    /// [作用域: 全局] 激励广告奖励发放。
    /// 初始化顺序：Resolve #5 → Register #5 → Initialize #5。
    /// </summary>
    [SerializeField] private AdRewardService adRewardService;

    /// <summary>
    /// [作用域: 全局] 成就进度与解锁。
    /// 初始化顺序：Resolve #6 → Register #6 → Initialize #6。
    /// </summary>
    [SerializeField] private AchievementManager achievementManager;

    /// <summary>
    /// [作用域: 全局] 每日签到奖励。
    /// 初始化顺序：Resolve #7 → Register #7 → Initialize #7。
    /// </summary>
    [SerializeField] private DailyRewardManager dailyRewardManager;

    /// <summary>
    /// [作用域: 全局] 升级卡牌 Meta 进度。
    /// 初始化顺序：Resolve #8 → Register #8 → Initialize #8。
    /// </summary>
    [SerializeField] private UpgradeCardManager upgradeCardManager;

    /// <summary>
    /// [作用域: 全局] 局外 Buff 解锁路径与解锁卡库存。
    /// </summary>
    [SerializeField] private BuffUnlockService buffUnlockService;

    /// <summary>
    /// [作用域: 全局] Meta 层通用奖励结算。
    /// 初始化顺序：Resolve #9 → Register #9 → Initialize #9。
    /// </summary>
    [SerializeField] private MetaRewardService metaRewardService;

    /// <summary>
    /// [作用域: 全局] 天赋树与天赋效果。
    /// 初始化顺序：Resolve #10 → Register #10 → Initialize #10。
    /// </summary>
    [SerializeField] private TalentManager talentManager;

    /// <summary>
    /// [作用域: 全局] 装备穿戴与属性。
    /// 初始化顺序：Resolve #11 → Register #11 → Initialize #11。
    /// </summary>
    [SerializeField] private EquipmentManager equipmentManager;

    /// <summary>
    /// [作用域: 全局] 性能档位与画质策略。
    /// 初始化顺序：Resolve #12 → Register #13 → Initialize #13（Register 在 EventBus 之后）。
    /// </summary>
    [SerializeField] private PerformanceManager performanceManager;

    /// <summary>
    /// [作用域: 全局] 对象池；Initialize 前会执行 <see cref="ApplyPoolRuntimePolicy"/>。
    /// 初始化顺序：Resolve #13 → Register #14 → Initialize #14。
    /// </summary>
    [SerializeField] private PoolManager poolManager;

    /// <summary>
    /// [作用域: 全局] 游戏状态机（菜单/战斗/暂停等）；<see cref="ApplyPostBootstrapFlow"/> 依赖本实例。
    /// 初始化顺序：Resolve #14 → Register #15 → Initialize #15。
    /// </summary>
    [SerializeField] private GameManager gameManager;

    /// <summary>
    /// [作用域: 全局] 测试用运行倍速（Time.timeScale 快进，1~5x）。
    /// </summary>
    [SerializeField] private GameRunSpeedController gameRunSpeedController;

    /// <summary>
    /// [作用域: 全局] 技能解锁与 Meta 进度。
    /// 初始化顺序：Resolve #15 → Register #16（主场景）/ #25（战斗场景）→ Initialize 同 Register 序号。
    /// </summary>
    [SerializeField] private SkillUnlockService skillUnlockService;

    /// <summary>
    /// [作用域: 全局] 内容表与配置注册表。
    /// 初始化顺序：Resolve #16 → Register #17（主场景）/ #26（战斗场景）→ Initialize 同 Register 序号。
    /// </summary>
    [SerializeField] private ContentRegistry contentRegistry;

    /// <summary>
    /// [作用域: 全局] 音频播放与 BGM 管理。
    /// 初始化顺序：Resolve #17 → Register #18（主场景）/ #29（战斗场景）→ Initialize 同 Register 序号。
    /// </summary>
    [SerializeField] private AudioManager audioManager;

    // ── Managers — 战斗场景（IncludesBattleManagers 为 true 时才 Resolve / Register / Initialize）──

    /// <summary>
    /// [作用域: 战斗场景] 单局流程（开局/结算/重开）。
    /// 初始化顺序：Resolve #18 → Register #16 → Initialize #16。
    /// </summary>
    [SerializeField] private GameFlowManager gameFlowManager;

    /// <summary>
    /// [作用域: 战斗场景] 当前 Run 会话统计。
    /// 初始化顺序：Resolve #19 → Register #17 → Initialize #17。
    /// </summary>
    [SerializeField] private RunSessionTracker runSessionTracker;

    /// <summary>
    /// [作用域: 战斗场景] Run 结束奖励结算。
    /// 初始化顺序：Resolve #20 → Register #18 → Initialize #18。
    /// </summary>
    [SerializeField] private RunRewardSettlementService runRewardSettlementService;

    /// <summary>
    /// [作用域: 战斗场景] 局内玩家经验与升级。
    /// 初始化顺序：Resolve #21 → Register #19 → Initialize #19。
    /// </summary>
    [SerializeField] private PlayerExperienceService playerExperienceService;

    /// <summary>
    /// [作用域: 战斗场景] 局内升级选项与刷新。
    /// 初始化顺序：Resolve #22 → Register #20 → Initialize #20。
    /// </summary>
    [SerializeField] private UpgradeManager upgradeManager;

    /// <summary>
    /// [作用域: 战斗场景] 随机奖励掉落。
    /// 初始化顺序：Resolve #23 → Register #21 → Initialize #21。
    /// </summary>
    [SerializeField] private RandomRewardManager randomRewardManager;

    /// <summary>
    /// [作用域: 战斗场景] 伤害计算与结算管线。
    /// 初始化顺序：Resolve #27 → Register #22 → Initialize #22。
    /// </summary>
    [SerializeField] private DamageSystem damageSystem;

    /// <summary>
    /// [作用域: 战斗场景] 碰撞检测与命中分发。
    /// 初始化顺序：Resolve #28 → Register #23 → Initialize #23。
    /// </summary>
    [SerializeField] private CollisionManager collisionManager;

    /// <summary>
    /// [作用域: 战斗场景] 投射物生命周期管理。
    /// 初始化顺序：Resolve #29 → Register #24 → Initialize #24。
    /// </summary>
    [SerializeField] private ProjectileManager projectileManager;

    /// <summary>
    /// [作用域: 战斗场景] 地图生成与区块管理。
    /// 初始化顺序：Resolve #30 → Register #27 → Initialize #27。
    /// </summary>
    [SerializeField] private MapManager mapManager;

    /// <summary>
    /// [作用域: 战斗场景] 局内随机事件调度。
    /// 初始化顺序：Resolve #31 → Register #28 → Initialize #28。
    /// </summary>
    [SerializeField] private GameplayEventManager gameplayEventManager;

    /// <summary>
    /// [作用域: 战斗场景] 局内事件调试桥接（Editor / 开发用）。
    /// 初始化顺序：Resolve #32 → Register #30 → Initialize #30。
    /// </summary>
    [SerializeField] private GameplayEventDebugBridge gameplayEventDebugBridge;

    /// <summary>
    /// [作用域: 战斗场景] 敌人生成与池化调度。
    /// 初始化顺序：Resolve #24 → Register #31 → Initialize #31。
    /// </summary>
    [SerializeField] private EnemySpawnerManager enemySpawnerManager;

    /// <summary>
    /// [作用域: 战斗场景] 波次推进与难度曲线。
    /// 初始化顺序：Resolve #25 → Register #32 → Initialize #32。
    /// </summary>
    [SerializeField] private WaveManager waveManager;

    /// <summary>
    /// [作用域: 战斗场景] Boss 战 Run 统计回传。
    /// 初始化顺序：Resolve #26 → Register #33 → Initialize #33（战斗模式下最后注册的系统）。
    /// </summary>
    [SerializeField] private BossRunStatsBridge bossRunStatsBridge;

    // ── 运行时状态（非 Inspector 序列化）──

    /// <summary>
    /// [作用域: 全局] 已注册且需每帧 <see cref="IGameSystem.Tick"/> 的系统列表；顺序与 Register 一致。
    /// 初始化顺序：RegisterServices 开始时 Clear，随后按 Register 顺序 Add。
    /// </summary>
    private readonly List<IGameSystem> systems = new List<IGameSystem>(16);

    /// <summary>
    /// [作用域: 全局] 全局事件总线；非 MonoBehaviour，由代码 new 创建。
    /// 初始化顺序：Resolve 最后一步 #33（所有 Manager 解析完成后）→ Register #12 → Initialize #12。
    /// </summary>
    private EventBus eventBus;

    /// <summary>
    /// [作用域: 全局] Bootstrap 是否已完成；防止重复初始化。
    /// 初始化顺序：InitializeSystems 与 ApplyPostBootstrapFlow 之间置为 true。
    /// </summary>
    private bool isBootstrapped;

    /// <summary>根据 Inspector 配置决定单例是否跨场景保留。</summary>
    protected override SingletonOptions Options =>
        dontDestroyOnLoad ? SingletonOptions.PersistentDefault : SingletonOptions.SceneDefault;

    /// <summary>单例认领成功后，按配置决定是否立即 Bootstrap。</summary>
    protected override void OnSingletonAwake()
    {
        if (initializeOnAwake)
        {
            Bootstrap();
        }
    }

    /// <summary>每帧驱动已注册 <see cref="IGameSystem"/> 的 <see cref="IGameSystem.Tick"/>。</summary>
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

    /// <summary>逆序关闭各系统并清空 <see cref="ServiceLocator"/>。</summary>
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

    /// <summary>Bootstrap 完成后按 <see cref="BootstrapPostFlow"/> 启动主菜单或开局。</summary>
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

    /// <summary>解析 Inspector 引用或在场景中查找/创建各 Manager 组件。</summary>
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
        buffUnlockService = ResolveOrCreate(buffUnlockService);
        metaRewardService = ResolveOrCreate(metaRewardService);
        talentManager = ResolveOrCreate(talentManager);
        equipmentManager = ResolveOrCreate(equipmentManager);
        performanceManager = ResolveOrCreate(performanceManager);
        poolManager = ResolveOrCreate(poolManager);
        gameManager = ResolveOrCreate(gameManager);
        gameRunSpeedController = ResolveOrCreate(gameRunSpeedController);
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

    /// <summary>将 Manager 与 <see cref="EventBus"/> 注册到 <see cref="ServiceLocator"/> 并加入 Tick 列表。</summary>
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
        RegisterSystem(buffUnlockService);
        RegisterSystem(metaRewardService);
        RegisterSystem(talentManager);
        RegisterSystem(equipmentManager);
        RegisterSystem(eventBus);
        RegisterSystem(performanceManager);
        RegisterSystem(poolManager);
        RegisterSystem(gameManager);
        RegisterSystem(gameRunSpeedController);

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

    /// <summary>按注册顺序初始化各系统，并应用难度与对象池运行时策略。</summary>
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

        GameConfig gameConfig = configManager != null ? configManager.GameConfig : null;
        ConfigDatabaseSO database = configManager != null ? configManager.Database : null;
        RunDifficultyBootstrap.ApplyFromGameConfig(gameConfig);
        WaveProgressionBootstrap.ApplyFromGameConfig(gameConfig, database);
        PlaytestBootstrap.Initialize(gameConfig);
    }

    /// <summary>从 <see cref="GameConfig"/> 读取预热数量与扩容策略并配置 <see cref="PoolManager"/>。</summary>
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

    /// <summary>判断当前 Bootstrap 配置是否应包含战斗场景 Manager。</summary>
    /// <returns>需要战斗 Manager 时返回 true。</returns>
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

    /// <summary>注册单个 <see cref="IGameSystem"/> 到定位器与 Tick 列表。</summary>
    /// <typeparam name="T">系统类型。</typeparam>
    /// <param name="system">系统实例；为 null 时跳过。</param>
    private void RegisterSystem<T>(T system) where T : class, IGameSystem
    {
        if (system == null)
        {
            return;
        }

        ServiceLocator.Register(system);
        systems.Add(system);
    }

    /// <summary>优先使用已有引用，否则在子物体、场景或本物体上查找/创建组件。</summary>
    /// <typeparam name="T">组件类型。</typeparam>
    /// <param name="current">Inspector 已指定引用。</param>
    /// <returns>解析到的组件实例。</returns>
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
