using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次推进：驱动 <see cref="EnemySpawnerManager"/> 刷怪，统计存活并在超时、清场或 Boss 击败后发布 <see cref="GameEvents.RaiseWaveCompleted"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 子物体上。</para>
/// <para>波次组合与难度由 <see cref="WaveScheduleSO"/> 段定义驱动，数值曲线由 <see cref="WaveProgressionConfigSO"/> 负责。</para>
/// </remarks>
public class WaveManager : MonoBehaviour, IGameSystem
{
    [Header("Wave Schedule")]
    [SerializeField] private string waveScheduleId = GameConstants.ConfigIds.WaveScheduleDefault;
    [SerializeField] private EnemySpawnerManager spawner;
    [SerializeField] private bool autoStartOnPlaying = true;

    [Header("Legacy Fallback")]
    [Tooltip("未配置 WaveSchedule 时回退使用逐波 WaveData Id 列表。")]
    [SerializeField] private string[] legacyWaveConfigIds = { GameConstants.ConfigIds.Wave01 };

    private ConfigManager configManager;
    private GameManager gameManager;
    private WaveScheduleSO waveSchedule;
    private WaveSpawnProfile currentWaveProfile;
    private int currentWaveIndex = 1;
    private int spawnedThisWave;
    private float waveElapsed;
    private float spawnTimer;
    private bool waveActive;
    private int spawnedBossPlanCount;
    private int expectedBossCount;
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
    /// <summary>配置波次总数（0 表示无限波次）。</summary>
    public int TotalWaveCount => waveSchedule != null ? waveSchedule.DisplayTotalWaves : 0;

    /// <summary>解析依赖、订阅事件并按需自动开始首波。</summary>
    public void Initialize()
    {
        configManager = ServiceLocator.TryGet(out ConfigManager cm) ? cm : null;
        gameManager = ServiceLocator.TryGet(out GameManager gm) ? gm : null;
        ResolveWaveSchedule();

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
        if (!isInitialized || !waveActive || currentWaveProfile == null || spawner == null)
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
        if (!TryResolveWaveProfile(currentWaveIndex, out currentWaveProfile))
        {
            Debug.LogError($"[WaveManager] 无法开始波次 index={currentWaveIndex}");
            return;
        }

        spawnedThisWave = 0;
        waveElapsed = 0f;
        spawnTimer = 0f;
        spawnedBossPlanCount = 0;
        expectedBossCount = currentWaveProfile.HasBoss
            ? Mathf.Max(1, currentWaveProfile.BossSpawnPlans.Count)
            : 0;
        if (currentWaveProfile.BossSpawnPlans.Count == 0 && currentWaveProfile.HasBoss)
        {
            expectedBossCount = 1;
        }

        bossDefeated = expectedBossCount <= 0;
        waveActive = true;
        RunProgressionContext.SetCurrentWave(currentWaveIndex);
        ApplyScheduleProgressionOverride();
        WaveSpawnDifficultyContext.Apply(
            currentWaveProfile.DifficultySnapshot,
            waveSchedule?.ProceduralRules != null
                ? waveSchedule.ProceduralRules.MaxDifficultyTierIndex
                : 12);
        spawner.ConfigureWavePool(currentWaveProfile, GetEffectiveWaveDuration());

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
        if (currentWaveProfile == null)
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
        if (!waveActive || currentWaveProfile == null || spawner == null)
        {
            return false;
        }

        return TrySpawnOne();
    }

