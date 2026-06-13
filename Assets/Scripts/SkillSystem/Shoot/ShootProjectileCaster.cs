using UnityEngine;

/// <summary>
/// 射击投射物发射工具：统一 SkillContext + BuffProfile + ProjectileManager 链路。
/// 数据流：<see cref="SkillContext.BuildDamageInfo"/> → <see cref="ProjectileSpawnRequest"/> → <see cref="ProjectileManager.Spawn"/>。
/// </summary>
public static class ShootProjectileCaster
{
    /// <summary>
    /// 向指定敌人发射射击投射物（含扇形多弹道与齐射 Buff）。
    /// </summary>
    /// <param name="context">技能释放上下文。</param>
    /// <param name="runtime">射击技能运行时。</param>
    /// <param name="enemy">主目标敌人。</param>
    /// <param name="spawnPos">发射点世界坐标。</param>
    /// <param name="fanAngleDegrees">多弹道扇形展开角度（度）。</param>
    /// <param name="projectileDataOverride">投射物配置覆盖；为空时使用默认值。</param>
    /// <returns>是否成功生成至少一枚投射物。</returns>
    public static bool TryFireAtEnemy(
        SkillContext context,
        SkillRuntime runtime,
        Enemy enemy,
        Vector2 spawnPos,
        float fanAngleDegrees = 10f,
        ProjectileDataSO projectileDataOverride = null)
    {
        if (context == null || runtime == null || enemy == null)
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out ProjectileManager projectileManager))
        {
            Debug.LogWarning("[ShootProjectileCaster] ProjectileManager 未注册。");
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        int trajectoryLines = buff != null ? Mathf.Max(1, buff.TrajectoryLines) : 1;
        int volley = buff != null ? Mathf.Max(1, buff.ShotsPerVolley) : runtime.BaseData.BulletsPerWave;
        float fan = trajectoryLines > 1 ? fanAngleDegrees : 0f;

        DamageInfo damageTemplate = context.BuildDamageInfo(runtime, enemy.gameObject);
        Vector2 baseDirection = ((Vector2)enemy.transform.position - spawnPos).normalized;

        ProjectileSpawnRequest template = ProjectileSpawnRequest.CreateStraight(
            context.Player,
            enemy.gameObject,
            spawnPos,
            baseDirection,
            damageTemplate.BaseDamage,
            runtime.Config != null ? runtime.Config.ConfigId : GameConstants.ConfigIds.SkillShoot,
            ResolveProjectileData(projectileManager, projectileDataOverride),
            damageTemplate.SkillMultiplier,
            runtime.Config != null ? runtime.Config.ElementType : ElementType.Physical);

        ProjectileRuntimeOverrides overrides = buff != null
            ? ProjectileRuntimeOverrides.FromShootProfile(buff)
            : default;

        int total = trajectoryLines * volley;
        int spawned = 0;
        if (total <= 1)
        {
            spawned = projectileManager.Spawn(template, overrides) != null ? 1 : 0;
        }
        else
        {
            int middle = total / 2;
            for (int i = 0; i < total; i++)
            {
                float angleOffset = (i - middle) * fan;
                Vector2 dir = Rotate(baseDirection, angleOffset);
                if (projectileManager.Spawn(template.WithDirection(dir), overrides) != null)
                {
                    spawned++;
                }
            }
        }

        if (spawned > 0)
        {
            context.NotifyCast(runtime);
            return true;
        }

        return false;
    }

    /// <summary>解析投射物配置（Override 优先，否则 Manager 默认值）。</summary>
    /// <param name="manager">投射物管理器。</param>
    /// <param name="overrideData">覆盖配置。</param>
    /// <returns>投射物数据资产。</returns>
    private static ProjectileDataSO ResolveProjectileData(
        ProjectileManager manager,
        ProjectileDataSO overrideData)
    {
        if (overrideData != null)
        {
            return overrideData;
        }

        return manager != null ? manager.DefaultData : null;
    }

    /// <summary>将二维方向向量旋转指定角度。</summary>
    /// <param name="direction">原始方向。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <returns>旋转后的单位方向。</returns>
    private static Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }
}
