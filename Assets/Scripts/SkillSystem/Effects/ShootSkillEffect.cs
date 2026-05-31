using UnityEngine;

/// <summary>
/// 射击技能效果：兼容技能管线的外部释放入口，实际常规射击由 <see cref="SkillShoot"/> 检测驱动。
/// </summary>
public sealed class ShootSkillEffect : ISkillEffect
{
    private const float DefaultFanAngleDegrees = 10f;

    public SkillType SkillType => SkillType.Shoot;

    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        if (!context.TryGetPrimaryTarget(out Enemy primary))
        {
            return false;
        }

        Vector2 spawnPos = context.CastOrigin != null
            ? context.CastOrigin.position
            : context.Player.transform.position;

        if (!ShootProjectileCaster.TryFireAtEnemy(
                context,
                runtime,
                primary,
                spawnPos,
                DefaultFanAngleDegrees,
                ResolveProjectileData()))
        {
            return false;
        }

        context.Controller?.NotifyAttackStarted(runtime.Config.ConfigId);
        return true;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);

    private static ProjectileDataSO ResolveProjectileData()
    {
        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
