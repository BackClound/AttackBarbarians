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
public class PlayerController : MonoBehaviour
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

    public Player Player => player;
    public AutoAttackController AutoAttack => autoAttack;
    public PlayerRuntimeStats RuntimeStats => runtimeStats;
    public PlayerTargetScanner TargetScanner => targetScanner;
    public PlayerDataSO ActiveData { get; private set; }
    public bool IsReady { get; private set; }

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

    private void Start()
    {
        InitializeFromConfig();
    }

    private void Update()
    {
        if (!IsReady)
        {
            return;
        }

        runtimeStats.TickBuffs(Time.deltaTime);
    }

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

    /// <summary>将 scanner 结果复制到外部列表（兼容 PlayerCombat / SkillShoot）。</summary>
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

    public Enemy GetPrimaryTarget() => targetScanner.PrimaryTarget;

    public void NotifyAttackStarted(string skillId = null)
    {
        GameEvents.RaisePlayerAttackStarted(this, new PlayerAttackEventArgs(targetScanner.PrimaryTarget, skillId));
    }

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

    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        runtimeStats.ApplyBuff(buff, stacks);
        RefreshEntityStats();
    }

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

        targetScanner.Configure(
            scanOrigin,
            radius,
            enemyLayer,
            enemyTag,
            policy,
            wallReference,
            wallFallback);
    }

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

    private void OnBuffChanged(GameEventContext context)
    {
        if (context.Payload is not BuffEventArgs args || args.Target != player)
        {
            return;
        }

        RefreshEntityStats();
    }
}
