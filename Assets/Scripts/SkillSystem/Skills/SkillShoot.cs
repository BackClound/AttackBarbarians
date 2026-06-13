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

    public float shootSpeedAnimMulti { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<Player>();
        playerController = player != null ? player.controller : null;
        autoAttack = player != null ? player.GetComponent<AutoAttackController>() : null;
        shootController = player != null ? player.GetComponent<ShootSkillController>() : null;
    }

    private void OnEnable()
    {
        GameEvents.SubscribePlayerSkillCast(OnPlayerSkillCast);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribePlayerSkillCast(OnPlayerSkillCast);
    }

    private void Start()
    {
        RefreshAttackSpeedFromStats();
        scanTimer = 0f;
    }

    protected override void Update()
    {
        if (player == null || playerController == null || !playerController.IsReady)
        {
            return;
        }

        TickTargetScan(Time.deltaTime);
    }

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

    private void OnPlayerSkillCast(GameEventContext ctx)
    {
        if (!driveShootAnimator || player?.anim == null)
        {
            return;
        }

        if (ctx.Payload is not string skillId || skillId != GameConstants.ConfigIds.SkillShoot)
        {
            return;
        }

        player.anim.SetTrigger("Shoot");
        RefreshAttackSpeedFromStats();
        ApplyAnimatorSpeedMultiplier();
    }

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

    private void RefreshCombatTargets(bool force)
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            autoAttack.RefreshTargets(force);
            return;
        }

        playerController?.ScanCombatTargets();
    }

    private bool HasCombatTarget()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.HasValidTarget;
        }

        return playerController != null && playerController.GetPrimaryTarget() != null;
    }

    private void ApplyAnimatorSpeedMultiplier()
    {
        if (player?.anim == null)
        {
            return;
        }

        player.anim.SetFloat("ShootSpeedMulti", Mathf.Max(0.1f, shootSpeedAnimMulti));
    }

    [ContextMenu("Update Bullet List")]
    private void UpdateTheBulletList() { }
}
