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

    /// <summary>取消 Buff 变更订阅。</summary>
    private void OnDestroy()
    {
        GameEvents.UnsubscribeBuffChanged(OnBuffChanged);
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
    public void GrantExperience(int baseAmount, object source = null)
    {
        if (!IsReady || baseAmount <= 0 || ActiveData == null)
        {
            return;
        }

        float gainMult = 1f + runtimeStats.Get(StatType.ExperienceGain);
        float amount = baseAmount * gainMult;
        var data = runtimeStats.Data;
        data.AddExperience(amount);

        while (data.CurrentExperienceValue >= ActiveData.ExperiencePerLevel)
        {
            data.AddExperience(-ActiveData.ExperiencePerLevel);
            int newLevel = data.CurrentLevel + 1;
            data.SetLevel(newLevel);
            GameEvents.RaisePlayerLevelUp(this, newLevel);
        }
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
