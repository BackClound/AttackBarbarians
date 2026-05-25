using System;
using UnityEngine;

/// <summary>
/// 射击技能兼容层：保留 Prefab 序列化字段与动画回调入口，逻辑委托 <see cref="ShootSkillController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（历史 Prefab 子物体）。</para>
/// </remarks>
public class SkillShoot : SkillBase
{
    public Action<float> updateAttackSpeedMultiAction;

    [Header("Prefab References (Inspector / migration)")]
    [SerializeField] private GameObject bulletSpawnPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform checkPosition;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private string enemyTag;

    private Player player;
    private ShootSkillController shootController;

    public float shootSpeedAnimMulti { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<Player>();
        shootController = player != null ? player.GetComponent<ShootSkillController>() : null;
    }

    private void Start()
    {
        RefreshAttackSpeedFromStats();
    }

    public void RefreshAttackSpeedFromStats()
    {
        if (shootController != null)
        {
            shootSpeedAnimMulti = shootController.GetAnimSpeedMultiplier();
        }
        else if (player != null && player.controller != null && player.controller.AutoAttack != null &&
                 player.controller.AutoAttack.IsReady)
        {
            shootSpeedAnimMulti = player.controller.AutoAttack.AnimSpeedMultiplier;
        }
        else if (player != null && player.controller != null && player.controller.RuntimeStats.IsInitialized)
        {
            shootSpeedAnimMulti = player.controller.RuntimeStats.Get(StatType.AttackSpeedMulti);
        }
        else if (player != null && player.player_Health != null && player.player_Health.entity_Stats != null)
        {
            shootSpeedAnimMulti = player.player_Health.entity_Stats.GetAttackSpeedMultiplier();
        }

        updateAttackSpeedMultiAction?.Invoke(shootSpeedAnimMulti);
    }

    protected override void Update() { }

    public bool CanUseShootSkill() => shootController != null && shootController.CanShoot();

    /// <summary>PlayerShootState 动画攻击帧调用。</summary>
    public void ActivateOneShootAttack()
    {
        if (shootController == null)
        {
            return;
        }

        shootController.ExecuteShoot();
        if (!shootController.CanShoot())
        {
            player?.stateMachine?.ChangeState(player.idleState);
        }
    }

    /// <summary>兼容旧调用；目标列表由 PlayerController 维护。</summary>
    public void CheckEnemyInRadiusWithSorted() { }

    /// <summary>兼容旧调用。</summary>
    public void CheckEnemyIsAvailable() { }

    [ContextMenu("Update Bullet List")]
    private void UpdateTheBulletList() { }
}
