using System;
using UnityEngine;

/// <summary>
/// 射击技能表现层：目标扫描与射击动画；实际发弹由 <see cref="SkillManager"/> 自动施法驱动。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（历史 Prefab 子物体）。</para>
/// <para><b>数据流：</b>扫描目标 → <see cref="SkillManager"/> → <see cref="ShootSkillEffect"/> → <see cref="ShootProjectileCaster"/>。</para>
/// </remarks>
public class SkillShoot : SkillBase
{
    /// <summary>攻速倍率变更时通知外部（如 UI/动画）。</summary>
    public Action<float> updateAttackSpeedMultiAction;

    [Header("Combat")]
    [Tooltip("无目标时的扫描间隔（秒）。")]
    [SerializeField] private float targetScanIntervalIdle = 0.12f;
    [Tooltip("持有目标时的扫描间隔（秒）。")]
    [SerializeField] private float targetScanIntervalEngaged = 0.08f;
    [SerializeField] private bool driveShootAnimator = true;

    [Header("Prefab References (Inspector / migration)")]
    [SerializeField] private GameObject bulletSpawnPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform checkPosition;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private string enemyTag;

    private Player player;
    private PlayerController playerController;
    private AutoAttackController autoAttack;
    private ShootSkillController shootController;

    private float scanTimer;

    /// <summary>射击动画攻速倍率（同步自玩家属性）。</summary>
    public float shootSpeedAnimMulti { get; private set; }

    /// <summary>解析玩家、控制器与射击兼容组件引用。</summary>
    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<Player>();
        playerController = player != null ? player.controller : null;
        autoAttack = player != null ? player.GetComponent<AutoAttackController>() : null;
        shootController = player != null ? player.GetComponent<ShootSkillController>() : null;
    }

    /// <summary>订阅技能施放事件以驱动射击动画。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribePlayerSkillCast(OnPlayerSkillCast);
    }

    /// <summary>取消技能施放事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribePlayerSkillCast(OnPlayerSkillCast);
    }

    /// <summary>初始化攻速倍率与目标扫描计时器。</summary>
    private void Start()
    {
        RefreshAttackSpeedFromStats();
        scanTimer = 0f;
    }

    /// <summary>每帧按间隔刷新战斗目标列表（供自动攻击/施法使用）。</summary>
    protected override void Update()
    {
        if (player == null || playerController == null || !playerController.IsReady)
        {
            return;
        }

        TickTargetScan(Time.deltaTime);
    }

    /// <summary>
    /// 从玩家运行时属性刷新射击动画攻速倍率并同步 Animator。
    /// </summary>
    public void RefreshAttackSpeedFromStats()
    {
        if (shootController != null)
        {
            shootSpeedAnimMulti = shootController.GetAnimSpeedMultiplier();
        }
        else if (playerController != null && playerController.RuntimeStats.IsInitialized)
        {
            shootSpeedAnimMulti = playerController.RuntimeStats.Get(StatType.AttackSpeedMulti);
        }
        else if (player != null && player.player_Health != null && player.player_Health.entity_Stats != null)
        {
            shootSpeedAnimMulti = player.player_Health.entity_Stats.GetAttackSpeedMultiplier();
        }
        else
        {
            shootSpeedAnimMulti = 1f;
        }

        updateAttackSpeedMultiAction?.Invoke(shootSpeedAnimMulti);
        ApplyAnimatorSpeedMultiplier();
    }

    /// <summary>
    /// 射击是否可释放（委托 <see cref="ShootSkillController.CanShoot"/>）。
    /// </summary>
    /// <returns>冷却就绪且有目标时返回 true。</returns>
    public bool CanUseShootSkill() => shootController != null && shootController.CanShoot();

    /// <summary>兼容旧调用：手动触发一次射击。</summary>
    public void ActivateOneShootAttack()
    {
        shootController?.ExecuteShoot();
        RefreshAttackSpeedFromStats();
    }

    /// <summary>兼容旧调用：执行目标扫描。</summary>
    public void CheckEnemyInRadiusWithSorted()
    {
        RefreshCombatTargets(force: true);
    }

    /// <summary>兼容旧调用：等价于 <see cref="CanUseShootSkill"/>。</summary>
    public void CheckEnemyIsAvailable() { }

    /// <summary>响应技能施放事件，触发射击 Animator 并刷新攻速。</summary>
    /// <param name="ctx">技能施放事件上下文。</param>
    private void OnPlayerSkillCast(GameEventContext ctx)
    {
        if (!driveShootAnimator || player?.AnimationDriver == null)
        {
            return;
        }

        if (ctx.Payload is not string skillId || skillId != GameConstants.ConfigIds.SkillShoot)
        {
            return;
        }

        RefreshAttackSpeedFromStats();
        ApplyAnimatorSpeedMultiplier();
        player.AnimationDriver.PlayPulse(EntityAnimParams.PlayerShoot);
    }

    /// <summary>按配置间隔执行目标扫描。</summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    private void TickTargetScan(float deltaTime)
    {
        scanTimer -= deltaTime;
        if (scanTimer > 0f)
        {
            return;
        }

        RefreshCombatTargets(force: true);
        scanTimer = HasCombatTarget() ? targetScanIntervalEngaged : targetScanIntervalIdle;
    }

    /// <summary>刷新战斗目标列表（优先 AutoAttackController）。</summary>
    /// <param name="force">是否强制刷新。</param>
    private void RefreshCombatTargets(bool force)
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            autoAttack.RefreshTargets(force);
            return;
        }

        playerController?.ScanCombatTargets();
    }

    /// <summary>当前是否存在有效战斗目标。</summary>
    /// <returns>有主目标时返回 true。</returns>
    private bool HasCombatTarget()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.HasValidTarget;
        }

        return playerController != null && playerController.GetPrimaryTarget() != null;
    }

    /// <summary>将攻速倍率写入 Animator 的 ShootSpeedMulti 参数。</summary>
    private void ApplyAnimatorSpeedMultiplier()
    {
        if (player?.AnimationDriver == null)
        {
            return;
        }

        player.AnimationDriver.SetFloat(
            EntityAnimParams.PlayerShootSpeedMulti,
            Mathf.Max(0.1f, shootSpeedAnimMulti));
    }

    /// <summary>Inspector 调试占位（Legacy 子弹列表刷新）。</summary>
    [ContextMenu("Update Bullet List")]
    private void UpdateTheBulletList() { }
}
