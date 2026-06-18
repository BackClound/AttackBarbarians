using UnityEngine;

/// <summary>
/// 敌人生成：由 <see cref="WaveManager"/> 驱动，从对象池取出并初始化 <see cref="EnemyController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。推荐挂在 <c>GameSystems</c> 下（与 <see cref="WaveManager"/> 同级）。</para>
/// </remarks>
public class EnemySpawnerManager : MonoBehaviour, IGameSystem
{
    [Header("Spawn Area")]
    [SerializeField] private SpawnAreaController spawnArea;

    private readonly WaveSpawnSelector spawnSelector = new WaveSpawnSelector();
    private readonly WaveSpecialEnemySelector specialSelector = new WaveSpecialEnemySelector();
    private WaveDataSO activeWaveData;

    private bool isInitialized;

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前存活敌人数量。</summary>
    public int AliveEnemyCount { get; private set; }
    /// <summary>当前存活 Boss 数量。</summary>
    public int AliveBossCount { get; private set; }

    /// <summary>初始化生成区域并订阅击杀事件。</summary>
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

    /// <summary>每帧 Tick（本服务无逐帧逻辑）。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并重置波次数据。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = false;
        activeWaveData = null;
    }

    /// <summary>配置当前波次的敌人生成池。</summary>
    /// <param name="waveData">波次配置。</param>
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

    /// <summary>生成指定配置的特殊敌人。</summary>
    /// <param name="configId">敌人配置 Id。</param>
    /// <param name="statMultiplier">属性倍率。</param>
    /// <param name="waveIndex">波次索引。</param>
    /// <returns>生成成功时为 <c>true</c>。</returns>
    public bool TrySpawnSpecialEnemy(string configId, float statMultiplier, int waveIndex)
    {
        if (string.IsNullOrEmpty(configId))
        {
            return false;
        }

        SpecialEnemySpawnContext.Set(waveIndex, statMultiplier);
        return SpawnEnemyInternal(configId, statMultiplier, waveIndex, markAsElite: false, markAsSpecial: true);
    }

    /// <summary>生成普通或精英敌人。</summary>
    /// <param name="forcedConfigId">强制配置 Id，为空则从波次池随机。</param>
    /// <param name="statMultiplier">属性倍率。</param>
    /// <param name="waveIndex">波次索引。</param>
    /// <param name="waveElapsedSeconds">波次已进行时间（用于权重选择）。</param>
    /// <param name="markAsElite">是否标记为精英。</param>
    /// <returns>生成成功时为 <c>true</c>。</returns>
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

    /// <summary>生成 Boss 敌人。</summary>
    /// <param name="bossConfigId">Boss 配置 Id。</param>
    /// <param name="statMultiplier">属性倍率。</param>
    /// <param name="waveIndex">波次索引。</param>
    /// <returns>生成成功时为 <c>true</c>。</returns>
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

    /// <summary>内部：实例化 Boss 并挂载 <see cref="BossController"/>。</summary>
    /// <returns>生成成功时为 <c>true</c>。</returns>
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
        EnsureBossController(instance).Initialize(bossConfigId, statMultiplier, waveIndex);

        AliveEnemyCount++;
        AliveBossCount++;
        return true;
    }

    /// <summary>从波次特殊敌人池随机生成一只。</summary>
    /// <param name="statMultiplier">属性倍率。</param>
    /// <param name="waveIndex">波次索引。</param>
    /// <returns>生成成功时为 <c>true</c>。</returns>
    public bool TrySpawnBonusSpecial(float statMultiplier, int waveIndex)
    {
        if (!specialSelector.TryPick(out string configId))
        {
            return false;
        }

        float entryMultiplier = spawnSelector.GetEntryStatMultiplier(configId, statMultiplier);
        return TrySpawnSpecialEnemy(configId, entryMultiplier, waveIndex);
    }

    /// <summary>内部：从对象池生成并初始化敌人。</summary>
    /// <returns>生成成功时为 <c>true</c>。</returns>
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

    /// <summary>确保实例上存在 BossController。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <returns>Boss 控制器实例。</returns>
    private static BossController EnsureBossController(GameObject instance)
    {
        if (!instance.TryGetComponent(out BossController controller))
        {
            controller = instance.AddComponent<BossController>();
        }

        return controller;
    }

    /// <summary>确保实例上存在 EliteController。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <returns>精英控制器实例。</returns>
    private static EliteController EnsureEliteController(GameObject instance)
    {
        if (!instance.TryGetComponent(out EliteController controller))
        {
            controller = instance.AddComponent<EliteController>();
        }

        return controller;
    }

    /// <summary>从对象池或 Prefab 实例化敌人。</summary>
    /// <param name="data">敌人配置。</param>
    /// <param name="position">生成位置。</param>
    /// <returns>实例 GameObject，失败时为 <c>null</c>。</returns>
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

    /// <summary>击杀事件回调：维护存活计数。</summary>
    /// <param name="ctx">击杀事件上下文。</param>
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
