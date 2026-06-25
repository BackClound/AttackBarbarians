using UnityEngine;

/// <summary>
/// 玩家运行时协调器：加载配置、维护 <see cref="PlayerRuntimeStats"/>、统一目标扫描，并驱动现有状态机。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景 Player 根物体上（与 <see cref="Player"/> 同物体）。</para>
/// <para><b>不要挂载到：</b>GameSystems、子弹或敌人 Prefab。</para>
/// <para><b>Inspector：</b>配置扫描原点、敌人 Layer/Tag；<c>Player Data Override</c> 可留空，则从 <see cref="ConfigManager"/> 按 configId 加载。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
[DefaultExecutionOrder(-40)]
public class PlayerController : MonoBehaviour, IEntityStateMachineHost
{
    [Header("Config")]
    [SerializeField] private string playerConfigId = GameConstants.ConfigIds.PlayerDefault;
    [SerializeField] private PlayerDataSO playerDataOverride;

    [Header("Target Scan")]
    [SerializeField] private Transform scanOrigin;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private string enemyTag = GameConstants.Tags.Enemy;
    [SerializeField] private Transform wallReference;

    private Player player;
    private Player_Health playerHealth;
    private Entity_Stats entityStats;
    private AutoAttackController autoAttack;
    private readonly PlayerRuntimeStats runtimeStats = new PlayerRuntimeStats();
    private readonly PlayerTargetScanner targetScanner = new PlayerTargetScanner();

    // 升级三选一弹窗进行中标记：弹窗期间经验条显示满格，确认（回到 Playing）后清除并归零经验。
    private bool levelUpPending;

    /// <summary>所属玩家实体。</summary>
    public Player Player => player;
    /// <summary>自动攻击控制器。</summary>
    public AutoAttackController AutoAttack => autoAttack;
    /// <summary>运行时属性快照。</summary>
    public PlayerRuntimeStats RuntimeStats => runtimeStats;
    /// <summary>战斗目标扫描器。</summary>
    public PlayerTargetScanner TargetScanner => targetScanner;
    /// <summary>当前生效的玩家配置。</summary>
    public PlayerDataSO ActiveData { get; private set; }
    /// <summary>配置与属性是否已完成初始化。</summary>
    public bool IsReady { get; private set; }
    /// <summary>是否正处于升级三选一弹窗中（供经验条显示满格）。</summary>
    public bool IsLevelUpPending => levelUpPending;

    /// <summary>缓存组件引用并设置扫描原点。</summary>
    private void Awake()
    {
        player = GetComponent<Player>();
        playerHealth = GetComponent<Player_Health>();
        autoAttack = GetComponent<AutoAttackController>();
        if (playerHealth != null)
        {
            entityStats = playerHealth.entity_Stats;
        }

        if (scanOrigin == null)
        {
            scanOrigin = transform;
        }
    }

    /// <summary>启动时从配置初始化玩家数据。</summary>
    private void Start()
    {
        InitializeFromConfig();
    }

    /// <summary>每帧驱动状态机并 Tick Buff。</summary>
    private void Update()
    {
        TickStateMachine(Time.deltaTime);

        if (!IsReady)
        {
            return;
        }

        runtimeStats.TickBuffs(Time.deltaTime);
    }

    /// <summary>驱动玩家状态机 Update。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void TickStateMachine(float deltaTime)
    {
        if (player == null || player.stateMachine == null)
        {
            return;
        }

        player.stateMachine.UpdateState();
    }

    /// <summary>驱动玩家状态机 FixedUpdate。</summary>
    /// <param name="fixedDeltaTime">物理帧间隔（秒）。</param>
    public void TickStateMachineFixed(float fixedDeltaTime)
    {
        player?.stateMachine?.FixedUpdateState();
    }

    /// <summary>取消 Buff 与状态变更订阅。</summary>
    private void OnDestroy()
    {
        GameEvents.UnsubscribeBuffChanged(OnBuffChanged);
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
    }

    /// <summary>从 Config / Save 初始化属性并同步到 Entity_Stats。</summary>
    public void InitializeFromConfig()
    {
        if (!TryResolvePlayerData(out PlayerDataSO data))
        {
            Debug.LogError("[PlayerController] 未找到 PlayerDataSO，请配置 Override 或 ConfigDatabase。", this);
            IsReady = false;
            return;
        }

        ActiveData = data;
        SaveData save = null;
        if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current != null)
        {
            save = saveManager.Current;
        }

        runtimeStats.Initialize(data, save);
        if (ServiceLocator.TryGet(out TalentManager talentManager))
        {
            talentManager.ApplyToPlayer(this);
        }

