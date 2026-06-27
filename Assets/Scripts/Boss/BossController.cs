using UnityEngine;

/// <summary>
/// Boss 运行时：叠加 Boss 配置、驱动阶段与技能，并发布 Boss 领域事件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>推荐挂在 Boss Prefab 根节点；缺失时生成 Boss 时会自动添加。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(Enemy_Health))]
public class BossController : MonoBehaviour
{
    private readonly BossPhaseController phaseController = new BossPhaseController();
    private readonly BossSkillRunner skillRunner = new BossSkillRunner();
    private readonly StatRuntimeSnapshot bossSnapshot = new StatRuntimeSnapshot();

    private Enemy enemy;
    private EnemyController enemyController;
    private Enemy_Health enemyHealth;
    private Entity_Stats entityStats;
    private BossDataSO bossData;
    private string bossConfigId = string.Empty;
    private int spawnWaveIndex = 1;
    private bool isInitialized;
    private bool defeatRaised;

    /// <summary>Boss 配置 Id。</summary>
    public string BossConfigId => bossConfigId;
    /// <summary>是否已完成初始化。</summary>
    public bool IsReady => isInitialized;
    /// <summary>当前阶段索引。</summary>
    public int CurrentPhaseIndex => phaseController.CurrentPhaseIndex;
    /// <summary>击败后额外经验奖励。</summary>
    public int BonusExperience => bossData != null ? bossData.BonusExperience : 0;

    /// <summary>缓存同物体上的敌人组件引用。</summary>
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyController = GetComponent<EnemyController>();
        enemyHealth = GetComponent<Enemy_Health>();
        entityStats = GetComponent<Entity_Stats>();
    }

    /// <summary>
    /// 由 <see cref="EnemySpawnerManager.TrySpawnBoss"/> 在敌人初始化后调用。
    /// </summary>
    /// <param name="configId">Boss 配置 Id。</param>
    /// <param name="waveStatMultiplier">外部属性倍率（地图 / 条目）。</param>
    /// <param name="waveIndex">当前波次序号。</param>
    public void Initialize(string configId, float waveStatMultiplier, int waveIndex)
    {
        Shutdown();
        bossConfigId = configId ?? string.Empty;
        spawnWaveIndex = Mathf.Max(1, waveIndex);
        defeatRaised = false;

        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetBoss(bossConfigId, out bossData))
        {
            Debug.LogError($"[BossController] Boss 配置缺失 configId={bossConfigId}", this);
            isInitialized = false;
            return;
        }

        if (enemy != null)
        {
            enemy.SetBossFlag(true);
        }

        ApplyBossStatOverrides(waveStatMultiplier);
        enemyHealth?.ResetForPool();

        phaseController.Initialize(bossData, enemyHealth, entityStats, gameObject);
        skillRunner.Initialize(this, enemyController, bossData, phaseController);
        isInitialized = true;

        GameEvents.RaiseBossSpawned(this, new BossSpawnedEventArgs(
            bossConfigId,
            gameObject,
            bossData.PhaseCount,
            enemyHealth != null ? enemyHealth.MaxHp : 1f));

        GameEvents.RaiseAudioPlayMusic(this, GameConstants.AudioIds.MusicBoss);
        GameEvents.RaiseAudioPlaySfx(this, GameConstants.AudioIds.SfxBossSpawn);
    }

    /// <summary>重置 Boss 运行时状态并关闭子系统。</summary>
    public void Shutdown()
    {
        isInitialized = false;
        phaseController.Shutdown();
        skillRunner.Shutdown();
        bossData = null;
        bossConfigId = string.Empty;
        defeatRaised = false;
    }

    /// <summary>
    /// 通知 Boss 已被击败并广播事件（仅触发一次）。
    /// </summary>
    /// <param name="killer">击杀来源对象。</param>
    public void NotifyDefeated(object killer)
    {
        if (!isInitialized || defeatRaised)
        {
            return;
        }

        defeatRaised = true;
        GameEvents.RaiseBossDefeated(this, new BossDefeatedEventArgs(
            bossConfigId,
            gameObject,
            transform.position,
            killer,
            BonusExperience,
            bossData != null ? bossData.DropTableId : string.Empty,
            phaseController.CurrentPhaseIndex));

        GameEvents.RaiseAudioPlaySfx(this, GameConstants.AudioIds.SfxBossDefeat);
    }

    /// <summary>每帧驱动阶段与技能子系统。</summary>
    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float dt = Time.deltaTime;
        phaseController.Tick(dt);
        skillRunner.Tick(dt);
    }

    /// <summary>禁用时关闭 Boss 运行时。</summary>
    private void OnDisable()
    {
        Shutdown();
    }

    /// <summary>应用 Boss 属性覆盖、波次倍率与精英模式修正。</summary>
    /// <param name="waveStatMultiplier">波次属性倍率。</param>
    private void ApplyBossStatOverrides(float waveStatMultiplier)
    {
        if (entityStats == null || bossData == null)
        {
            return;
        }

        bossSnapshot.CopyFromEntityStats(entityStats);

        if (bossData.UseStatOverrides)
        {
            StatBlockConfig overrides = bossData.StatOverrides;
            if (overrides.MaxHp > 0f)
            {
                bossSnapshot.Set(StatType.MaxHp, overrides.MaxHp);
            }

            if (overrides.Damage > 0f)
            {
                bossSnapshot.Set(StatType.Damage, overrides.Damage);
            }

            if (overrides.MoveSpeed > 0f)
            {
                bossSnapshot.Set(StatType.MoveSpeed, overrides.MoveSpeed);
            }

            if (overrides.AttackSpeed > 0f)
            {
                bossSnapshot.Set(StatType.AttackSpeed, overrides.AttackSpeed);
            }
        }

        float combatMult = waveStatMultiplier;
        float moveMult = WaveSpawnDifficultyContext.MoveSpeedMultiplier;

        if (RunProgressionContext.IsActive)
        {
            EnemyStatScaling.ApplyWaveScaling(
                bossSnapshot,
                spawnWaveIndex,
                combatMult,
                moveMult,
                RunProgressionContext.Config);
        }
        else
        {
            EnemyStatScaling.ApplyCombatMultiplier(bossSnapshot, combatMult);
            EnemyStatScaling.ApplyMoveSpeedMultiplier(bossSnapshot, moveMult);
        }

        if (RunDifficultyContext.IsEliteMode && RunDifficultyContext.EliteConfig != null)
        {
            RunDifficultyContext.EliteConfig.ApplyToSnapshot(bossSnapshot, applyEliteEnemyBonus: false);
        }

        ConfigStatBridge.ApplyToEntityStats(bossSnapshot, entityStats);

        if (enemy != null)
        {
            enemy.moveSpeed = bossSnapshot.Get(StatType.MoveSpeed);
        }
    }
}
