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
    private bool isInitialized;
    private bool defeatRaised;

    public string BossConfigId => bossConfigId;
    public bool IsReady => isInitialized;
    public int CurrentPhaseIndex => phaseController.CurrentPhaseIndex;
    public int BonusExperience => bossData != null ? bossData.BonusExperience : 0;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyController = GetComponent<EnemyController>();
        enemyHealth = GetComponent<Enemy_Health>();
        entityStats = GetComponent<Entity_Stats>();
    }

    /// <summary>由 <see cref="EnemySpawnerManager.TrySpawnBoss"/> 在敌人初始化后调用。</summary>
    public void Initialize(string configId, float waveStatMultiplier)
    {
        Shutdown();
        bossConfigId = configId ?? string.Empty;
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

    public void Shutdown()
    {
        isInitialized = false;
        phaseController.Shutdown();
        skillRunner.Shutdown();
        bossData = null;
        bossConfigId = string.Empty;
        defeatRaised = false;
    }

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

    private void OnDisable()
    {
        Shutdown();
    }

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

        EnemyStatScaling.ApplyMultiplier(bossSnapshot, waveStatMultiplier);

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
