using UnityEngine;

/// <summary>
/// 敌人生成：由 <see cref="WaveManager"/> 驱动，从对象池取出并初始化 <see cref="EnemyController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。推荐挂在 <c>GameSystems</c> 或场景 <c>EnemyGenerateManager</c> 同级物体。</para>
/// </remarks>
public class EnemySpawnerManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private bool disableLegacySpawnerOnInit = true;

    [Header("Spawn Area")]
    [SerializeField] private SpawnAreaController spawnArea;

    private readonly WaveSpawnSelector spawnSelector = new WaveSpawnSelector();
    private readonly WaveSpecialEnemySelector specialSelector = new WaveSpecialEnemySelector();
    private WaveDataSO activeWaveData;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public int AliveEnemyCount { get; private set; }
    public int AliveBossCount { get; private set; }

    public void Initialize()
    {
        if (spawnArea == null)
        {
            spawnArea = GetComponent<SpawnAreaController>();
        }

        if (spawnArea == null)
        {
            spawnArea = gameObject.AddComponent<SpawnAreaController>();
        }

        bool mapConfigured = ServiceLocator.TryGet(out MapManager mapManager) && mapManager.IsMapLoaded;
        if (!mapConfigured)
        {
            spawnArea.BeginInitialize();
        }
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = false;
        activeWaveData = null;
    }

    public void ConfigureWavePool(WaveDataSO waveData)
    {
        activeWaveData = waveData;
        if (!ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return;
        }

        spawnSelector.Configure(waveData, configManager);
        specialSelector.Configure(waveData, configManager);
    }

    public bool TrySpawnSpecialEnemy(string configId, float statMultiplier, int waveIndex)
    {
        if (string.IsNullOrEmpty(configId))
        {
            return false;
        }

        SpecialEnemySpawnContext.Set(waveIndex, statMultiplier);
        return SpawnEnemyInternal(configId, statMultiplier, waveIndex, markAsElite: false, markAsSpecial: true);
    }

    public bool TrySpawnEnemy(
        string forcedConfigId,
        float statMultiplier,
        int waveIndex,
        float waveElapsedSeconds = 0f,
        bool markAsElite = false)
    {
        if (spawnArea == null || !spawnArea.IsReady)
        {
            return false;
        }

        string configId = forcedConfigId;
        if (string.IsNullOrEmpty(configId))
        {
            configId = spawnSelector.PickEnemyId(waveElapsedSeconds);
        }

        float entryMultiplier = spawnSelector.GetEntryStatMultiplier(configId, statMultiplier);
        return SpawnEnemyInternal(configId, entryMultiplier, waveIndex, markAsElite, markAsSpecial: false);
    }

    public bool TrySpawnBoss(string bossConfigId, float statMultiplier, int waveIndex)
    {
        if (string.IsNullOrEmpty(bossConfigId) ||
            !ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetBoss(bossConfigId, out BossDataSO bossData))
        {
            Debug.LogWarning($"[EnemySpawnerManager] Boss 配置缺失 configId={bossConfigId}");
            return false;
        }

        return SpawnBossInternal(bossConfigId, bossData.BaseEnemyConfigId, statMultiplier, waveIndex);
    }

    private bool SpawnBossInternal(
        string bossConfigId,
        string enemyConfigId,
        float statMultiplier,
        int waveIndex)
    {
        if (!spawnArea.TryGetRandomSpawnPosition(out Vector3 position))
        {
            return false;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance) &&
            !performance.TryAcquire(PerformanceBudgetCategory.Enemy))
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetEnemy(enemyConfigId, out EnemyDataSO enemyData))
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfRollback))
            {
                perfRollback.Release(PerformanceBudgetCategory.Enemy);
            }

            GameDebug.LogWarning($"[EnemySpawnerManager] Boss 基础敌人配置缺失 configId={enemyConfigId}");
            return false;
        }

        GameObject instance = SpawnFromPool(enemyData, position);
        if (instance == null || !instance.TryGetComponent(out EnemyController controller))
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfFail))
            {
                perfFail.Release(PerformanceBudgetCategory.Enemy);
            }

            return false;
        }

        if (instance.TryGetComponent(out Enemy enemy))
        {
            enemy.SetBossFlag(true);
        }

        controller.InitializeForSpawn(enemyConfigId, statMultiplier, waveIndex);
        EnsureBossController(instance).Initialize(bossConfigId, statMultiplier);

        AliveEnemyCount++;
        AliveBossCount++;
        return true;
    }

    public bool TrySpawnBonusSpecial(float statMultiplier, int waveIndex)
    {
        if (!specialSelector.TryPick(out string configId))
        {
            return false;
        }

        float entryMultiplier = spawnSelector.GetEntryStatMultiplier(configId, statMultiplier);
        return TrySpawnSpecialEnemy(configId, entryMultiplier, waveIndex);
    }

    private bool SpawnEnemyInternal(
        string configId,
        float statMultiplier,
        int waveIndex,
        bool markAsElite,
        bool markAsSpecial)
    {
        if (!spawnArea.TryGetRandomSpawnPosition(out Vector3 position))
        {
            return false;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance) &&
            !performance.TryAcquire(PerformanceBudgetCategory.Enemy))
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetEnemy(configId, out EnemyDataSO enemyData))
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfRollback))
            {
                perfRollback.Release(PerformanceBudgetCategory.Enemy);
            }
            GameDebug.LogWarning($"[EnemySpawnerManager] 无法生成敌人，配置缺失 configId={configId}");
            return false;
        }

        GameObject instance = SpawnFromPool(enemyData, position);
        if (instance == null)
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfFail))
            {
                perfFail.Release(PerformanceBudgetCategory.Enemy);
            }

            return false;
        }

        if (!instance.TryGetComponent(out EnemyController controller))
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfMissing))
            {
                perfMissing.Release(PerformanceBudgetCategory.Enemy);
            }

            GameDebug.LogWarning($"[EnemySpawnerManager] Prefab 缺少 EnemyController configId={configId}", instance);
            return false;
        }

        if (markAsElite && instance.TryGetComponent(out Enemy enemy))
        {
            enemy.SetEliteFlag(true);
        }

        controller.InitializeForSpawn(configId, statMultiplier, waveIndex, markAsElite, markAsSpecial);

        if (markAsElite)
        {
            EnsureEliteController(instance).Initialize(configId, RunDifficultyContext.IsEliteMode);
        }

        AliveEnemyCount++;
        return true;
    }

    private static BossController EnsureBossController(GameObject instance)
    {
        if (!instance.TryGetComponent(out BossController controller))
        {
            controller = instance.AddComponent<BossController>();
        }

        return controller;
    }

    private static EliteController EnsureEliteController(GameObject instance)
    {
        if (!instance.TryGetComponent(out EliteController controller))
        {
            controller = instance.AddComponent<EliteController>();
        }

        return controller;
    }

    private GameObject SpawnFromPool(EnemyDataSO data, Vector3 position)
    {
        string poolKey = data.PoolKey;
        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.HasPool(poolKey))
        {
            return poolManager.Spawn(poolKey, position, Quaternion.identity);
        }

        if (data.Prefab != null)
        {
            GameDebug.LogWarning($"[EnemySpawnerManager] 对象池缺失，直接实例化敌人 configId={data.ConfigId} poolKey={poolKey}");
            return Instantiate(data.Prefab, position, Quaternion.identity);
        }

        Debug.LogWarning($"[EnemySpawnerManager] 无法生成敌人 configId={data.ConfigId}");
        return null;
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is not EnemyEventArgs args)
        {
            return;
        }

        AliveEnemyCount = Mathf.Max(0, AliveEnemyCount - 1);

        if (args.EnemyObject != null &&
            args.EnemyObject.TryGetComponent(out Enemy enemy) &&
            enemy.IsBoss)
        {
            AliveBossCount = Mathf.Max(0, AliveBossCount - 1);
        }
    }
}
