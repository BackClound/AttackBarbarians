using UnityEngine;

/// <summary>
/// 自动攻击节拍：目标扫描调度；实际发弹由 <see cref="SkillManager"/> 统一自动施法执行。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景 Player 根物体（与 <see cref="Player"/>、<see cref="PlayerController"/> 同物体）。</para>
/// <para><b>数据流：</b>本组件（扫描）→ <see cref="SkillManager"/> → <see cref="ShootSkillEffect"/> → <see cref="ProjectileManager"/>。</para>
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
    [SerializeField] private bool combatEnabled = true;

    private PlayerController playerController;
    private SkillManager skillManager;
    private ShootSkillController shootController;
    private AutoAttackDataSO activeData;

    private float scanTimer;
    private bool hasValidTarget;

    /// <summary>当前生效的自动攻击配置。</summary>
    public AutoAttackDataSO ActiveData => activeData;
    /// <summary>是否已完成配置初始化。</summary>
    public bool IsReady { get; private set; }
    /// <summary>是否存在有效战斗目标。</summary>
    public bool HasValidTarget => hasValidTarget;
    /// <summary>当前主目标敌人。</summary>
    public Enemy PrimaryTarget => playerController != null ? playerController.GetPrimaryTarget() : null;

    /// <summary>可进入射击状态：有目标且 <see cref="ShootSkillController"/> 允许释放。</summary>
    public bool CanAttack =>
        combatEnabled && IsReady && hasValidTarget && shootController != null && shootController.CanShoot();

    /// <summary>供 Animator ShootSpeedMulti 使用。</summary>
    public float AnimSpeedMultiplier => shootController != null
        ? shootController.GetAnimSpeedMultiplier()
        : 1f;

    /// <summary>缓存玩家控制器与技能组件引用。</summary>
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        skillManager = GetComponent<SkillManager>();
        shootController = GetComponent<ShootSkillController>();
    }

    /// <summary>启动时从配置初始化。</summary>
    private void Start()
    {
        InitializeFromConfig();
    }

    /// <summary>每帧驱动目标扫描节拍。</summary>
    private void Update()
    {
        if (!IsReady)
        {
            return;
        }

        Tick(Time.deltaTime);
    }

    /// <summary>从 Config / Resources 加载自动攻击配置。</summary>
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
        scanTimer = 0f;
        IsReady = true;
        RefreshTargets(force: true);
    }

    /// <summary>外部 Tick 入口（目标扫描节拍）。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!IsReady)
        {
            return;
        }

        TickTargetScan(deltaTime);
    }

    /// <summary>扫描射程内敌人并更新 <see cref="HasValidTarget"/>。</summary>
    /// <param name="force">是否忽略扫描间隔强制刷新。</param>
    /// <returns>存在有效目标时为 <c>true</c>。</returns>
    public bool RefreshTargets(bool force = false)
    {
        hasValidTarget = false;

        if (!combatEnabled || playerController == null || !playerController.IsReady)
        {
            return false;
        }

        if (!playerController.ScanCombatTargets())
        {
            return false;
        }

        hasValidTarget = playerController.GetPrimaryTarget() != null;
        return hasValidTarget;
    }

    /// <summary>手动触发一发（走 <see cref="SkillManager"/> 统一施法管线）。</summary>
    public void ExecuteAttack()
    {
        if (!IsReady || !combatEnabled || skillManager == null)
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

        if (skillManager.TryCastSkill(SkillType.Shoot))
        {
            RefreshTargetsAfterShot();
        }
    }

    /// <summary>属性变更后刷新动画攻速（当前由 ShootSkillController 负责）。</summary>
    public void RefreshAnimSpeedFromStats() { }

    /// <summary>启用或禁用自动战斗扫描。</summary>
    /// <param name="enabled">是否启用。</param>
    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            hasValidTarget = false;
        }
    }

    /// <summary>按配置间隔 Tick 目标扫描。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
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

    /// <summary>射击后刷新目标列表。</summary>
    private void RefreshTargetsAfterShot()
    {
        hasValidTarget = RefreshTargets(force: true);
    }

    /// <summary>从 Override、ConfigManager 或 Resources 解析配置。</summary>
    /// <param name="data">解析到的配置。</param>
    /// <returns>成功找到配置时为 <c>true</c>。</returns>
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
