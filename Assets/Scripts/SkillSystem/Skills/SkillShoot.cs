using System;
using UnityEngine;

/// <summary>
/// 射击技能：由射程内敌人检测驱动发弹，不依赖动画攻击帧。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（历史 Prefab 子物体）。</para>
/// <para><b>数据流：</b>扫描目标 → <see cref="ShootSkillController"/> → <see cref="ShootProjectileCaster"/>。</para>
/// </remarks>
public class SkillShoot : SkillBase
{
    public Action<float> updateAttackSpeedMultiAction;

    [Header("Combat")]
    [Tooltip("单发基础间隔（秒），实际间隔 = baseShotInterval / 攻速倍率。")]
    [SerializeField] private float baseShotInterval = 0.3f;
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

    private float shotTimer;
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

    private void Start()
    {
        RefreshAttackSpeedFromStats();
        shotTimer = 0f;
        scanTimer = 0f;
    }

    protected override void Update()
    {
        if (player == null || shootController == null || playerController == null || !playerController.IsReady)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        TickTargetScan(deltaTime);
        TickCombatShoot(deltaTime);
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

    /// <summary>检测驱动的一发射击（不经动画事件）。</summary>
    public void ActivateOneShootAttack()
    {
        if (shootController == null)
        {
            return;
        }

        shootController.ExecuteShoot();
        RefreshAttackSpeedFromStats();
    }

    /// <summary>兼容旧调用：执行目标扫描。</summary>
    public void CheckEnemyInRadiusWithSorted()
    {
        RefreshCombatTargets(force: true);
    }

    /// <summary>兼容旧调用：等价于 <see cref="CanUseShootSkill"/>。</summary>
    public void CheckEnemyIsAvailable() { }

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

    private void TickCombatShoot(float deltaTime)
    {
        shotTimer -= deltaTime;

        if (!HasCombatTarget())
        {
            return;
        }

        if (!CanUseShootSkill())
        {
            return;
        }

        if (shotTimer > 0f)
        {
            return;
        }

        ActivateOneShootAttack();
        shotTimer = GetShotInterval();

        if (driveShootAnimator && player.anim != null)
        {
            player.anim.SetTrigger("Shoot");
            ApplyAnimatorSpeedMultiplier();
        }
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

    private float GetShotInterval()
    {
        float speedMulti = Mathf.Max(0.1f, shootSpeedAnimMulti);
        return baseShotInterval / speedMulti;
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
