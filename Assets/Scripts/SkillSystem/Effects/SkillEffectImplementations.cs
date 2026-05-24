using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内置技能效果：射击、闪电、落雷、火雨、水浪、冰霜、恢复。
/// </summary>
public static class SkillEffectFactory
{
    public static ISkillEffect Create(SkillType type)
    {
        switch (type)
        {
            case SkillType.Shoot:
                return new ShootSkillEffect();
            case SkillType.Lightning:
                return new LightningSkillEffect();
            case SkillType.Thunder:
                return new ThunderSkillEffect();
            case SkillType.FireRain:
                return new FireRainSkillEffect();
            case SkillType.WaterWave:
                return new WaterWaveSkillEffect();
            case SkillType.Ice:
                return new IceSkillEffect();
            case SkillType.Heal:
                return new HealSkillEffect();
            default:
                return null;
        }
    }
}

/// <summary>射击：与闪电/冰霜相同，由 <see cref="SkillManager"/> 冷却自动释放。</summary>
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

public sealed class LightningSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(16);
    private readonly List<Enemy> explosionScratch = new List<Enemy>(16);

    public SkillType SkillType => SkillType.Lightning;

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

        SkillBuffProfile buff = runtime.BuffProfile;
        int bolts = Mathf.Max(1, buff.LightningBolts);
        int chainLen = Mathf.Max(1, buff.ChainTargets);

        for (int b = 0; b < bolts; b++)
        {
            Enemy start = b == 0 ? primary : primary;
            List<Enemy> chain = context.GetChainTargets(start, chainLen, runtime.Config.AreaRadius * 2f);
            Vector2 from = context.CastOrigin != null ? context.CastOrigin.position : context.Player.transform.position;
            for (int i = 0; i < chain.Count; i++)
            {
                Enemy target = chain[i];
                if (target == null)
                {
                    continue;
                }

                Vector2 to = target.transform.position;
                SkillCastVfxPlayer.PlayLightningSegment(from, to, chainLen);
                DamageInfo info = context.BuildDamageInfo(runtime, target.gameObject);
                DamagePipeline.Apply(info);

                EnemyStatusController status = target.GetComponent<EnemyStatusController>();
                if (buff.LightningStun && status != null)
                {
                    status.ApplyStun(buff.LightningStunDuration);
                }

                if (i < chain.Count - 1)
                {
                    from = to;
                }
                else if (buff.LightningEndExplosion)
                {
                    context.QueryEnemiesInCircle(to, buff.LightningExplosionRadius, explosionScratch);
                    for (int e = 0; e < explosionScratch.Count; e++)
                    {
                        DamagePipeline.Apply(context.BuildDamageInfo(runtime, explosionScratch[e].gameObject, 0.6f));
                    }
                }
            }
        }

        context.NotifyCast(runtime);
        return true;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

public sealed class ThunderSkillEffect : ISkillEffect
{
    private readonly List<Enemy> anchorTargets = new List<Enemy>(24);
    private readonly List<Enemy> areaScratch = new List<Enemy>(24);

    public SkillType SkillType => SkillType.Thunder;

    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        if (!context.TryCopyTargets(anchorTargets) || anchorTargets.Count == 0)
        {
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        float radius = runtime.Config.AreaRadius * buff.ThunderRadiusScale;
        int strikes = Mathf.Max(1, buff.ThunderStrikeCount);

        for (int s = 0; s < strikes; s++)
        {
            Enemy anchor = anchorTargets[Random.Range(0, anchorTargets.Count)];
            Vector2 center = anchor.transform.position;
            context.QueryEnemiesInCircle(center, radius, areaScratch);
            for (int i = 0; i < areaScratch.Count; i++)
            {
                Enemy enemy = areaScratch[i];
                DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject, 1.1f));
                if (buff.ThunderStunAllInArea)
                {
                    EnemyStatusController status = enemy.GetComponent<EnemyStatusController>();
                    status?.ApplyStun(buff.GetThunderStunDuration());
                }
            }

            if (buff.ThunderPersistentZone)
            {
                SkillAreaZone.Spawn(
                    center,
                    radius,
                    buff.GetThunderZoneDuration(),
                    runtime,
                    context,
                    0.35f);
            }
        }

        context.NotifyCast(runtime);
        return true;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

public sealed class FireRainSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(24);
    private readonly List<Enemy> chainScratch = new List<Enemy>(8);

    public SkillType SkillType => SkillType.FireRain;

    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        if (!context.TryCopyTargets(scratch) || scratch.Count == 0)
        {
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        float radius = runtime.Config.AreaRadius * buff.FireRainRadiusScale;
        int ticks = 1 + Mathf.RoundToInt(buff.FireRainDurationScale * 3f);
        Enemy anchor = scratch[Random.Range(0, scratch.Count)];
        Vector2 center = anchor.transform.position;

        for (int t = 0; t < ticks; t++)
        {
            context.QueryEnemiesInCircle(center, radius, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                ApplyFireHit(context, runtime, scratch[i], buff, chainScratch);
            }
        }

        context.NotifyCast(runtime);
        return true;
    }

    private static void ApplyFireHit(
        SkillContext context,
        SkillRuntime runtime,
        Enemy enemy,
        SkillBuffProfile buff,
        List<Enemy> chainBuffer)
    {
        if (enemy == null)
        {
            return;
        }

        DamageResult result = DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject, 0.85f));
        if (result.IsKill && buff.FireRainChainOnKill > 0 && chainBuffer != null)
        {
            chainBuffer.Clear();
            context.QueryEnemiesInCircle(enemy.transform.position, runtime.Config.AreaRadius, chainBuffer);
            int chained = 0;
            for (int i = 0; i < chainBuffer.Count && chained < buff.FireRainChainOnKill; i++)
            {
                if (chainBuffer[i] == enemy)
                {
                    continue;
                }

                DamagePipeline.Apply(context.BuildDamageInfo(runtime, chainBuffer[i].gameObject, 0.7f));
                chained++;
            }
        }
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

