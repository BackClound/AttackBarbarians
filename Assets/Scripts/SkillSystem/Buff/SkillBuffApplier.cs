/// <summary>
/// 将 <see cref="SkillBuffKind"/> 应用到指定技能的 <see cref="SkillBuffProfile"/>。
/// 流水线位置：Buff 入口 → 本类/<see cref="SkillBuffCatalog"/> → 施法阶段读取 Profile。
/// </summary>
public static class SkillBuffApplier
{
    /// <summary>
    /// 将 Buff 写入单个技能 Profile。
    /// </summary>
    /// <param name="profile">目标 Buff 聚合表。</param>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    public static void ApplyToProfile(SkillBuffProfile profile, SkillBuffKind kind, int tier)
    {
        SkillBuffCatalog.Apply(profile, kind, tier);
    }

    /// <summary>
    /// 将 Buff 批量写入多个技能 Profile（如全局冷却缩减）。
    /// </summary>
    /// <param name="profiles">技能类型 → Profile 映射。</param>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    public static void ApplyGlobalToAll(System.Collections.Generic.IReadOnlyDictionary<SkillType, SkillBuffProfile> profiles, SkillBuffKind kind, int tier)
    {
        if (profiles == null || kind == SkillBuffKind.None)
        {
            return;
        }

        foreach (var pair in profiles)
        {
            if (pair.Value == null)
            {
                continue;
            }

            SkillBuffCatalog.Apply(pair.Value, kind, tier);
        }
    }
}
