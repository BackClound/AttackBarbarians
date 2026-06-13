/// <summary>
/// 升级卡展示用英文标题与中文副标题。
/// </summary>
public static class UpgradeCardDisplayNames
{
    /// <summary>根据卡片配置 ID 获取英文展示标题。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <returns>英文标题；未知 ID 时返回原 ID。</returns>
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

    /// <summary>根据稀有度获取星级数量。</summary>
    /// <param name="rarity">升级卡稀有度。</param>
    /// <returns>星级数量（1–5）。</returns>
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
