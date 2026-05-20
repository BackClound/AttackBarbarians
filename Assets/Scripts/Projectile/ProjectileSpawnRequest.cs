using UnityEngine;

/// <summary>
/// 单次投射物生成请求：来源、方向、伤害上下文与可选追踪目标。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct ProjectileSpawnRequest
{
    public object Source { get; }
    // 目标
    public GameObject Target { get; }
    // 生成位置
    public Vector2 SpawnPosition { get; }
    // 方向
    public Vector2 Direction { get; }
    // 伤害上下文
    public DamageInfo DamageInfo { get; }
    // 投射物数据
    public ProjectileDataSO Data { get; }
    // 弹道排布
    public ProjectileSpawnPattern Pattern { get; }
    // 弹道数量
    public int PatternCount { get; }
    // 弹道角度
    public float PatternAngleDegrees { get; }
    // 技能ID
    public string SkillId { get; }

    public ProjectileSpawnRequest(
        object source,
        GameObject target,
        Vector2 spawnPosition,
        Vector2 direction,
        DamageInfo damageInfo,
        ProjectileDataSO data = null,
        ProjectileSpawnPattern pattern = ProjectileSpawnPattern.Single,
        int patternCount = 1,
        float patternAngleDegrees = 10f,
        string skillId = null)
    {
        Source = source;
        Target = target;
        SpawnPosition = spawnPosition;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        DamageInfo = damageInfo;
        Data = data;
        Pattern = pattern;
        PatternCount = Mathf.Max(1, patternCount);
        PatternAngleDegrees = patternAngleDegrees;
        SkillId = skillId ?? damageInfo.SkillId;
    }

    public static ProjectileSpawnRequest CreateStraight(
        object source,
        GameObject target,
        Vector2 spawnPosition,
        Vector2 direction,
        float baseDamage,
        string skillId = null,
        ProjectileDataSO data = null,
        float skillMultiplier = 1f,
        ElementType element = ElementType.None)
    {
        DamageInfo damage = DamageInfo.Create(
            source,
            target,
            baseDamage,
            skillMultiplier,
            skillId ?? GameConstants.ConfigIds.SkillShoot,
            element);

        return new ProjectileSpawnRequest(
            source,
            target,
            spawnPosition,
            direction,
            damage,
            data,
            ProjectileSpawnPattern.Single,
            1,
            0f,
            skillId);
    }

    public ProjectileSpawnRequest WithTarget(GameObject target)
    {
        return new ProjectileSpawnRequest(
            Source,
            target,
            SpawnPosition,
            Direction,
            DamageInfo.WithTarget(target),
            Data,
            Pattern,
            PatternCount,
            PatternAngleDegrees,
            SkillId);
    }

    public ProjectileSpawnRequest WithDirection(Vector2 direction)
    {
        return new ProjectileSpawnRequest(
            Source,
            Target,
            SpawnPosition,
            direction,
            DamageInfo,
            Data,
            Pattern,
            PatternCount,
            PatternAngleDegrees,
            SkillId);
    }
}