    /// <summary>敌人死亡时由事件或外部调用，用于提前结束波次判定。</summary>
    public void OnEnemyDied(EnemyEventArgs args)
    {
        if (!waveActive || currentWaveProfile == null)
        {
            return;
        }

        if (currentWaveProfile.HasBoss && args.EnemyObject != null &&
            args.EnemyObject.TryGetComponent(out Enemy enemy) && enemy.IsBoss &&
            spawnedBossPlanCount >= expectedBossCount &&
            spawner.AliveBossCount <= 0)
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
    private bool ShouldPauseSpawnsForBoss()
    {
        return currentWaveProfile.PauseNormalSpawnsWhileBossAlive &&
               currentWaveProfile.HasBoss &&
               spawnedBossPlanCount > 0 &&
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

    /// <summary>到达时间点时尝试生成 Boss（支持同波多只、独立难度系数）。</summary>
    private void TrySpawnBoss()
    {
        if (!currentWaveProfile.HasBoss || spawnedBossPlanCount >= expectedBossCount)
        {
            return;
        }

        IReadOnlyList<WaveBossSpawnPlan> plans = currentWaveProfile.BossSpawnPlans;
        if (plans != null && plans.Count > 0)
        {
            TrySpawnBossFromPlans(plans);
            return;
        }

        TrySpawnLegacySingleBoss();
    }

    /// <summary>按 BossSpawnPlans 逐只生成。</summary>
    private void TrySpawnBossFromPlans(IReadOnlyList<WaveBossSpawnPlan> plans)
    {
        while (spawnedBossPlanCount < plans.Count)
        {
            WaveBossSpawnPlan plan = plans[spawnedBossPlanCount];
            if (waveElapsed < plan.SpawnAtElapsed)
            {
                break;
            }

            float multiplier = ResolveSpawnExternalMultiplier() * plan.StatMultiplier;
            if (!spawner.TrySpawnBoss(plan.BossConfigId, multiplier, currentWaveIndex))
            {
                break;
            }

            spawnedBossPlanCount++;
            if (!currentWaveProfile.RequireBossDefeatToComplete)
            {
                bossDefeated = spawnedBossPlanCount >= expectedBossCount;
            }
        }
    }

    /// <summary>Legacy 单 Boss 模式。</summary>
    private void TrySpawnLegacySingleBoss()
    {
        if (spawnedBossPlanCount > 0 ||
            string.IsNullOrEmpty(currentWaveProfile.BossConfigId) ||
            waveElapsed < currentWaveProfile.BossSpawnAtElapsed)
        {
            return;
        }

        float multiplier = ResolveSpawnExternalMultiplier();
        if (spawner.TrySpawnBoss(currentWaveProfile.BossConfigId, multiplier, currentWaveIndex))
        {
            spawnedBossPlanCount = 1;
            if (!currentWaveProfile.RequireBossDefeatToComplete)
            {
                bossDefeated = true;
            }
        }
    }

    /// <summary>尝试生成一名普通敌人。</summary>
    private bool TrySpawnOne()
    {
        float multiplier = ResolveSpawnExternalMultiplier();
        return spawner.TrySpawnEnemy(null, multiplier, currentWaveIndex, waveElapsed, markAsElite: false);
    }

    /// <summary>Legacy 线性倍率或 V2 外部倍率（地图 / 难度 / 段修正 / 条目在 Spawner 层叠乘）。</summary>
    private float ResolveSpawnExternalMultiplier()
    {
        float mapAndDifficulty = MapRuntimeContext.EnemyStatMultiplier *
                                 RunDifficultyContext.EnemyStatDifficultyMult *
                                 currentWaveProfile.Difficulty.StatMultiplier;

        if (RunProgressionContext.IsActive)
        {
            return mapAndDifficulty;
        }

        float legacyWaveMult = 1f + (currentWaveIndex - 1) * currentWaveProfile.LegacyStatScalePerWave;
        return legacyWaveMult * mapAndDifficulty;
    }

    /// <summary>解析当前波次的精英怪生成概率（含波次递进曲线与段修正）。</summary>
    private float ResolveEliteSpawnChance()
    {
        float baseChance = currentWaveProfile.EliteSpawnChance +
                           currentWaveProfile.Difficulty.EliteSpawnChanceAdd;

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetEliteSpawnChance(
                currentWaveIndex,
                RunProgressionContext.Config,
                baseChance);
        }

        return Mathf.Clamp01(baseChance);
    }

    /// <summary>解析当前波次的特殊敌人生成概率（含波次递进曲线与段修正）。</summary>
    private float ResolveSpecialSpawnChance()
    {
        float baseChance = currentWaveProfile.SpecialSpawnChance +
                           currentWaveProfile.Difficulty.SpecialSpawnChanceAdd;

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetSpecialSpawnChance(
                currentWaveIndex,
                RunProgressionContext.Config,
                baseChance);
        }

        return Mathf.Clamp01(baseChance);
    }

    /// <summary>获取含地图与段修正的有效刷怪间隔。</summary>
    private float GetEffectiveSpawnInterval()
    {
        float segmentMult = currentWaveProfile.Difficulty.SpawnIntervalMultiplier;

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetSpawnInterval(
                currentWaveIndex,
                RunProgressionContext.Config,
                MapRuntimeContext.SpawnIntervalMultiplier * segmentMult);
        }

