using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 射击技能统一入口：目标扫描、连发休整、投射物发射与 AutoAttack 兼容。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 上，与 <see cref="SkillManager"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class ShootSkillController : MonoBehaviour
{
    [Header("Cast")]
    [SerializeField] private Transform castOrigin;
    [SerializeField] private float fanAngleDegrees = 10f;
    [SerializeField] private ProjectileDataSO projectileDataOverride;

    private readonly List<Enemy> targetScratch = new List<Enemy>(16);

    private Player player;
    private PlayerController controller;
    private AutoAttackController autoAttack;
    private SkillManager skillManager;
    private ShootBurstController burstController;

    public Transform CastOrigin => castOrigin != null ? castOrigin : transform;

    private void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<PlayerController>();
        autoAttack = GetComponent<AutoAttackController>();
        skillManager = GetComponent<SkillManager>();
        burstController = GetComponent<ShootBurstController>();
        if (castOrigin == null)
        {
            castOrigin = transform;
        }
    }

    /// <summary>是否可进入射击 / 释放一发。</summary>
    public bool CanShoot()
    {
        if (PreferSkillShootPipeline())
        {
            return CanShootViaSkillRuntime();
        }

        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.CanAttack;
        }

        return false;
    }

    /// <summary>动画攻击帧或 AutoAttack 调用：向目标发射并推进连发计数。</summary>
    public void ExecuteShoot()
    {
        if (PreferSkillShootPipeline())
        {
            ExecuteShootViaSkillRuntime();
            return;
        }

        if (autoAttack != null && autoAttack.IsReady)
        {
            autoAttack.ExecuteAttack();
        }
    }

    private bool CanShootViaSkillRuntime()
    {
        if (!skillManager.TryGetRuntime(SkillType.Shoot, out SkillRuntime runtime) || !runtime.IsUnlocked)
        {
            return false;
        }

        if (burstController != null && !burstController.CanShoot(runtime))
        {
            return false;
        }

        return controller != null && controller.CopyCombatTargetsTo(targetScratch);
    }

    private void ExecuteShootViaSkillRuntime()
    {

        if (!CanShootViaSkillRuntime() || !skillManager.TryGetRuntime(SkillType.Shoot, out SkillRuntime runtime))
        {
            return;
        }

        if (!TryCopyValidTargets() || targetScratch.Count == 0)
        {
            return;
        }

        Enemy target = SelectTarget(targetScratch);
        if (target == null)
        {
            return;
        }

        SkillContext context = skillManager.Context;
        if (context == null)
        {
            return;
        }

        Vector2 spawnPos = CastOrigin.position;
        if (!ShootProjectileCaster.TryFireAtEnemy(
                context,
                runtime,
                target,
                spawnPos,
                fanAngleDegrees,
                ResolveProjectileData()))
        {
            return;
        }

        burstController?.RecordShot(runtime);
        controller?.NotifyAttackStarted(runtime.Config?.ConfigId);
    }

    public float GetAnimSpeedMultiplier()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.AnimSpeedMultiplier;
        }

        if (controller != null && controller.RuntimeStats.IsInitialized)
        {
            return Mathf.Max(0.1f, controller.RuntimeStats.Get(StatType.AttackSpeedMulti));
        }

        return player != null && player.player_Health != null && player.player_Health.entity_Stats != null
            ? Mathf.Max(0.1f, player.player_Health.entity_Stats.GetAttackSpeedMultiplier())
            : 1f;
    }

    private bool PreferSkillShootPipeline()
    {
        return skillManager != null &&
               skillManager.TryGetRuntime(SkillType.Shoot, out SkillRuntime runtime) &&
               runtime.IsUnlocked;
    }

    private bool TryCopyValidTargets()
    {
        targetScratch.Clear();
        if (controller == null || !controller.CopyCombatTargetsTo(targetScratch))
        {
            return false;
        }

        PruneInvalidTargets(targetScratch);
        return targetScratch.Count > 0;
    }

    private static void PruneInvalidTargets(List<Enemy> targets)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Enemy enemy = targets[i];
            if (enemy == null || enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
            {
                targets.RemoveAt(i);
            }
        }
    }

    private static Enemy SelectTarget(List<Enemy> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Enemy enemy = targets[i];
            if (enemy != null && enemy.enemy_Health != null && enemy.enemy_Health.CanBeDamage())
            {
                return enemy;
            }
        }

        return null;
    }

    private ProjectileDataSO ResolveProjectileData()
    {
        if (projectileDataOverride != null)
        {
            return projectileDataOverride;
        }

        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
