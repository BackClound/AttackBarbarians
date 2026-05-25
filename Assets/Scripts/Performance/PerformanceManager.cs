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

    public bool IsInitialized => isInitialized;
    public bool IsMobileProfile => isMobileProfile;
    public PerformanceBudgetSO ActiveBudget => activeBudget;
    public bool RuntimeLogsEnabled { get; private set; }

    public void Initialize()
    {
        ResolveBudget();
        SyncRuntimeLogs();
        ApplyPlatformProfile();
        ApplyGraphicsFromSave();
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        RefreshPlayerFocus();
    }

    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        for (int i = 0; i < activeCounts.Length; i++)
        {
            activeCounts[i] = 0;
        }

        isInitialized = false;
    }

    public int GetLimit(PerformanceBudgetCategory category) =>
        activeBudget != null ? activeBudget.GetLimit(category, isMobileProfile) : 0;

    public int GetActiveCount(PerformanceBudgetCategory category) =>
        activeCounts[(int)category];

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

    public void Release(PerformanceBudgetCategory category)
    {
        int index = (int)category;
        if (activeCounts[index] > 0)
        {
            activeCounts[index]--;
        }
    }

    /// <summary>敌人 <see cref="EnemyController"/> 是否在本帧执行完整 Update。</summary>
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

    private void SyncRuntimeLogs()
    {
        RuntimeLogsEnabled = ServiceLocator.TryGet(out ConfigManager config) &&
                             config.GameConfig != null &&
                             config.GameConfig.EnableRuntimeLogs;
        GameDebug.SetRuntimeLogsEnabled(RuntimeLogsEnabled);
    }

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

    private void OnEnemyKilled(GameEventContext ctx)
    {
        Release(PerformanceBudgetCategory.Enemy);
    }

    private void LateUpdate()
    {
        enemyThrottleFrame++;
    }
}
