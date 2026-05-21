using UnityEngine;

/// <summary>
/// 射击投射物发射工具：统一 SkillContext + BuffProfile + ProjectileManager 链路。
/// </summary>
public static class ShootProjectileCaster
{
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
