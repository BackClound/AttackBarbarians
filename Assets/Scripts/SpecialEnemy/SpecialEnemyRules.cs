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

    public static bool HasMechanics(EnemyAbilityTag tags) => (tags & MechanicalMask) != 0;

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
