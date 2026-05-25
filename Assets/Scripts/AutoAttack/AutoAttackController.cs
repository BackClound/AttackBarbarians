using UnityEngine;

/// <summary>
/// 自动攻击节拍：目标扫描调度、连发休整计时；实际发弹由 <see cref="ShootSkillController"/> 经 <see cref="SkillManager"/> 执行。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景 Player 根物体（与 <see cref="Player"/>、<see cref="PlayerController"/> 同物体）。</para>
/// <para><b>数据流：</b>本组件（扫描/休整）→ <see cref="ShootSkillController"/> → <see cref="ShootProjectileCaster"/> → <see cref="ProjectileManager"/>。</para>
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
    private ShootSkillController shootController;
    private AutoAttackDataSO activeData;

    private float scanTimer;
    private bool hasValidTarget;

    public AutoAttackDataSO ActiveData => activeData;
    public bool IsReady { get; private set; }
    public bool HasValidTarget => hasValidTarget;
    public Enemy PrimaryTarget => playerController != null ? playerController.GetPrimaryTarget() : null;

    /// <summary>可进入射击状态：有目标且 <see cref="ShootSkillController"/> 允许释放。</summary>
    public bool CanAttack =>
        combatEnabled && IsReady && hasValidTarget && shootController != null && shootController.CanShoot();

    /// <summary>供 Animator ShootSpeedMulti 使用。</summary>
    public float AnimSpeedMultiplier => shootController != null
        ? shootController.GetAnimSpeedMultiplier()
        : 1f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        shootController = GetComponent<ShootSkillController>();
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

    public void Tick(float deltaTime)
    {
        if (!IsReady)
        {
            return;
        }

        TickTargetScan(deltaTime);
    }

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

    /// <summary>手动触发一发（一般由 <see cref="SkillShoot"/> 检测驱动，勿依赖动画帧）。</summary>
    public void ExecuteAttack()
    {
        if (!IsReady || !combatEnabled || shootController == null)
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

        shootController.ExecuteShoot();
        RefreshTargetsAfterShot();
    }

    public void RefreshAnimSpeedFromStats() { }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            hasValidTarget = false;
        }
    }

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

    private void RefreshTargetsAfterShot()
    {
        hasValidTarget = RefreshTargets(force: true);
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
