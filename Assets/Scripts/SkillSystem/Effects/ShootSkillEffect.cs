using UnityEngine;

/// <summary>
/// 射击技能效果：由 <see cref="SkillManager"/> 自动施法；目标经 <see cref="PlayerTargetScanner"/> 按策略排序
/// （最近 / 最低血量 / Boss 优先等，见 <see cref="PlayerDataSO.TargetPolicy"/>）。
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

        if (!context.TryGetPrimaryTarget(out Enemy target))
        {
            return false;
        }

        Vector2 spawnPos = context.CastOrigin != null
            ? context.CastOrigin.position
            : context.Player.transform.position;

        if (!ShootProjectileCaster.TryFireAtEnemy(
                context,
                runtime,
                target,
                spawnPos,
                DefaultFanAngleDegrees,
                ResolveProjectileData()))
        {
            return false;
        }

        context.NotifyCast(runtime);
        return true;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);

    private static ProjectileDataSO ResolveProjectileData()
    {
        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
