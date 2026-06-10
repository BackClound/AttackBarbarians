/// <summary>
/// 升级卡展示用英文标题与中文副标题。
/// </summary>
public static class UpgradeCardDisplayNames
{
    public static string GetEnglishTitle(string cardConfigId)
    {
        if (string.IsNullOrWhiteSpace(cardConfigId))
        {
            return string.Empty;
        }

        return cardConfigId switch
        {
            UpgradeCardConstants.CardIds.SkillShoot => "Shoot",
            UpgradeCardConstants.CardIds.SkillFireRain => "Fire Rain",
            UpgradeCardConstants.CardIds.SkillIce => "Ice",
            UpgradeCardConstants.CardIds.SkillLightning => "Lightning",
            UpgradeCardConstants.CardIds.SkillThunder => "Thunder",
            UpgradeCardConstants.CardIds.SkillWaterWave => "Water Wave",
            UpgradeCardConstants.CardIds.SkillHeal => "Heal",
            UpgradeCardConstants.CardIds.SkillGeneric => "Universal Skill",
            UpgradeCardConstants.CardIds.AttrMaxHp => "HP",
            UpgradeCardConstants.CardIds.AttrDamage => "ATK",
            UpgradeCardConstants.CardIds.AttrMoveSpeed => "Move Speed",
            UpgradeCardConstants.CardIds.AttrAttackSpeed => "Attack Speed",
            UpgradeCardConstants.CardIds.AttrGeneric => "Universal Attribute",
            _ => cardConfigId,
        };
    }

    public static int GetStarCount(UpgradeRarity rarity) =>
        rarity switch
        {
            UpgradeRarity.Common => 1,
            UpgradeRarity.Uncommon => 2,
            UpgradeRarity.Rare => 3,
            UpgradeRarity.Epic => 4,
            UpgradeRarity.Legendary => 5,
            _ => 3,
        };
}
