using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家自动攻击核心：目标扫描调度、连发/休整、投射物生成与攻击事件发布。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景 Player 根物体（与 <see cref="Player"/>、<see cref="PlayerController"/> 同物体）。</para>
/// <para><b>Inspector：</b>配置 <c>Fire Origin</c>（发射点 Transform）、可选 <c>Data Override</c>；留空 configId 时从 Resources 加载默认资产。</para>
/// <para><b>数据流：</b><see cref="PlayerController"/> 目标扫描 → 本组件 → <see cref="ProjectileManager"/> → <see cref="DamageSystem"/>。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
[DefaultExecutionOrder(-35)]
public class AutoAttackController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string autoAttackConfigId = GameConstants.ConfigIds.AutoAttackDefault;
    [SerializeField] private AutoAttackDataSO dataOverride;

    [Header("References")]
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private bool combatEnabled = true;

    private Player player;
    private PlayerController playerController;
    private AutoAttackDataSO activeData;

    private readonly List<Enemy> combatTargets = new List<Enemy>(24);

    private float scanTimer;
    private int burstShotsFired;
    private bool burstExhausted;
    private float burstRecoveryTimer;
    private bool hasValidTarget;

    public AutoAttackDataSO ActiveData => activeData;
    public bool IsReady { get; private set; }
    public bool HasValidTarget => hasValidTarget;
    public Enemy PrimaryTarget => playerController != null ? playerController.GetPrimaryTarget() : null;

    /// <summary>可进入射击状态：有目标且未处于连发休整。</summary>
    public bool CanAttack =>
        combatEnabled && IsReady && hasValidTarget && !burstExhausted;

    /// <summary>供 Animator ShootSpeedMulti 使用。</summary>
    public float AnimSpeedMultiplier
    {
        get
        {
            if (playerController != null && playerController.RuntimeStats.IsInitialized)
            {
                return Mathf.Max(0.1f, playerController.RuntimeStats.Get(StatType.AttackSpeedMulti));
            }

            return player != null && player.player_Health != null && player.player_Health.entity_Stats != null
                ? Mathf.Max(0.1f, player.player_Health.entity_Stats.GetAttackSpeedMultiplier())
                : 1f;
        }
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        playerController = GetComponent<PlayerController>();

        if (fireOrigin == null)
        {
            fireOrigin = transform;
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

        Tick(Time.deltaTime);
    }

    /// <summary>从 Config / Override 加载自动攻击配置。</summary>
    public void InitializeFromConfig()
    {
        if (!TryResolveData(out AutoAttackDataSO data))
        {
            Debug.LogError(
                "[AutoAttackController] 未找到 AutoAttackDataSO，请配置 Override 或 Resources 默认资产。",
                this);
            IsReady = false;
            return;
        }

        activeData = data;
        ResetBurstState();
        scanTimer = 0f;
        IsReady = true;
        RefreshTargets(force: true);
    }

    public void Tick(float deltaTime)
    {
        if (!IsReady)
        {
            return;
        }

        TickBurstRecovery(deltaTime);
        TickTargetScan(deltaTime);
    }

    /// <summary>立即扫描射程内敌人（供 Combat / Skill 兼容）。</summary>
    public bool RefreshTargets(bool force = false)
    {
        hasValidTarget = false;
        combatTargets.Clear();

        if (!combatEnabled || playerController == null || !playerController.IsReady)
        {
            return false;
        }

        if (!playerController.ScanCombatTargets())
        {
            return false;
        }

        playerController.CopyCombatTargetsTo(combatTargets);
        PruneInvalidTargets();
        hasValidTarget = combatTargets.Count > 0;
        return hasValidTarget;
    }

    /// <summary>复制当前战斗目标列表到目标列表。</summary>
    public bool CopyCombatTargetsTo(List<Enemy> destination)
    {
        if (destination == null)
        {
            return false;
        }

        if (!hasValidTarget)
        {
            RefreshTargets(force: true);
        }

        destination.Clear();
        for (int i = 0; i < combatTargets.Count; i++)
        {
            destination.Add(combatTargets[i]);
        }

        return destination.Count > 0;
    }

    /// <summary>在动画攻击帧触发：向主目标发射投射物并推进连发计数。</summary>
    public void ExecuteAttack()
    {
        if (!IsReady || !combatEnabled)
        {
            return;
        }

        if (!hasValidTarget)
        {
            RefreshTargets(force: true);
            if (!hasValidTarget)
            {
                return;
            }
        }

        Enemy target = SelectAttackTarget();
        if (target == null)
        {
            hasValidTarget = false;
            return;
        }

        if (!ServiceLocator.TryGet(out ProjectileManager projectileManager))
        {
            Debug.LogWarning("[AutoAttackController] ProjectileManager 未注册，无法发射。");
            return;
        }

        FireAtTarget(target, projectileManager);

        if (playerController != null)
        {
            playerController.NotifyAttackStarted(activeData.SkillId);
            playerController.NotifySkillCast(activeData.SkillId);
        }

        burstShotsFired++;
        if (burstShotsFired >= activeData.ShotsPerBurst)
        {
            BeginBurstRecovery();
        }

        RefreshTargetsAfterShot();
    }

    public void RefreshAnimSpeedFromStats()
    {
        // 动画倍率由 PlayerShootState 订阅；此处保留供 PlayerController 统一刷新入口。
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            hasValidTarget = false;
            combatTargets.Clear();
        }
    }

    /// <summary>连发休整计时。</summary>
    private void TickBurstRecovery(float deltaTime)
    {
        if (!burstExhausted)
        {
            return;
        }

        burstRecoveryTimer -= deltaTime;
        if (burstRecoveryTimer > 0f)
        {
            return;
        }

        ResetBurstState();
    }

    /// <summary>扫描目标。</summary>
    private void TickTargetScan(float deltaTime)
    {
        scanTimer -= deltaTime;
        if (scanTimer > 0f)
        {
            return;
        }

        RefreshTargets(force: true);
        float idleInterval = activeData.ScanIntervalWhileIdle;
        float engagedInterval = activeData.ScanIntervalWhileEngaged;
        if (ServiceLocator.TryGet(out PerformanceManager performance) && performance.ActiveBudget != null)
        {
            idleInterval = performance.ActiveBudget.TargetScanIntervalIdle;
            engagedInterval = performance.ActiveBudget.TargetScanIntervalEngaged;
        }

        scanTimer = hasValidTarget ? engagedInterval : idleInterval;
    }

    /// <summary>开始连发休整。</summary>
    private void BeginBurstRecovery()
    {
        burstExhausted = true;
        burstRecoveryTimer = activeData.BurstRecoverySeconds;
        burstShotsFired = 0;
    }

    /// <summary>重置连发状态。</summary>
    private void ResetBurstState()
    {
        burstShotsFired = 0;
        burstExhausted = false;
        burstRecoveryTimer = 0f;
    }

    /// <summary>刷新目标列表后处理。</summary>
    private void RefreshTargetsAfterShot()
    {
        PruneInvalidTargets();
        if (combatTargets.Count > 0)
        {
            hasValidTarget = true;
            return;
        }

        hasValidTarget = RefreshTargets(force: true);
    }

    /// <summary>移除无法攻击的目标。</summary>
    private void PruneInvalidTargets()
    {
        for (int i = combatTargets.Count - 1; i >= 0; i--)
        {
            Enemy enemy = combatTargets[i];
            if (enemy == null || enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
            {
                combatTargets.RemoveAt(i);
            }
        }
    }

    private Enemy SelectAttackTarget()
    {
        for (int i = 0; i < combatTargets.Count; i++)
        {
            Enemy enemy = combatTargets[i];
            if (enemy != null && enemy.enemy_Health != null && enemy.enemy_Health.CanBeDamage())
            {
                return enemy;
            }
        }

        return PrimaryTarget;
    }

/// <summary>向目标发射投射物。</summary>
    private void FireAtTarget(Enemy enemy, ProjectileManager projectileManager)
    {
        Entity_Stats stats = player.player_Health != null ? player.player_Health.entity_Stats : null;
        float baseDamage = stats != null ? stats.GetBaseAttackDamage() : 0f;
        Vector2 spawnPos = fireOrigin.position;
        Vector2 direction = ((Vector2)enemy.transform.position - spawnPos).normalized;

        ProjectileDataSO projectileData = ResolveProjectileData(projectileManager);
        ProjectileSpawnRequest template = ProjectileSpawnRequest.CreateStraight(
            player,
            enemy.gameObject,
            spawnPos,
            direction,
            baseDamage,
            activeData.SkillId,
            projectileData);

        int count = activeData.ProjectilesPerShot;
        if (count <= 1 && activeData.SpawnPattern == ProjectileSpawnPattern.Single)
        {
            projectileManager.Spawn(template);
            return;
        }

        ProjectileSpawnRequest patternRequest = new ProjectileSpawnRequest(
            template.Source,
            template.Target,
            template.SpawnPosition,
            template.Direction,
            template.DamageInfo,
            template.Data,
            activeData.SpawnPattern,
            count,
            activeData.FanAngleDegrees,
            activeData.SkillId);

        projectileManager.SpawnPattern(patternRequest);
    }

    private ProjectileDataSO ResolveProjectileData(ProjectileManager manager)
    {
        if (activeData != null && activeData.ProjectileData != null)
        {
            return activeData.ProjectileData;
        }

        return manager != null ? manager.DefaultData : null;
    }

    private bool TryResolveData(out AutoAttackDataSO data)
    {
        if (dataOverride != null)
        {
            data = dataOverride;
            return true;
        }

        if (ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.TryGetAutoAttack(autoAttackConfigId, out data))
        {
            return true;
        }

        data = Resources.Load<AutoAttackDataSO>(GameConstants.ResourcePaths.AutoAttackDefault);
        return data != null;
    }
}
