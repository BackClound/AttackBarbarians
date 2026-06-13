using UnityEngine;

/// <summary>
/// 性能与移动端配置：预算计数、画质档位、敌人逻辑降频、日志门控。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>GameSystems/Performance</c>，由 <see cref="GameBootstrapper"/> 注册。</para>
/// </remarks>
public class PerformanceManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private PerformanceBudgetSO budgetOverride;

    private readonly int[] activeCounts = new int[4];
    private PerformanceBudgetSO activeBudget;
    private bool isMobileProfile;
    private bool isInitialized;
    private Vector2 playerFocusPoint;
    private bool hasPlayerFocus;
    private int enemyThrottleFrame;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>当前是否处于移动端性能配置档。</summary>
    public bool IsMobileProfile => isMobileProfile;

    /// <summary>当前生效的性能预算配置。</summary>
    public PerformanceBudgetSO ActiveBudget => activeBudget;

    /// <summary>运行时日志是否启用。</summary>
    public bool RuntimeLogsEnabled { get; private set; }

    /// <summary>
    /// 解析预算配置、应用平台档位并订阅事件。
    /// </summary>
    public void Initialize()
    {
        ResolveBudget();
        SyncRuntimeLogs();
        ApplyPlatformProfile();
        ApplyGraphicsFromSave();
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    /// <summary>
    /// 每帧刷新玩家焦点位置，供敌人降频逻辑使用。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        RefreshPlayerFocus();
    }

    /// <summary>
    /// 取消事件订阅并重置活跃计数。
    /// </summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        for (int i = 0; i < activeCounts.Length; i++)
        {
            activeCounts[i] = 0;
        }

        isInitialized = false;
    }

    /// <summary>
    /// 获取指定预算分类的当前上限（含移动端缩放）。
    /// </summary>
    /// <param name="category">预算分类。</param>
    /// <returns>上限值，无配置时返回 0。</returns>
    public int GetLimit(PerformanceBudgetCategory category) =>
        activeBudget != null ? activeBudget.GetLimit(category, isMobileProfile) : 0;

    /// <summary>
    /// 获取指定预算分类的当前活跃数量。
    /// </summary>
    /// <param name="category">预算分类。</param>
    /// <returns>当前活跃计数。</returns>
    public int GetActiveCount(PerformanceBudgetCategory category) =>
        activeCounts[(int)category];

    /// <summary>
    /// 获取最大并发音效数（含移动端缩放）。
    /// </summary>
    /// <returns>并发上限，无配置时返回 12。</returns>
    public int GetMaxConcurrentSfx()
    {
        if (activeBudget == null)
        {
            return 12;
        }

        int limit = activeBudget.MaxConcurrentSfx;
        if (!isMobileProfile || activeBudget.MobileBudgetScale >= 0.999f)
        {
            return limit;
        }

        return Mathf.Max(1, Mathf.RoundToInt(limit * activeBudget.MobileBudgetScale));
    }

    /// <summary>
    /// 尝试占用一个预算槽位。
    /// </summary>
    /// <param name="category">预算分类。</param>
    /// <returns>占用成功或不限流时返回 true，已达上限返回 false。</returns>
    public bool TryAcquire(PerformanceBudgetCategory category)
    {
        int limit = GetLimit(category);
        if (limit <= 0)
        {
            return true;
        }

        int index = (int)category;
        if (activeCounts[index] >= limit)
        {
            return false;
        }

        activeCounts[index]++;
        return true;
    }

    /// <summary>
    /// 释放一个已占用的预算槽位。
    /// </summary>
    /// <param name="category">预算分类。</param>
    public void Release(PerformanceBudgetCategory category)
    {
        int index = (int)category;
        if (activeCounts[index] > 0)
        {
            activeCounts[index]--;
        }
    }

    /// <summary>
    /// 判断敌人是否在本帧执行完整 Update（远距离非 Boss/精英按间隔降频）。
    /// </summary>
    /// <param name="enemyTransform">敌人 Transform。</param>
    /// <param name="isBossOrElite">是否为 Boss 或精英（始终完整更新）。</param>
    /// <returns>本帧应执行完整 Update 返回 true，否则返回 false。</returns>
    public bool ShouldRunEnemyUpdateThisFrame(Transform enemyTransform, bool isBossOrElite)
    {
        if (enemyTransform == null || activeBudget == null)
        {
            return true;
        }

        if (isBossOrElite)
        {
            return true;
        }

        RefreshPlayerFocus();
        if (!hasPlayerFocus)
        {
            return true;
        }

        float distSqr = ((Vector2)enemyTransform.position - playerFocusPoint).sqrMagnitude;
        if (distSqr <= activeBudget.EnemyNearDistanceSqr)
        {
            return true;
        }

        int interval = activeBudget.EnemyFarUpdateInterval;
        int bucket = (enemyTransform.GetInstanceID() & 0x7FFFFFFF) % interval;
        return (enemyThrottleFrame % interval) == bucket;
    }

    /// <summary>
    /// 根据存档设置与移动端偏移应用画质档位。
    /// </summary>
    public void ApplyGraphicsFromSave()
    {
        if (activeBudget == null)
        {
            return;
        }

        int qualityLevel = QualitySettings.GetQualityLevel();
        if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current?.settings != null)
        {
            int saved = saveManager.Current.settings.graphicsQualityLevel;
            if (saved >= 0 && saved < QualitySettings.names.Length)
            {
                qualityLevel = saved;
            }
        }

        if (isMobileProfile)
        {
            qualityLevel += activeBudget.MobileQualityLevelOffset;
        }

        qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges: true);
    }

    /// <summary>
    /// 解析生效的性能预算配置（Inspector 覆盖 → ConfigManager → Resources）。
    /// </summary>
    private void ResolveBudget()
    {
        activeBudget = budgetOverride;
        if (activeBudget == null && ServiceLocator.TryGet(out ConfigManager configManager))
        {
            activeBudget = configManager.PerformanceBudget;
        }

        if (activeBudget == null)
        {
            activeBudget = Resources.Load<PerformanceBudgetSO>(GameConstants.ResourcePaths.PerformanceBudget);
        }

        if (activeBudget == null)
        {
            Debug.LogWarning("[PerformanceManager] 未找到 PerformanceBudgetSO，预算限流与移动端配置将使用宽松默认值。");
        }
    }

    /// <summary>
    /// 同步运行时日志开关至 <see cref="GameDebug"/>。
    /// </summary>
    private void SyncRuntimeLogs()
    {
        RuntimeLogsEnabled = ServiceLocator.TryGet(out ConfigManager config) &&
                             config.GameConfig != null &&
                             config.GameConfig.EnableRuntimeLogs;
        GameDebug.SetRuntimeLogsEnabled(RuntimeLogsEnabled);
    }

    /// <summary>
    /// 根据运行平台应用移动端帧率等性能配置。
    /// </summary>
    private void ApplyPlatformProfile()
    {
        isMobileProfile = Application.isMobilePlatform;
#if UNITY_EDITOR
        if (activeBudget != null && activeBudget.ApplyMobileProfileOnBootstrap && !Application.isMobilePlatform)
        {
            // 编辑器保持桌面档，仅应用帧率与预算缩放可通过单独开关扩展。
        }
#endif
        if (isMobileProfile && activeBudget != null)
        {
            if (activeBudget.ApplyMobileProfileOnBootstrap)
            {
                Application.targetFrameRate = activeBudget.MobileTargetFrameRate;
            }
        }
    }

    /// <summary>
    /// 刷新玩家焦点坐标，供敌人 Update 降频判定使用。
    /// </summary>
    private void RefreshPlayerFocus()
    {
        hasPlayerFocus = false;
        if (PlayerSceneAccess.TryGetPlayer(out Player player) && player != null)
        {
            playerFocusPoint = player.transform.position;
            hasPlayerFocus = true;
            return;
        }

        if (WallControlManager.HasInstance)
        {
            playerFocusPoint = WallControlManager.Instance.transform.position;
            hasPlayerFocus = true;
        }
    }

    /// <summary>
    /// 敌人击杀事件回调：释放敌人预算槽位。
    /// </summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        Release(PerformanceBudgetCategory.Enemy);
    }

    /// <summary>
    /// 每帧递增敌人降频分桶帧计数。
    /// </summary>
    private void LateUpdate()
    {
        enemyThrottleFrame++;
    }
}
