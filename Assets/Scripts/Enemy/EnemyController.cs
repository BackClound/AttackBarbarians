using UnityEngine;

/// <summary>
/// 敌人运行时协调器：加载 <see cref="EnemyDataSO"/>、应用波次缩放、驱动对象池复用与能力组件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人 Prefab 根节点（与 <see cref="Enemy"/>、<see cref="Enemy_Health"/> 同物体）。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Enemy_Health))]
[RequireComponent(typeof(Entity_Stats))]
public class EnemyController : MonoBehaviour, IEntityStateMachineHost
{
    [Header("Config")]
    [SerializeField] private string defaultConfigId = GameConstants.ConfigIds.EnemyBat;
    [SerializeField] private EnemyDataSO dataOverride;

    private Enemy enemy;
    private Enemy_Health enemyHealth;
    private Entity_Stats entityStats;
    private readonly EnemyRuntimeData runtimeData = new EnemyRuntimeData();
    private readonly StatRuntimeSnapshot scaledSnapshot = new StatRuntimeSnapshot();
    private IEnemyAbility[] abilities;
    private CollisionProfile collisionProfile;
    private float waveStatMultiplier = 1f;
    private int spawnWaveIndex = 1;
    private float meleeDamage = 10f;
    private float wallRayDistance = 1.5f;
    private LayerMask wallLayerMask;
    private bool isInitialized;

    /// <summary>所属敌人实体。</summary>
    public Enemy Enemy => enemy;
    /// <summary>运行时数据（配置 Id、经验等）。</summary>
    public EnemyRuntimeData RuntimeData => runtimeData;
    /// <summary>当前敌人配置 Id。</summary>
    public string ConfigId => runtimeData.ConfigId;
    /// <summary>是否已完成池化初始化。</summary>
    public bool IsReady => isInitialized;

