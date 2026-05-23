using UnityEngine;

/// <summary>
/// 波次推进：驱动 <see cref="EnemySpawnerManager"/> 刷怪，统计存活并在超时、清场或 Boss 击败后发布 <see cref="GameEvents.RaiseWaveCompleted"/>。
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
    private float waveElapsed;
    private float spawnTimer;
    private bool waveActive;
    private bool bossSpawned;
    private bool bossDefeated;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public bool IsWaveActive => waveActive;
    public int CurrentWaveIndex => currentWaveIndex;
    public float WaveElapsed => waveElapsed;

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
            return;
        }

        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        waveElapsed += deltaTime;
        spawnTimer += deltaTime;

        TrySpawnBoss();
        TrySpawnByInterval();
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
        bossSpawned = false;
        bossDefeated = !currentWaveData.HasBoss;
        waveActive = true;
        spawner.ConfigureWavePool(currentWaveData);

        GameEvents.RaiseWaveStarted(this, new WaveEventArgs(
            currentWaveIndex,
            currentWaveData.WaveDuration,
            currentWaveData.MaxSpawnCount));
    }

    public void StopWave()
    {
        waveActive = false;
    }

    /// <summary>立即尝试生成一名敌人（供调试或脚本触发）。</summary>
    public bool SpawnNext()
    {
        if (!waveActive || currentWaveData == null || spawner == null)
        {
            return false;
        }

        return TrySpawnOne();
    }

    /// <summary>敌人死亡时由事件或外部调用，用于提前结束波次判定。</summary>
    public void OnEnemyDied(EnemyEventArgs args)
    {
        if (!waveActive || currentWaveData == null)
        {
            return;
        }

        if (currentWaveData.HasBoss && args.EnemyObject != null &&
            args.EnemyObject.TryGetComponent(out Enemy enemy) && enemy.IsBoss)
        {
            bossDefeated = true;
        }

        TryCompleteWave();
    }

    private void TrySpawnByInterval()
    {
        if (spawnedThisWave >= currentWaveData.MaxSpawnCount || spawnTimer < currentWaveData.SpawnInterval)
        {
            return;
        }

        if (ShouldPauseSpawnsForBoss())
        {
            return;
        }

        if (TrySpawnOne())
        {
            spawnedThisWave++;
            spawnTimer = 0f;
            TrySpawnBonusElite();
        }
    }

    private bool ShouldPauseSpawnsForBoss()
    {
        return currentWaveData.PauseNormalSpawnsWhileBossAlive &&
               currentWaveData.HasBoss &&
               bossSpawned &&
               spawner.AliveBossCount > 0;
    }

    private void TrySpawnBonusElite()
    {
        float chance = currentWaveData.EliteSpawnChance;
        if (chance <= 0f || Random.value > chance)
        {
            return;
        }

        float multiplier = currentWaveData.GetStatMultiplierForWave(currentWaveIndex);
        spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex, waveElapsed, markAsElite: true);
    }

    private void TrySpawnBoss()
    {
        if (bossSpawned || !currentWaveData.HasBoss || string.IsNullOrEmpty(currentWaveData.BossConfigId))
        {
            return;
        }

        if (waveElapsed < currentWaveData.BossSpawnAtElapsed)
        {
            return;
        }

        float multiplier = currentWaveData.GetStatMultiplierForWave(currentWaveIndex);
        if (spawner.TrySpawnBoss(currentWaveData.BossConfigId, multiplier, currentWaveIndex))
        {
            bossSpawned = true;
            if (!currentWaveData.RequireBossDefeatToComplete)
            {
                bossDefeated = true;
            }
        }
    }

    private bool TrySpawnOne()
    {
        float multiplier = currentWaveData.GetStatMultiplierForWave(currentWaveIndex);
        return spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex, waveElapsed, markAsElite: false);
    }

    private void TryCompleteWave()
    {
        if (!waveActive || currentWaveData == null)
        {
            return;
        }

        bool allSpawned = spawnedThisWave >= currentWaveData.MaxSpawnCount;
        bool noAlive = spawner.AliveEnemyCount <= 0;
        bool timedOut = waveElapsed >= currentWaveData.WaveDuration;
        bool bossCleared = !currentWaveData.HasBoss || !currentWaveData.RequireBossDefeatToComplete ||
                           (bossSpawned && bossDefeated && spawner.AliveBossCount <= 0);
        bool bossOnlyComplete = currentWaveData.HasBoss && currentWaveData.RequireBossDefeatToComplete &&
                                bossSpawned && bossDefeated && spawner.AliveBossCount <= 0;

        if (bossOnlyComplete || timedOut || (allSpawned && noAlive && bossCleared))
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
        if (ctx.Payload is EnemyEventArgs args)
        {
            OnEnemyDied(args);
        }
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
