/// <summary>
/// 特殊敌人判定与默认能力配置 Id 映射（纯逻辑，无挂载）。
/// </summary>
public static class SpecialEnemyRules
{
    private const EnemyAbilityTag MechanicalMask =
        EnemyAbilityTag.Charge |
        EnemyAbilityTag.Shield |
        EnemyAbilityTag.Split |
        EnemyAbilityTag.Summon |
        EnemyAbilityTag.Ranged;

    /// <summary>
    /// 判断能力标签是否包含可执行机制。
    /// </summary>
    /// <param name="tags">敌人能力标签。</param>
    /// <returns>含机制返回 true，否则返回 false。</returns>
    public static bool HasMechanics(EnemyAbilityTag tags) => (tags & MechanicalMask) != 0;

    /// <summary>
    /// 获取指定能力标签的默认配置 Id。
    /// </summary>
    /// <param name="tag">能力标签。</param>
    /// <returns>默认 configId；无映射时返回空字符串。</returns>
    public static string GetDefaultAbilityConfigId(EnemyAbilityTag tag)
    {
        switch (tag)
        {
            case EnemyAbilityTag.Charge:
                return GameConstants.ConfigIds.SpecialAbilityCharge;
            case EnemyAbilityTag.Shield:
                return GameConstants.ConfigIds.SpecialAbilityShield;
            case EnemyAbilityTag.Split:
                return GameConstants.ConfigIds.SpecialAbilitySplit;
            case EnemyAbilityTag.Summon:
                return GameConstants.ConfigIds.SpecialAbilitySummon;
            case EnemyAbilityTag.Ranged:
                return GameConstants.ConfigIds.SpecialAbilityRanged;
            default:
                return string.Empty;
        }
    }
}
