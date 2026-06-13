using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能效果工厂：按 <see cref="SkillType"/> 创建 <see cref="ISkillEffect"/> 实例。
/// 流水线位置：<see cref="SkillManager"/> 注册阶段 → 自动施法循环调用各效果。
/// </summary>
public static class SkillEffectFactory
{
    /// <summary>
    /// 创建指定类型的技能效果实现。
    /// </summary>
    /// <param name="type">技能类型。</param>
    /// <returns>效果实例；未知类型返回 null。</returns>
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

/// <summary>
/// 闪电技能效果：链式命中、麻痹与末端爆炸。
/// 流水线位置：SkillManager → 本类 → VFX + DamagePipeline + EnemyStatusController。
/// </summary>
public sealed class LightningSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(16);
    private readonly List<Enemy> explosionScratch = new List<Enemy>(16);

    /// <summary>技能类型：闪电。</summary>
    public SkillType SkillType => SkillType.Lightning;

    /// <summary>
    /// 自动施法：从主目标出发链式伤害，可选麻痹与末端 AoE。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">闪电运行时。</param>
    /// <returns>是否成功施放。</returns>
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">闪电运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

/// <summary>
/// 落雷技能效果：随机锚点 AoE、全体麻痹与持续伤害圈。
/// 流水线位置：SkillManager → 本类 → DamagePipeline + SkillAreaZone。
/// </summary>
public sealed class ThunderSkillEffect : ISkillEffect
{
    private readonly List<Enemy> anchorTargets = new List<Enemy>(24);
    private readonly List<Enemy> areaScratch = new List<Enemy>(24);

    /// <summary>技能类型：落雷。</summary>
    public SkillType SkillType => SkillType.Thunder;

    /// <summary>
    /// 自动施法：多次随机落点范围伤害，可选麻痹与持续圈。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">落雷运行时。</param>
    /// <returns>是否成功施放。</returns>
    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        float radius = runtime.Config.AreaRadius * buff.ThunderRadiusScale;
        int strikes = Mathf.Max(1, buff.ThunderStrikeCount);

        for (int s = 0; s < strikes; s++)
        {
            if (!context.TryPickRandomTarget(anchorTargets, out Enemy anchor))
            {
                break;
            }
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">落雷运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

/// <summary>
/// 火雨技能效果：随机区域多段 Tick 伤害与击杀连锁。
/// 流水线位置：SkillManager → 本类 → DamagePipeline（含击杀连锁二次伤害）。
/// </summary>
public sealed class FireRainSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(24);
    private readonly List<Enemy> chainScratch = new List<Enemy>(8);

    /// <summary>技能类型：火雨。</summary>
    public SkillType SkillType => SkillType.FireRain;

    /// <summary>
    /// 自动施法：以随机敌人为锚点进行多 Tick 范围伤害。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">火雨运行时。</param>
    /// <returns>是否成功施放。</returns>
    public bool TryAutoCast(SkillContext context, SkillRuntime runtime)
    {
        if (context == null || runtime?.Config == null || !runtime.Config.AutoCast)
        {
            return false;
        }

        if (!context.TryPickRandomTarget(scratch, out Enemy anchor))
        {
            return false;
        }

        SkillBuffProfile buff = runtime.BuffProfile;
        float radius = runtime.Config.AreaRadius * buff.FireRainRadiusScale;
        int ticks = 1 + Mathf.RoundToInt(buff.FireRainDurationScale * 3f);
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

    /// <summary>
    /// 对单个敌人造成火雨伤害；击杀时可选连锁附近目标。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">火雨运行时。</param>
    /// <param name="enemy">受击敌人。</param>
    /// <param name="buff">火雨 Buff 表。</param>
    /// <param name="chainBuffer">连锁查询缓冲区。</param>
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">火雨运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

/// <summary>
/// 水浪技能效果：朝向主目标的多道方向波，附带减速。
/// 流水线位置：SkillManager → 本类 → DamagePipeline + EnemyStatusController。
/// </summary>
public sealed class WaterWaveSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(24);

    /// <summary>技能类型：水浪。</summary>
    public SkillType SkillType => SkillType.WaterWave;

    /// <summary>
    /// 自动施法：沿主目标方向释放多道水浪并减速命中敌人。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">水浪运行时。</param>
    /// <returns>是否成功施放。</returns>
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">水浪运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

/// <summary>
/// 冰霜技能效果：扇形冰霜投射物，路径穿透与命中冰冻。
/// 流水线位置：SkillManager → 本类 → ProjectileManager → DamagePipeline。
/// </summary>
public sealed class IceSkillEffect : ISkillEffect
{
    private readonly List<Enemy> scratch = new List<Enemy>(16);

    /// <summary>技能类型：冰霜。</summary>
    public SkillType SkillType => SkillType.Ice;

    /// <summary>
    /// 自动施法：向主目标方向生成多道冰霜投射物。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">冰霜运行时。</param>
    /// <returns>是否至少成功生成一枚投射物。</returns>
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

    /// <summary>将二维方向向量旋转指定角度。</summary>
    /// <param name="direction">原始方向。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <returns>归一化后的新方向。</returns>
    private static Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">冰霜运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);
}

/// <summary>
/// 恢复技能效果：被动 Tick 回血与一次性满血 Buff 触发。
/// 流水线位置：SkillManager.Update 被动 Tick + 冷却就绪时 TryAutoCast。
/// </summary>
public sealed class HealSkillEffect : ISkillEffect
{
    /// <summary>技能类型：恢复。</summary>
    public SkillType SkillType => SkillType.Heal;

    /// <summary>
    /// 自动施法：处理一次性满血 Buff（被动回血由 <see cref="TickPassive"/> 驱动）。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">恢复运行时。</param>
    /// <returns>是否触发了一次性满血。</returns>
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

    /// <summary>外部触发时复用自动施法逻辑。</summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">恢复运行时。</param>
    public void OnExternalCast(SkillContext context, SkillRuntime runtime) => TryAutoCast(context, runtime);

    /// <summary>
    /// 每帧被动回血（每秒 1% 最大生命，需 Buff 开启）。
    /// </summary>
    /// <param name="context">技能上下文。</param>
    /// <param name="runtime">恢复运行时。</param>
    /// <param name="deltaTime">帧间隔秒数。</param>
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

/// <summary>
/// 落雷持续区域简易 Tick（非池化，低频创建）；由 <see cref="ThunderSkillEffect"/> 生成。
/// 流水线位置：落雷 Buff 持续圈 → 本组件 Update → DamagePipeline。
/// </summary>
public sealed class SkillAreaZone : MonoBehaviour
{
    private float remaining;
    private float interval = 0.35f;
    private float tickTimer;
    private float radius;
    private SkillRuntime runtime;
    private SkillContext context;
    private readonly List<Enemy> scratch = new List<Enemy>(16);

    /// <summary>
    /// 在指定位置生成持续伤害区域。
    /// </summary>
    /// <param name="center">区域中心。</param>
    /// <param name="radius">伤害半径。</param>
    /// <param name="duration">存在时长（秒）。</param>
    /// <param name="runtime">关联技能运行时。</param>
    /// <param name="context">技能上下文。</param>
    /// <param name="tickInterval">Tick 间隔（秒）。</param>
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

    /// <summary>每帧递减寿命并按间隔对范围内敌人造成伤害。</summary>
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
