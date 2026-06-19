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
    private bool advanceWaveAfterUpgrade;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前波次是否处于激活刷怪状态。</summary>
    public bool IsWaveActive => waveActive;
    /// <summary>当前波次序号（从 1 开始）。</summary>
    public int CurrentWaveIndex => currentWaveIndex;
    /// <summary>当前波次已过时间（秒）。</summary>
    public float WaveElapsed => waveElapsed;
    /// <summary>配置波次总数（用于 HUD 展示）。</summary>
    public int TotalWaveCount => waveConfigIds != null && waveConfigIds.Length > 0 ? waveConfigIds.Length : 20;

    /// <summary>解析依赖、订阅事件并按需自动开始首波。</summary>
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

    /// <summary>驱动刷怪计时、Boss 生成与波次完成判定。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
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

    /// <summary>取消订阅并停止当前波次。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        waveActive = false;
        isInitialized = false;
    }

    /// <summary>
    /// 开始指定序号的波次。
    /// </summary>
    /// <param name="waveIndex">波次序号（从 1 开始）。</param>
    public void StartWave(int waveIndex)
    {
        advanceWaveAfterUpgrade = false;
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
        RunProgressionContext.SetCurrentWave(currentWaveIndex);
        spawner.ConfigureWavePool(currentWaveData, GetEffectiveWaveDuration());

        GameEvents.RaiseWaveStarted(this, new WaveEventArgs(
            currentWaveIndex,
            GetEffectiveWaveDuration(),
            GetEffectiveMaxSpawnCount()));
    }

    /// <summary>停止当前波次刷怪。</summary>
    public void StopWave()
    {
        waveActive = false;
    }

    /// <summary>局内升级后恢复当前波次（不重置进度、不推进波次序号）。</summary>
    private void ResumeCurrentWave()
    {
        if (currentWaveData == null)
        {
            StartWave(Mathf.Max(1, currentWaveIndex));
            return;
        }

        waveActive = true;
        RunProgressionContext.SetCurrentWave(currentWaveIndex);
        TryCompleteWave();
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

    /// <summary>按间隔尝试生成一名普通敌人。</summary>
    private void TrySpawnByInterval()
    {
        if (spawnedThisWave >= GetEffectiveMaxSpawnCount() || spawnTimer < GetEffectiveSpawnInterval())
        {
            return;
        }

        if (ShouldPauseSpawnsForBoss() || MapRuntimeContext.PauseSpawns)
        {
            return;
        }

        if (TrySpawnOne())
        {
            spawnedThisWave++;
            spawnTimer = 0f;
            TrySpawnBonusElite();
            TrySpawnBonusSpecial();
        }
    }

    /// <summary>按概率额外生成特殊敌人。</summary>
    private void TrySpawnBonusSpecial()
    {
        float chance = ResolveSpecialSpawnChance();
        if (chance <= 0f || Random.value > chance)
        {
            return;
        }

        float multiplier = ResolveSpawnExternalMultiplier();
        spawner.TrySpawnBonusSpecial(multiplier, currentWaveIndex);
    }

    /// <summary>Boss 存活且配置要求时是否暂停普通刷怪。</summary>
    /// <returns>应暂停返回 true，否则返回 false。</returns>
    private bool ShouldPauseSpawnsForBoss()
    {
        return currentWaveData.PauseNormalSpawnsWhileBossAlive &&
               currentWaveData.HasBoss &&
               bossSpawned &&
               spawner.AliveBossCount > 0;
    }

    /// <summary>按概率额外生成精英敌人。</summary>
    private void TrySpawnBonusElite()
    {
        float chance = ResolveEliteSpawnChance();
        if (chance <= 0f || Random.value > chance)
        {
            return;
        }

        float multiplier = ResolveSpawnExternalMultiplier();
        spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex, waveElapsed, markAsElite: true);
    }

    /// <summary>到达时间点时尝试生成 Boss。</summary>
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

        float multiplier = ResolveSpawnExternalMultiplier();
        if (spawner.TrySpawnBoss(currentWaveData.BossConfigId, multiplier, currentWaveIndex))
        {
            bossSpawned = true;
            if (!currentWaveData.RequireBossDefeatToComplete)
            {
                bossDefeated = true;
            }
        }
    }

    /// <summary>尝试生成一名普通敌人。</summary>
    /// <returns>生成成功返回 true，否则返回 false。</returns>
    private bool TrySpawnOne()
    {
        float multiplier = ResolveSpawnExternalMultiplier();
        return spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex, waveElapsed, markAsElite: false);
    }

    /// <summary>Legacy 线性倍率或 V2 外部倍率（地图 / 难度 / 条目在 Spawner 层叠乘）。</summary>
    private float ResolveSpawnExternalMultiplier()
    {
        float mapAndDifficulty = MapRuntimeContext.EnemyStatMultiplier *
                                 RunDifficultyContext.EnemyStatDifficultyMult;

        if (RunProgressionContext.IsActive)
        {
            return mapAndDifficulty;
        }

        return currentWaveData.GetLegacyStatMultiplierForWave(currentWaveIndex) * mapAndDifficulty;
    }

    /// <summary>解析当前波次的精英怪生成概率（含波次递进曲线）。</summary>
    /// <returns>0~1 之间的生成概率。</returns>
    private float ResolveEliteSpawnChance()
    {
        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetEliteSpawnChance(
                currentWaveIndex,
                RunProgressionContext.Config,
                currentWaveData.EliteSpawnChance);
        }

        return currentWaveData.EliteSpawnChance;
    }

    /// <summary>解析当前波次的特殊敌人生成概率（含波次递进曲线）。</summary>
    /// <returns>0~1 之间的生成概率。</returns>
    private float ResolveSpecialSpawnChance()
    {
        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetSpecialSpawnChance(
                currentWaveIndex,
                RunProgressionContext.Config,
                currentWaveData.SpecialSpawnChance);
        }

        return currentWaveData.SpecialSpawnChance;
    }

    /// <summary>获取含地图修正的有效刷怪间隔。</summary>
    /// <returns>刷怪间隔（秒）。</returns>
    private float GetEffectiveSpawnInterval()
    {
        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetSpawnInterval(
                currentWaveIndex,
                RunProgressionContext.Config,
                MapRuntimeContext.SpawnIntervalMultiplier);
        }

        return currentWaveData.SpawnInterval * MapRuntimeContext.SpawnIntervalMultiplier;
    }

    /// <summary>获取含地图修正的有效单波最大刷怪数。</summary>
    /// <returns>最大刷怪数量。</returns>
    private int GetEffectiveMaxSpawnCount()
    {
        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetMaxSpawnCount(
                currentWaveIndex,
                RunProgressionContext.Config,
                MapRuntimeContext.MaxSpawnCountMultiplier);
        }

        return Mathf.Max(1, Mathf.RoundToInt(
            currentWaveData.MaxSpawnCount * MapRuntimeContext.MaxSpawnCountMultiplier));
    }

    /// <summary>获取有效波次时长（V2：每 5 波 +5s；Legacy：<see cref="WaveDataSO.WaveDuration"/>）。</summary>
    private float GetEffectiveWaveDuration()
    {
        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetWaveDuration(
                currentWaveIndex,
                RunProgressionContext.Config);
        }

        if (currentWaveData != null)
        {
            return currentWaveData.WaveDuration;
        }

        return GameConstants.Progression.WaveDurationSeconds;
    }

    /// <summary>检测是否满足波次完成条件：全灭或达到波次时长（Boss 波保留击败 Boss 条件）。</summary>
    private void TryCompleteWave()
    {
        if (!waveActive || currentWaveData == null)
        {
            return;
        }

        bool allSpawned = spawnedThisWave >= GetEffectiveMaxSpawnCount();
        bool noAlive = spawner.AliveEnemyCount <= 0;
        bool timedOut = waveElapsed >= GetEffectiveWaveDuration();
        bool bossCleared = !currentWaveData.HasBoss || !currentWaveData.RequireBossDefeatToComplete ||
                           (bossSpawned && bossDefeated && spawner.AliveBossCount <= 0);
        bool bossOnlyComplete = currentWaveData.HasBoss && currentWaveData.RequireBossDefeatToComplete &&
                                bossSpawned && bossDefeated && spawner.AliveBossCount <= 0;
        bool allEnemiesCleared = allSpawned && noAlive && bossCleared;

        if (bossOnlyComplete || allEnemiesCleared || timedOut)
        {
            CompleteCurrentWave();
        }
    }

    /// <summary>结束当前波次并广播 WaveCompleted。</summary>
    private void CompleteCurrentWave()
    {
        waveActive = false;
        advanceWaveAfterUpgrade = true;
        GameEvents.RaiseWaveCompleted(this, new WaveEventArgs(
            currentWaveIndex,
            waveElapsed,
            spawnedThisWave));
    }

    /// <summary>敌人击杀事件回调。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is EnemyEventArgs args)
        {
            OnEnemyDied(args);
        }
    }

    /// <summary>游戏状态变更时控制波次启停。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        if (change.NewState == GameState.Playing && change.OldState == GameState.UpgradeChoosing)
        {
            if (advanceWaveAfterUpgrade)
            {
                advanceWaveAfterUpgrade = false;
                StartWave(currentWaveIndex + 1);
            }
            else
            {
                ResumeCurrentWave();
            }
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

    /// <summary>
    /// 按波次序号解析波次配置。
    /// </summary>
    /// <param name="waveIndex">波次序号。</param>
    /// <param name="data">输出的波次配置。</param>
    /// <returns>解析成功返回 true，否则返回 false。</returns>
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