        if (ServiceLocator.TryGet(out EquipmentManager equipmentManager))
        {
            equipmentManager.ApplyToPlayer(this);
        }

        RefreshEntityStats();
        ConfigureTargetScanner();
        IsReady = true;
        playerHealth?.InitializeHpFromStats();
        autoAttack?.InitializeFromConfig();

        GameEvents.UnsubscribeBuffChanged(OnBuffChanged);
        GameEvents.SubscribeBuffChanged(OnBuffChanged);
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        levelUpPending = false;
    }

    /// <summary>扫描射程内敌人并写入 scanner 结果列表。</summary>
    public bool ScanCombatTargets()
    {
        if (!IsReady)
        {
            return false;
        }

        ConfigureTargetScanner();
        return targetScanner.Scan() > 0;
    }

    /// <summary>将 scanner 结果复制到外部列表（供 SkillShoot 等消费）。</summary>
    public bool CopyCombatTargetsTo(System.Collections.Generic.List<Enemy> destination)
    {
        if (destination == null)
        {
            return false;
        }

        if (!ScanCombatTargets())
        {
            destination.Clear();
            return false;
        }

        targetScanner.CopyResultsTo(destination);
        return destination.Count > 0;
    }

    /// <summary>获取当前主目标敌人。</summary>
    /// <returns>扫描器选中的首要目标，无目标时为 <c>null</c>。</returns>
    public Enemy GetPrimaryTarget() => targetScanner.PrimaryTarget;

    /// <summary>发布玩家开始攻击事件。</summary>
    /// <param name="skillId">触发攻击的技能 Id，可为空。</param>
    public void NotifyAttackStarted(string skillId = null)
    {
        GameEvents.RaisePlayerAttackStarted(this, new PlayerAttackEventArgs(targetScanner.PrimaryTarget, skillId));
    }

    /// <summary>发布技能施放与使用事件。</summary>
    /// <param name="skillId">技能配置 Id。</param>
    public void NotifySkillCast(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return;
        }

        GameEvents.RaisePlayerSkillCast(this, skillId);
        GameEvents.RaiseSkillUsed(this, skillId);
    }

    /// <summary>属性变更后刷新 Entity_Stats 与技能攻速等。</summary>
    public void RefreshEntityStats()
    {
        if (entityStats == null)
        {
            return;
        }

        runtimeStats.ApplyToEntityStats(entityStats, this);
        playerHealth?.OnMaxHpStatsChanged();
        player.skillManager?.sKillShoot?.RefreshAttackSpeedFromStats();
        autoAttack?.RefreshAnimSpeedFromStats();
        ConfigureTargetScanner();
    }

    /// <summary>应用 Buff 并刷新实体属性。</summary>
    /// <param name="buff">Buff 配置。</param>
    /// <param name="stacks">叠加层数。</param>
    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        runtimeStats.ApplyBuff(buff, stacks);
        RefreshEntityStats();
    }

    /// <summary>应用单条属性修正并刷新实体属性。</summary>
    /// <param name="modifier">属性修正配置。</param>
    public void ApplyModifier(StatModifierConfig modifier)
    {
        runtimeStats.ApplyModifier(modifier);
        RefreshEntityStats();
    }

    /// <summary>击杀敌人获得经验；受 ExperienceGain 属性加成。</summary>
    /// <remarks>
    /// 经验填满当前等级即升一级并弹出一次 Buff 三选一；单次击杀最多升一级，溢出经验直接丢弃（不结转），
    /// 从而保证“经验条满 → 升级 → 弹窗 → 归零”一一对应、选 buff 不改变经验。
    /// </remarks>
    public void GrantExperience(int baseAmount, object source = null)
    {
        if (!IsReady || baseAmount <= 0 || ActiveData == null)
        {
            return;
        }

        // 升级弹窗未确认前不再累计经验，避免溢出与重复升级。
        if (levelUpPending)
        {
            return;
        }

        float gainMult = 1f + runtimeStats.Get(StatType.ExperienceGain);
        float amount = baseAmount * gainMult;
        runtimeStats.Data.AddExperience(amount);

        TryTriggerLevelUp();
    }

    /// <summary>
    /// 经验达到当前等级所需值时升一级：丢弃溢出经验、标记弹窗进行中并发布升级事件（弹出一次三选一）。
    /// </summary>
    /// <remarks>
    /// <para>仅在 <see cref="GameState.Playing"/> 下触发；升级三选一打开后游戏切到 <see cref="GameState.UpgradeChoosing"/>，
    /// 期间经验条由 <see cref="IsLevelUpPending"/> 显示为满格，玩家确认回到 Playing 后清除标记并将经验归零（见 <see cref="OnGameStateChanged"/>）。</para>
    /// <para>need 强制 >= 1，避免 Legacy 配置 ExperiencePerLevel&lt;=0 时判断异常。</para>
    /// </remarks>
    private void TryTriggerLevelUp()
    {
        if (!IsReady || ActiveData == null || levelUpPending)
        {
            return;
        }

        // 必须确实处于战斗中才升级；若拿不到 GameManager（弹窗也无法打开）则不置标记，避免永久卡住经验。
        if (!ServiceLocator.TryGet(out GameManager gameManager) ||
            gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        var data = runtimeStats.Data;
        float need = Mathf.Max(1f, GetNeedExperienceForCurrentLevel());
        if (data.CurrentExperienceValue < need)
        {
            return;
        }

        levelUpPending = true;
        int newLevel = data.CurrentLevel + 1;
        data.SetLevel(newLevel);
        GameEvents.RaisePlayerLevelUp(this, newLevel);
    }

    /// <summary>状态回到 Playing 时（升级三选一确认完成）：清除弹窗标记并将经验归零，开始累计下一级。</summary>
    /// <param name="ctx">状态变更事件上下文。</param>
    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change || change.NewState != GameState.Playing)
        {
            return;
        }

        if (!levelUpPending)
        {
            return;
        }

        levelUpPending = false;
        if (IsReady)
        {
            ResetCurrentExperience();
        }
    }

    /// <summary>将当前经验归零（升级结算后调用）。</summary>
    private void ResetCurrentExperience()
    {
        PlayerRuntimeData data = runtimeStats.Data;
        if (data != null && data.CurrentExperienceValue > 0f)
        {
            data.AddExperience(-data.CurrentExperienceValue);
        }
    }

    /// <summary>当前等级升级所需经验（V2 曲线或 Legacy 固定值）。</summary>
    public float GetNeedExperienceForCurrentLevel()
    {
        if (!IsReady || ActiveData == null)
        {
            return 100f;
        }

        if (RunProgressionContext.IsActive)
        {
            return WaveProgressionCalculator.GetNeedExperience(
                runtimeStats.Data.CurrentLevel,
                RunProgressionContext.CurrentWave,
                RunProgressionContext.Config,
                RunDifficultyContext.ExpNeedDifficultyMult);
        }

        return ActiveData.ExperiencePerLevel;
    }

    /// <summary>配置目标扫描器参数（射程、Layer、策略等）。</summary>
    private void ConfigureTargetScanner()
    {
        Vector2 wallFallback = wallReference != null
            ? wallReference.position
            : new Vector2(transform.position.x, transform.position.y);

        if (wallReference == null && WallControlManager.HasInstance)
        {
            wallReference = WallControlManager.Instance.transform;
            wallFallback = wallReference.position;
        }

        float radius = runtimeStats.IsInitialized
            ? runtimeStats.GetAttackRadius()
            : ActiveData != null ? ActiveData.AttackRadius : 25f;

        PlayerTargetPolicy policy = ActiveData != null
            ? ActiveData.TargetPolicy
            : PlayerTargetPolicy.NearestToWall;

        LayerMask scanLayers = enemyLayer;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            scanLayers = collisionManager.GetPlayerEnemyScanLayers(enemyLayer);
        }

        targetScanner.Configure(
            scanOrigin,
            radius,
            scanLayers,
            enemyTag,
            policy,
            wallReference,
            wallFallback);
    }

    /// <summary>判断是否可进入战斗/射击状态。</summary>
    /// <returns>技能或自动攻击允许释放时为 <c>true</c>。</returns>
    public bool CanEnterCombatState()
    {
        if (player?.skillManager != null)
        {
            return player.skillManager.CanShoot();
        }

        AutoAttackController autoAttack = AutoAttack;
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.CanAttack;
        }

        return false;
    }

    /// <summary>从 Override 或 ConfigManager 解析玩家配置。</summary>
    /// <param name="data">解析到的配置。</param>
    /// <returns>成功找到配置时为 <c>true</c>。</returns>
    private bool TryResolvePlayerData(out PlayerDataSO data)
    {
        if (playerDataOverride != null)
        {
            data = playerDataOverride;
            return true;
        }

        if (ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.TryGetPlayer(playerConfigId, out data))
        {
            return true;
        }

        data = null;
        return false;
    }

    /// <summary>Buff 变更回调：刷新实体属性。</summary>
    /// <param name="context">事件上下文。</param>
    private void OnBuffChanged(GameEventContext context)
    {
        if (context.Payload is not BuffEventArgs args || args.Target != player)
        {
            return;
        }

        RefreshEntityStats();
    }
}