public sealed class WaterWaveSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(24);

    public SkillType SkillType => SkillType.WaterWave;

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

        SkillBuffProfile buff = runtime.BuffProfile;
        Vector2 origin = context.CastOrigin != null ? context.CastOrigin.position : context.Player.transform.position;
        Vector2 dir = ((Vector2)primary.transform.position - origin).normalized;
        int waves = Mathf.Max(1, buff.WaterWaveCount);
        float radius = runtime.Config.AreaRadius * buff.WaterWaveSizeScale;
        float slowMult = Mathf.Clamp(1f - buff.WaterSlowPercent, 0.1f, 0.95f);

        for (int w = 0; w < waves; w++)
        {
            float offset = w * 1.2f;
            Vector2 center = origin + dir * (3f + offset);
            context.QueryEnemiesInCircle(center, radius, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                Enemy enemy = scratch[i];
                DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject));
                EnemyStatusController status = enemy.GetComponent<EnemyStatusController>();
                status?.ApplySlow(buff.GetWaterSlowDuration(), slowMult);
            }
        }

        context.NotifyCast(runtime);
        return true;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

public sealed class IceSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(16);

    public SkillType SkillType => SkillType.Ice;

    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out ProjectileManager projectileManager))
        {
            return false;
        }

        if (!context.TryGetPrimaryTarget(out Enemy primary))
        {
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        Vector2 spawnPos = context.CastOrigin != null ? context.CastOrigin.position : context.Player.transform.position;
        Vector2 baseDir = ((Vector2)primary.transform.position - spawnPos).normalized;
        int lines = Mathf.Max(1, buff.IceTrajectoryLines);
        int shots = Mathf.Max(1, buff.IceShotsPerCast);
        float fanAngle = lines > 1 ? 12f : 0f;

        DamageInfo damageTemplate = context.BuildDamageInfo(runtime, primary.gameObject);
        ProjectileSpawnRequest template = ProjectileSpawnRequest.CreateStraight(
            context.Player,
            primary.gameObject,
            spawnPos,
            baseDir,
            damageTemplate.BaseDamage,
            runtime.Config.ConfigId,
            null,
            damageTemplate.SkillMultiplier,
            ElementType.Ice);

        ProjectileRuntimeOverrides overrides = ProjectileRuntimeOverrides.FromIceProfile(buff);
        int total = lines * shots;
        int middle = total / 2;
        int spawned = 0;
        for (int i = 0; i < total; i++)
        {
            float angle = (i - middle) * fanAngle;
            Vector2 dir = Rotate(baseDir, angle);
            ProjectileSpawnRequest req = template.WithDirection(dir);
            if (projectileManager.Spawn(req, overrides) != null)
            {
                spawned++;
            }
        }

        if (spawned > 0)
        {
            context.NotifyCast(runtime);
            return true;
        }

        return false;
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

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

public sealed class HealSkillEffect : ISkillEffect
{
    public SkillType SkillType => SkillType.Heal;

    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context?.Player?.player_Health == null || runtime?.BuffProfile == null)
        {
            return false;
        }

        if (runtime.BuffProfile.HealOneTimeFull)
        {
            context.Player.player_Health.RestoreToFull();
            runtime.BuffProfile.HealOneTimeFull = false;
            context.NotifyCast(runtime);
            return true;
        }

        return false;
    }

    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);

    public static void TickPassive(SkillContext context, SkillRuntime runtime, float deltaTime)
    {
        if (context?.Player?.player_Health == null || runtime?.BuffProfile == null)
        {
            return;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        Player_Health health = context.Player.player_Health;

        if (buff.HealRegenPercentPerSecond)
        {
            float amount = health.MaxHp * 0.01f * deltaTime;
            health.Heal(amount);
        }

    }
}

/// <summary>落雷持续区域简易 Tick（非池化，低频创建）。</summary>
public sealed class SkillAreaZone : MonoBehaviour
{
    private float remaining;
    private float interval = 0.35f;
    private float tickTimer;
    private float radius;
    private SkillRuntime runtime;
    private SkillContext context;
    private readonly List<Enemy> scratch = new List<Enemy>(16);

    public static void Spawn(Vector2 center, float radius, float duration, SkillRuntime runtime, SkillContext context, float tickInterval)
    {
        var go = new GameObject("SkillAreaZone");
        go.transform.position = center;
        var zone = go.AddComponent<SkillAreaZone>();
        zone.radius = radius;
        zone.remaining = duration;
        zone.runtime = runtime;
        zone.context = context;
        zone.interval = tickInterval;
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        tickTimer -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (tickTimer > 0f || context == null || runtime == null)
        {
            return;
        }

        tickTimer = interval;
        context.QueryEnemiesInCircle(transform.position, radius, scratch);
        for (int i = 0; i < scratch.Count; i++)
        {
            DamagePipeline.Apply(context.BuildDamageInfo(runtime, scratch[i].gameObject, 0.5f));
        }
    }
}