    /// <summary>缓存组件与能力引用。</summary>
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyHealth = GetComponent<Enemy_Health>();
        entityStats = GetComponent<Entity_Stats>();
        abilities = GetComponents<IEnemyAbility>();
        collisionProfile = GetComponentInChildren<CollisionProfile>();
    }

    /// <summary>由 <see cref="EnemySpawnerManager"/> 在池取出后调用。</summary>
    /// <param name="configId">敌人配置 Id。</param>
    /// <param name="statMultiplier">波次属性倍率。</param>
    /// <param name="waveIndex">当前波次索引。</param>
    /// <param name="markAsElite">是否标记为精英。</param>
    /// <param name="markAsSpecial">是否标记为特殊敌人。</param>
    public void InitializeForSpawn(
        string configId,
        float statMultiplier,
        int waveIndex,
        bool markAsElite = false,
        bool markAsSpecial = false)
    {
        waveStatMultiplier = Mathf.Max(0.1f, statMultiplier);
        spawnWaveIndex = Mathf.Max(1, waveIndex);
        SpecialEnemySpawnContext.Set(waveIndex, statMultiplier);
        if (enemy != null)
        {
            enemy.SetEliteFlag(markAsElite);
            enemy.SetSpecialFlag(markAsSpecial);
        }
        if (!TryResolveData(configId, out EnemyDataSO data))
        {
            Debug.LogError($"[EnemyController] 未找到 EnemyData configId={configId}", this);
            isInitialized = false;
            return;
        }

        runtimeData.Initialize(data);
        ApplyScaledStats();
        enemyHealth.ResetForPool();
        ApplyCombatFieldsFromConfig(data);
        if (enemy != null && enemy.idleState != null)
        {
            enemy.stateMachine.InitialState(enemy.idleState);
        }

        SetupSpecialEnemyMechanics(data, markAsSpecial);
        NotifyAbilitiesSpawn();
        isInitialized = true;

        GameEvents.RaiseEnemySpawned(this, new EnemyEventArgs(
            gameObject,
            transform.position,
            null,
            runtimeData.ConfigId));
    }

    /// <summary>
    /// 对象池回收回调：重置初始化标记并通知敌人清理。
    /// </summary>
    public void OnPoolDespawn()
    {
        isInitialized = false;
        waveStatMultiplier = 1f;
        spawnWaveIndex = 1;
        enemy?.OnDespawn();
    }

    /// <summary>每帧驱动状态机与特殊能力组件（受性能预算节流）。</summary>
    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance))
        {
            bool highPriority = enemy != null && (enemy.IsBoss || enemy.IsElite || enemy.IsSpecial);
            if (!performance.ShouldRunEnemyUpdateThisFrame(transform, highPriority))
            {
                return;
            }
        }

        TickStateMachine(Time.deltaTime);

        if (abilities == null || abilities.Length == 0)
        {
            return;
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnUpdate(this, dt);
        }
    }

    /// <summary>驱动敌人状态机 Update。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void TickStateMachine(float deltaTime)
    {
        enemy?.stateMachine?.UpdateState();
    }

    /// <summary>驱动敌人状态机 FixedUpdate。</summary>
    /// <param name="fixedDeltaTime">物理帧间隔（秒）。</param>
    public void TickStateMachineFixed(float fixedDeltaTime)
    {
        enemy?.stateMachine?.FixedUpdateState();
    }

    /// <summary>
    /// 检测攻击探测点下方是否命中墙体。
    /// </summary>
    /// <returns>墙体在攻击范围内时为 <c>true</c>。</returns>
    public bool IsWallInAttackRange()
    {
        if (enemy == null)
        {
            return false;
        }

        Vector2 origin = collisionProfile != null ? collisionProfile.ProbePosition : enemy.AttackProbe.position;
        float distance = collisionProfile != null ? collisionProfile.RayDistance : enemy.AttackProbeDistance;
        LayerMask mask = ResolveWallLayers();

        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            return collisionManager.TryDetectWall(origin, distance, mask, out _, out _);
        }

        return CollisionQuery.Raycast(origin, Vector2.down, distance, mask, out _);
    }

    /// <summary>获取对城墙的近战伤害值。</summary>
    /// <returns>配置或缩放后的近战伤害。</returns>
    public float GetMeleeDamage() => meleeDamage;

    /// <summary>动画攻击帧：对墙体射线命中后经 <see cref="DamagePipeline"/> 结算玩家伤害。</summary>
    public void ExecuteWallAttack()
    {
        if (!isInitialized || enemy == null)
        {
            return;
        }

        Vector2 origin = collisionProfile != null ? collisionProfile.ProbePosition : enemy.AttackProbe.position;
        float distance = collisionProfile != null ? collisionProfile.RayDistance : enemy.AttackProbeDistance;
        LayerMask mask = ResolveWallLayers();

        WallControlManager wall = null;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager) &&
            collisionManager.TryDetectWall(origin, distance, mask, out _, out wall))
        {
            wall.TakeDamageFromEnemy(enemy, meleeDamage);
            return;
        }

        if (CollisionQuery.Raycast(origin, Vector2.down, distance, mask, out RaycastHit2D hit) &&
            CollisionQuery.TryResolveWall(hit, out wall))
        {
            wall.TakeDamageFromEnemy(enemy, meleeDamage);
        }
    }

    /// <summary>
    /// 计算击杀该敌人授予玩家的经验值（含 Boss/特殊敌人加成）。
    /// </summary>
    /// <returns>经验奖励，未初始化时为 0。</returns>
    public int GetExperienceReward()
    {
        if (!isInitialized)
        {
            return 0;
        }

        float baseExp = runtimeData.ExperienceReward;
        if (baseExp <= 0f)
        {
            return 0;
        }

        float typeWeight = ResolveExperienceTypeWeight();
        int reward;
        if (RunProgressionContext.IsActive)
        {
            reward = WaveProgressionCalculator.GetKillExperience(
                Mathf.RoundToInt(baseExp),
                spawnWaveIndex,
                typeWeight,
                RunProgressionContext.Config,
                RunDifficultyContext.ExpGainDifficultyMult);
        }
        else
        {
            reward = Mathf.Max(1, Mathf.RoundToInt(baseExp));
        }
        if (enemy != null && enemy.TryGetComponent(out BossController boss) && boss.IsReady)
        {
            reward += boss.BonusExperience;
        }

        if (enemy != null && enemy.TryGetComponent(out SpecialEnemyController special) && special.IsSpecialSpawn)
        {
            reward += special.BonusExperience;
        }

        return reward;
    }

    /// <summary>根据配置挂载特殊敌人能力与控制器。</summary>
    /// <param name="data">敌人配置。</param>
    /// <param name="markAsSpecial">是否标记为特殊敌人。</param>
    private void SetupSpecialEnemyMechanics(EnemyDataSO data, bool markAsSpecial)
    {
        bool hasMechanics = data != null && SpecialEnemyRules.HasMechanics(data.AbilityTags);
        if (!hasMechanics)
        {
            return;
        }

        SpecialEnemyAbilityFactory.EnsureAbilities(this, data);
        abilities = GetComponents<IEnemyAbility>();

        bool shouldMarkSpecial = markAsSpecial || hasMechanics;
        if (!shouldMarkSpecial || enemy == null)
        {
            return;
        }

        enemy.SetSpecialFlag(true);
        EnsureSpecialEnemyController().Initialize(
            runtimeData.ConfigId,
            runtimeData.AbilityTags,
            data.SpecialBonusExperience);
    }

    /// <summary>按敌人类型解析经验权重。</summary>
    private float ResolveExperienceTypeWeight()
    {
        if (!RunProgressionContext.IsActive || RunProgressionContext.Config == null || enemy == null)
        {
            return 1f;
        }

        WaveProgressionConfigSO cfg = RunProgressionContext.Config;
        if (enemy.IsBoss)
        {
            return cfg.NormalEnemyExpWeight;
        }

        if (enemy.IsElite)
        {
            return cfg.EliteEnemyExpWeight;
        }

        if (enemy.IsSpecial)
        {
            return cfg.SpecialEnemyExpWeight;
        }

        return cfg.NormalEnemyExpWeight;
    }

    /// <summary>确保存在 <see cref="SpecialEnemyController"/> 组件。</summary>
    /// <returns>特殊敌人控制器实例。</returns>
    private SpecialEnemyController EnsureSpecialEnemyController()
    {
        if (!TryGetComponent(out SpecialEnemyController controller))
        {
            controller = gameObject.AddComponent<SpecialEnemyController>();
        }

        return controller;
    }

    /// <summary>将波次缩放后的属性快照写入 Entity_Stats。</summary>
    private void ApplyScaledStats()
    {
        scaledSnapshot.CopyFrom(runtimeData.Stats);
        if (RunProgressionContext.IsActive)
        {
            EnemyStatScaling.ApplyWaveScaling(
                scaledSnapshot,
                spawnWaveIndex,
                waveStatMultiplier,
                RunProgressionContext.Config);
        }
        else
        {
            EnemyStatScaling.ApplyMultiplier(scaledSnapshot, waveStatMultiplier);
        }
        ApplyEliteScaling();
        ConfigStatBridge.ApplyToEntityStats(scaledSnapshot, entityStats);
    }

    /// <summary>应用精英模式或精英敌人的额外属性缩放。</summary>
    private void ApplyEliteScaling()
    {
        EliteModeConfigSO eliteConfig = RunDifficultyContext.EliteConfig;
        if (eliteConfig == null)
        {
            return;
        }

        bool eliteEnemy = enemy != null && enemy.IsElite;
        if (RunDifficultyContext.IsEliteMode || eliteEnemy)
        {
            eliteConfig.ApplyToSnapshot(scaledSnapshot, applyEliteEnemyBonus: eliteEnemy);
        }
    }

    /// <summary>从配置同步移速、冷却、近战伤害与探测距离。</summary>
    /// <param name="data">敌人配置。</param>
    private void ApplyCombatFieldsFromConfig(EnemyDataSO data)
    {
        if (enemy == null || data == null)
        {
            return;
        }

        enemy.moveSpeed = scaledSnapshot.Get(StatType.MoveSpeed);
        enemy.cooldownThreshold = data.AttackCooldown;
        if (data.ContactDamage > 0f)
        {
            if (RunProgressionContext.IsActive)
            {
                float damageMult = WaveProgressionCalculator.GetStatMultiplier(
                                         StatType.Damage,
                                         spawnWaveIndex,
                                         RunProgressionContext.Config) *
                                     waveStatMultiplier;
                meleeDamage = data.ContactDamage * damageMult;
            }
            else
            {
                meleeDamage = data.ContactDamage * waveStatMultiplier;
            }
        }
        else
        {
            meleeDamage = scaledSnapshot.Get(StatType.Damage);
        }
        wallRayDistance = data.AttackDistance;
        enemy?.SetAttackProbeDistance(wallRayDistance);
    }

    /// <summary>解析墙体检测层级掩码（优先 Inspector，其次 CollisionManager）。</summary>
    /// <returns>墙体射线检测层级。</returns>
    private LayerMask ResolveWallLayers()
    {
        if (wallLayerMask.value != 0)
        {
            return wallLayerMask;
        }

        if (enemy != null && enemy.WallLayer.value != 0)
        {
            wallLayerMask = enemy.WallLayer;
        }

        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            wallLayerMask = collisionManager.GetEnemyWallLayers(wallLayerMask);
        }

        return wallLayerMask;
    }

    /// <summary>通知所有 <see cref="IEnemyAbility"/> 组件已完成生成。</summary>
    private void NotifyAbilitiesSpawn()
    {
        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnSpawn(this);
        }
    }

    /// <summary>死亡时通知 Boss 控制器与特殊能力组件。</summary>
    public void NotifyDeath()
    {
        if (TryGetComponent(out BossController bossController))
        {
            bossController.NotifyDefeated(null);
        }

        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnDeath(this);
        }
    }

    /// <summary>从 Override 或 ConfigManager 解析敌人配置。</summary>
    /// <param name="configId">配置 Id。</param>
    /// <param name="data">解析到的配置。</param>
    /// <returns>成功找到配置时为 <c>true</c>。</returns>
    private bool TryResolveData(string configId, out EnemyDataSO data)
    {
        if (dataOverride != null)
        {
            data = dataOverride;
            return true;
        }

        string id = string.IsNullOrEmpty(configId) ? defaultConfigId : configId;
        if (ServiceLocator.TryGet(out ConfigManager configManager) && configManager.TryGetEnemy(id, out data))
        {
            return true;
        }

        data = null;
        return false;
    }
}

