/// <summary>
/// 将 <see cref="SkillBuffKind"/> 应用到指定技能的 <see cref="SkillBuffProfile"/>。
/// </summary>
public static class SkillBuffApplier
{
    public static void ApplyToProfile(SkillBuffProfile profile, SkillBuffKind kind, int tier)
    {
        SkillBuffCatalog.Apply(profile, kind, tier);
    }

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
