using UnityEngine;

/// <summary>
/// 单次投射物生成请求：来源、方向、伤害上下文与可选追踪目标。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct ProjectileSpawnRequest
{
    /// <summary>伤害/弹道来源对象。</summary>
    public object Source { get; }
    /// <summary>追踪目标 GameObject，可为空。</summary>
    public GameObject Target { get; }
    /// <summary>生成世界坐标。</summary>
    public Vector2 SpawnPosition { get; }
    /// <summary>初始飞行方向（自动归一化）。</summary>
    public Vector2 Direction { get; }
    /// <summary>伤害结算上下文。</summary>
    public DamageInfo DamageInfo { get; }
    /// <summary>投射物配置，为空时使用管理器默认。</summary>
    public ProjectileDataSO Data { get; }
    /// <summary>弹道排布模式。</summary>
    public ProjectileSpawnPattern Pattern { get; }
    /// <summary>排布模式下的弹道数量。</summary>
    public int PatternCount { get; }
    /// <summary>扇形排布时相邻弹道夹角（度）。</summary>
    public float PatternAngleDegrees { get; }
    /// <summary>关联技能 Id。</summary>
    public string SkillId { get; }

    /// <summary>
    /// 构造完整的投射物生成请求。
    /// </summary>
    /// <param name="source">来源对象。</param>
    /// <param name="target">追踪目标。</param>
    /// <param name="spawnPosition">生成位置。</param>
    /// <param name="direction">飞行方向。</param>
    /// <param name="damageInfo">伤害上下文。</param>
    /// <param name="data">投射物配置。</param>
    /// <param name="pattern">弹道排布。</param>
    /// <param name="patternCount">弹道数量。</param>
    /// <param name="patternAngleDegrees">扇形夹角。</param>
    /// <param name="skillId">技能 Id。</param>
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

    /// <summary>
    /// 创建直线单发投射物请求（含伤害上下文工厂）。
    /// </summary>
    /// <param name="source">来源对象。</param>
    /// <param name="target">追踪目标。</param>
    /// <param name="spawnPosition">生成位置。</param>
    /// <param name="direction">飞行方向。</param>
    /// <param name="baseDamage">基础伤害。</param>
    /// <param name="skillId">技能 Id。</param>
    /// <param name="data">投射物配置。</param>
    /// <param name="skillMultiplier">技能倍率。</param>
    /// <param name="element">元素类型。</param>
    /// <returns>构造的生成请求。</returns>
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

    /// <summary>
    /// 返回替换追踪目标后的新请求。
    /// </summary>
    /// <param name="target">新目标。</param>
    /// <returns>更新目标后的副本。</returns>
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

    /// <summary>
    /// 返回替换飞行方向后的新请求。
    /// </summary>
    /// <param name="direction">新方向。</param>
    /// <returns>更新方向后的副本。</returns>
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
