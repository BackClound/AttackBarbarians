using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 射击技能统一入口：由 <see cref="SkillShoot"/> 敌人检测驱动，经 <see cref="SkillManager"/> 发弹。
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
    private SkillManager skillManager;
    private ShootBurstController burstController;

    public Transform CastOrigin => castOrigin != null ? castOrigin : transform;

    private void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<PlayerController>();
        skillManager = GetComponent<SkillManager>();
        burstController = GetComponent<ShootBurstController>();
        if (castOrigin == null)
        {
            castOrigin = transform;
        }
    }

    /// <summary>是否可进入射击 / 释放一发（连发未休整、有目标、射击技能已解锁）。</summary>
    public bool CanShoot()
    {
        if (!TryGetShootRuntime(out SkillRuntime runtime))
        {
            return false;
        }

        if (burstController == null || !burstController.CanShoot(runtime))
        {
            return false;
        }

        return TrySelectTarget(out _);
    }

    /// <summary><see cref="SkillShoot"/> 或 <see cref="AutoAttackController"/> 调用：经技能管线发射一发。</summary>
    public void ExecuteShoot()
    {
        if (!TryGetShootRuntime(out SkillRuntime runtime))
        {
            return;
        }

        if (burstController == null || !burstController.CanShoot(runtime))
        {
            return;
        }

        if (!TrySelectTarget(out Enemy target))
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

        string skillId = runtime.Config != null ? runtime.Config.ConfigId : GameConstants.ConfigIds.SkillShoot;
        controller?.NotifyAttackStarted(skillId);
    }

    public float GetAnimSpeedMultiplier()
    {
        if (controller != null && controller.RuntimeStats.IsInitialized)
        {
            return Mathf.Max(0.1f, controller.RuntimeStats.Get(StatType.AttackSpeedMulti));
        }

        return player != null && player.player_Health != null && player.player_Health.entity_Stats != null
            ? Mathf.Max(0.1f, player.player_Health.entity_Stats.GetAttackSpeedMultiplier())
            : 1f;
    }

    private bool TryGetShootRuntime(out SkillRuntime runtime)
    {
        runtime = null;
        return skillManager != null &&
               skillManager.TryGetRuntime(SkillType.Shoot, out runtime) &&
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

    private bool TrySelectTarget(out Enemy target)
    {
        target = null;
        if (!TryCopyValidTargets())
        {
            return false;
        }

        target = SelectTarget(targetScratch);
        return target != null;
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