/// <summary>
/// 敌人属性波次缩放工具（纯逻辑，无挂载）。
/// </summary>
public static class EnemyStatScaling
{
    /// <summary>
    /// V2：按属性曲线应用波次缩放，再乘外部倍率（地图 / 条目）。
    /// </summary>
    public static void ApplyWaveScaling(
        StatRuntimeSnapshot snapshot,
        int waveIndex,
        float externalMultiplier,
        WaveProgressionConfigSO cfg)
    {
        if (snapshot == null || cfg == null)
        {
            return;
        }

        externalMultiplier = Mathf.Max(0.01f, externalMultiplier);
        waveIndex = Mathf.Max(1, waveIndex);

        ApplyMultiplicativeStat(snapshot, StatType.MaxHp, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.Damage, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.FireDamage, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.IceDamage, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.LightningDamage, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.MoveSpeed, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.AttackSpeed, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.AttackSpeedMulti, waveIndex, cfg, externalMultiplier);
        ApplyMultiplicativeStat(snapshot, StatType.Armor, waveIndex, cfg, externalMultiplier);

        ApplyAdditiveStat(snapshot, StatType.CritChance, waveIndex, cfg);
        ApplyAdditiveStat(snapshot, StatType.CritPower, waveIndex, cfg);
    }

    /// <summary>
    /// 将统一波次倍率应用到快照中的核心战斗属性（Legacy）。
    /// </summary>
    /// <param name="snapshot">属性快照。</param>
    /// <param name="multiplier">波次缩放倍率。</param>
    public static void ApplyMultiplier(StatRuntimeSnapshot snapshot, float multiplier)
    {
        if (snapshot == null || Mathf.Approximately(multiplier, 1f))
        {
            return;
        }

        ScaleStat(snapshot, StatType.MaxHp, multiplier);
        ScaleStat(snapshot, StatType.MoveSpeed, multiplier);
        ScaleStat(snapshot, StatType.AttackSpeed, multiplier);
        ScaleStat(snapshot, StatType.AttackSpeedMulti, multiplier);
        ScaleStat(snapshot, StatType.Damage, multiplier);
        ScaleStat(snapshot, StatType.FireDamage, multiplier);
        ScaleStat(snapshot, StatType.IceDamage, multiplier);
        ScaleStat(snapshot, StatType.LightningDamage, multiplier);
        ScaleStat(snapshot, StatType.Armor, multiplier);
    }

