using UnityEngine;

/// <summary>
/// 射击技能效果：由 <see cref="SkillManager"/> 自动施法；目标经 <see cref="PlayerTargetScanner"/> 按策略排序
/// （最近 / 最低血量 / Boss 优先等，见 <see cref="PlayerDataSO.TargetPolicy"/>）。
/// 流水线位置：SkillManager 冷却就绪 → 本类 → <see cref="ShootProjectileCaster"/> → DamagePipeline。
/// </summary>
public sealed class ShootSkillEffect : ISkillEffect
{
    private const float DefaultFanAngleDegrees = 10f;

    /// <summary>技能类型：射击。</summary>
    public SkillType SkillType => SkillType.Shoot;

    /// <summary>
    /// 自动施法：选取主目标并发射投射物。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">射击运行时。</param>
    /// <returns>是否成功施放。</returns>
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">射击运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);

    /// <summary>解析默认投射物配置。</summary>
    /// <returns>投射物数据；Manager 未注册时返回 null。</returns>
    private static ProjectileDataSO ResolveProjectileData()
    {
        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
