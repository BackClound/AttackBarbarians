using UnityEngine;

/// <summary>
/// 波次推进：驱动 <see cref="EnemySpawnerManager"/> 刷怪，统计存活并在超时或清场后发布 <see cref="GameEvents.RaiseWaveCompleted"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 子物体上。</para>
/// </remarks>
public class WaveManager : MonoBehaviour, IGameSystem
{
    [Header("Waves")]
    [SerializeField] private string[] waveConfigIds = { GameConstants.ConfigIds.Wave01 };
    [SerializeField] private EnemySpawnerManager spawner;
    [SerializeField] private bool autoStartOnPlaying = true;

    private ConfigManager configManager;
    private GameManager gameManager;
    private WaveDataSO currentWaveData;
    private int currentWaveIndex = 1;
    private int spawnedThisWave;
    // 波次已过时间 
    private float waveElapsed;
    // 生成计时器
    private float spawnTimer;
    // 波次是否激活
    private bool waveActive;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public int CurrentWaveIndex => currentWaveIndex;

    public void Initialize()
    {
        configManager = ServiceLocator.TryGet(out ConfigManager cm) ? cm : null;
        gameManager = ServiceLocator.TryGet(out GameManager gm) ? gm : null;

        if (spawner == null)
        {
            spawner = GetComponent<EnemySpawnerManager>();
        }

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemySpawnerManager>();
        }

        if (spawner != null && !spawner.IsInitialized)
        {
            spawner.Initialize();
        }

        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;

        if (autoStartOnPlaying && gameManager != null && gameManager.CurrentState == GameState.Playing)
        {
            StartWave(currentWaveIndex);
        }
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized || !waveActive || currentWaveData == null || spawner == null)
        {
            Debug.LogWarning("[WaveManager] 无法推进波次，条件不满足。");
            return;
        }

        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
        {
            Debug.LogWarning("[WaveManager] 游戏未处于 Playing 状态，暂停波次推进。");
            return;
        }

        waveElapsed += deltaTime;
        spawnTimer += deltaTime;

        // 如果生成的敌人数量小于最大数量，并且生成计时器大于生成间隔
        if (spawnedThisWave < currentWaveData.MaxSpawnCount && spawnTimer >= currentWaveData.SpawnInterval)
        {
            Debug.Log($"[WaveManager] 尝试生成敌人 wave={currentWaveIndex} spawned={spawnedThisWave}/{currentWaveData.MaxSpawnCount}");
            float multiplier = currentWaveData.GetStatMultiplierForWave(currentWaveIndex);
            if (spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex))
            {
                spawnedThisWave++;
            }

            spawnTimer = 0f;
        }

        TryCompleteWave();
    }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        waveActive = false;
        isInitialized = false;
    }

    public void StartWave(int waveIndex)
    {
        currentWaveIndex = Mathf.Max(1, waveIndex);
        if (!TryResolveWaveData(currentWaveIndex, out currentWaveData))
        {
            Debug.LogError($"[WaveManager] 无法开始波次 index={currentWaveIndex}");
            return;
        }

        spawnedThisWave = 0;
        waveElapsed = 0f;
        spawnTimer = 0f;
        waveActive = true;
        spawner?.ConfigureWavePool(currentWaveData);

        GameEvents.RaiseWaveStarted(this, new WaveEventArgs(
            currentWaveIndex,
            currentWaveData.WaveDuration,
            currentWaveData.MaxSpawnCount));
    }

    private void TryCompleteWave()
    {
        if (!waveActive || currentWaveData == null)
        {
            return;
        }

        bool allSpawned = spawnedThisWave >= currentWaveData.MaxSpawnCount;
        bool noAlive = spawner != null && spawner.AliveEnemyCount <= 0;
        bool timedOut = waveElapsed >= currentWaveData.WaveDuration;

        if ((allSpawned && noAlive) || timedOut)
        {
            CompleteCurrentWave();
        }
    }

    private void CompleteCurrentWave()
    {
        waveActive = false;
        GameEvents.RaiseWaveCompleted(this, new WaveEventArgs(
            currentWaveIndex,
            waveElapsed,
            spawnedThisWave));
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (!waveActive)
        {
            return;
        }

        TryCompleteWave();
    }

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        if (change.NewState == GameState.Playing && change.OldState == GameState.UpgradeChoosing)
        {
            StartWave(currentWaveIndex + 1);
        }
        else if (change.NewState == GameState.Playing && change.OldState == GameState.Bootstrapping)
        {
            if (!waveActive && autoStartOnPlaying)
            {
                StartWave(1);
            }
        }
        else if (change.NewState != GameState.Playing)
        {
            waveActive = false;
        }
    }

    private bool TryResolveWaveData(int waveIndex, out WaveDataSO data)
    {
        data = null;
        if (configManager == null)
        {
            return false;
        }

        if (waveConfigIds != null && waveConfigIds.Length > 0)
        {
            int listIndex = Mathf.Clamp(waveIndex - 1, 0, waveConfigIds.Length - 1);
            if (configManager.TryGetWave(waveConfigIds[listIndex], out data))
            {
                return true;
            }
        }

        return configManager.TryGetWave(GameConstants.ConfigIds.Wave01, out data);
    }
}