        return currentWaveProfile.LegacySpawnInterval *
               MapRuntimeContext.SpawnIntervalMultiplier * segmentMult;
    }

    /// <summary>获取含地图与段修正的有效单波最大刷怪数。</summary>
    private int GetEffectiveMaxSpawnCount()
    {
        float segmentMult = currentWaveProfile.Difficulty.MaxSpawnCountMultiplier;

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetMaxSpawnCount(
                currentWaveIndex,
                RunProgressionContext.Config,
                MapRuntimeContext.MaxSpawnCountMultiplier * segmentMult);
        }

        return Mathf.Max(1, Mathf.RoundToInt(
            currentWaveProfile.LegacyMaxSpawnCount *
            MapRuntimeContext.MaxSpawnCountMultiplier * segmentMult));
    }

    /// <summary>获取有效波次时长（V2 曲线或 Legacy 静态值，叠加段修正）。</summary>
    private float GetEffectiveWaveDuration()
    {
        float segmentMult = currentWaveProfile.Difficulty.WaveDurationMultiplier;

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetWaveDuration(
                currentWaveIndex,
                RunProgressionContext.Config) * segmentMult;
        }

        return currentWaveProfile.LegacyWaveDuration * segmentMult;
    }

    /// <summary>检测是否满足波次完成条件。</summary>
    private void TryCompleteWave()
    {
        if (!waveActive || currentWaveProfile == null)
        {
            return;
        }

        bool allSpawned = spawnedThisWave >= GetEffectiveMaxSpawnCount();
        bool noAlive = spawner.AliveEnemyCount <= 0;
        bool timedOut = waveElapsed >= GetEffectiveWaveDuration();
        bool bossCleared = !currentWaveProfile.HasBoss || !currentWaveProfile.RequireBossDefeatToComplete ||
                           (spawnedBossPlanCount >= expectedBossCount && bossDefeated &&
                            spawner.AliveBossCount <= 0);
        bool bossOnlyComplete = currentWaveProfile.HasBoss && currentWaveProfile.RequireBossDefeatToComplete &&
                                spawnedBossPlanCount >= expectedBossCount && bossDefeated &&
                                spawner.AliveBossCount <= 0;
        bool allEnemiesCleared = allSpawned && noAlive && bossCleared;

        if (bossOnlyComplete || allEnemiesCleared || timedOut)
        {
            CompleteCurrentWave();
        }
    }

    /// <summary>结束当前波次并广播 WaveCompleted，随后直接推进下一波。</summary>
    private void CompleteCurrentWave()
    {
        waveActive = false;
        advanceWaveAfterUpgrade = false;
        GameEvents.RaiseWaveCompleted(this, new WaveEventArgs(
            currentWaveIndex,
            waveElapsed,
            spawnedThisWave));

        StartWave(currentWaveIndex + 1);
    }

    /// <summary>敌人击杀事件回调。</summary>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is EnemyEventArgs args)
        {
            OnEnemyDied(args);
        }
    }

    /// <summary>游戏状态变更时控制波次启停。</summary>
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

    /// <summary>加载波次表资产。</summary>
    private void ResolveWaveSchedule()
    {
        waveSchedule = null;
        if (configManager != null && !string.IsNullOrEmpty(waveScheduleId))
        {
            configManager.TryGetWaveSchedule(waveScheduleId, out waveSchedule);
        }
    }

    /// <summary>若波次表指定了 Progression 覆写，临时注入 RunProgressionContext。</summary>
    private void ApplyScheduleProgressionOverride()
    {
        if (waveSchedule == null || waveSchedule.ProgressionOverride == null)
        {
            return;
        }

        if (RunProgressionContext.UseWaveProgressionV2)
        {
            RunProgressionContext.ApplySettings(true, waveSchedule.ProgressionOverride);
        }
    }

    /// <summary>按波次序号解析运行时刷怪快照。</summary>
    private bool TryResolveWaveProfile(int waveIndex, out WaveSpawnProfile profile)
    {
        return WaveDefinitionResolver.TryResolve(
            waveIndex,
            waveSchedule,
            legacyWaveConfigIds,
            configManager,
            out profile);
    }
}