    private static void ApplyMultiplicativeStat(
        StatRuntimeSnapshot snapshot,
        StatType statType,
        int waveIndex,
        WaveProgressionConfigSO cfg,
        float externalMultiplier)
    {
        float mult = WaveProgressionCalculator.GetStatMultiplier(statType, waveIndex, cfg) * externalMultiplier;
        if (Mathf.Approximately(mult, 1f))
        {
            return;
        }

        ScaleStat(snapshot, statType, mult);
    }

    private static void ApplyAdditiveStat(
        StatRuntimeSnapshot snapshot,
        StatType statType,
        int waveIndex,
        WaveProgressionConfigSO cfg)
    {
        float bonus = WaveProgressionCalculator.GetStatAdditiveBonus(statType, waveIndex, cfg);
        if (Mathf.Approximately(bonus, 0f))
        {
            return;
        }

        snapshot.Set(statType, snapshot.Get(statType) + bonus);
    }

    /// <summary>缩放快照中的单个属性。</summary>
    /// <param name="snapshot">属性快照。</param>
    /// <param name="statType">属性类型。</param>
    /// <param name="multiplier">缩放倍率。</param>
    private static void ScaleStat(StatRuntimeSnapshot snapshot, StatType statType, float multiplier)
    {
        snapshot.Set(statType, snapshot.Get(statType) * multiplier);
    }
}
