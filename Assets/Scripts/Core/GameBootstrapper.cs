using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏启动引导器，负责在场景加载后按固定顺序初始化各 Manager 并注册到 <see cref="ServiceLocator"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是，必须挂载到场景中的常驻物体上。</para>
/// <para><b>推荐挂载对象：</b>在首个可玩场景中创建空物体，命名为 <c>GameSystems</c>（或 <c>[Bootstrap]</c>），将本组件挂在该物体根节点上。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、Wall、UI Canvas 等玩法或表现物体上。</para>
/// <para><b>Inspector 配置：</b></para>
/// <list type="bullet">
/// <item><description>将同物体或子物体上的 <see cref="ConfigManager"/>、<see cref="PoolManager"/>、<see cref="GameManager"/> 拖入对应槽位；留空时会在 Awake 时自动查找或在本物体上 AddComponent。</description></item>
/// <item><description><c>Dont Destroy On Load</c> 建议开启，保证跨场景保留引导流程（单例冲突时会销毁重复实例）。</description></item>
/// </list>
/// <para><b>启动顺序：</b>ConfigManager → EventBus（代码创建）→ PoolManager → GameManager → GameFlowManager。</para>
/// <para><b>获取方式：</b><c>GameBootstrapper.Instance</c> 或 <c>ServiceLocator.Get&lt;GameBootstrapper&gt;()</c>（Bootstrap 完成后）。</para>
/// </remarks>
public class GameBootstrapper : MonoSingleton<GameBootstrapper>
{
    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Managers")]
    [SerializeField] private ConfigManager configManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameFlowManager gameFlowManager;

    private readonly List<IGameSystem> systems = new List<IGameSystem>(8);
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

        if (configManager.GameConfig == null || configManager.GameConfig.StartGameOnBootstrap)
        {
            gameManager.StartGame();
        }
    }

    private void ResolveManagers()
    {
        configManager = ResolveOrCreate(configManager);
        poolManager = ResolveOrCreate(poolManager);
        gameManager = ResolveOrCreate(gameManager);
        gameFlowManager = ResolveOrCreate(gameFlowManager);
        eventBus = new EventBus();
    }

    private void RegisterServices()
    {
        systems.Clear();

        ServiceLocator.Register(this);
        ServiceLocator.Register(configManager);
        ServiceLocator.Register(eventBus);
        ServiceLocator.Register(poolManager);
        ServiceLocator.Register(gameManager);
        ServiceLocator.Register(gameFlowManager);

        systems.Add(configManager);
        systems.Add(eventBus);
        systems.Add(poolManager);
        systems.Add(gameManager);
        systems.Add(gameFlowManager);
    }

    private void InitializeSystems()
    {
        for (int i = 0; i < systems.Count; i++)
        {
            systems[i].Initialize();
        }
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
