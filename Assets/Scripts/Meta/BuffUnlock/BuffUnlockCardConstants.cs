/// <summary>
/// Buff 解锁卡 configId 常量（局外科技页背包与消耗）。
/// </summary>
public static class BuffUnlockCardConstants
{
    /// <summary>通用 Buff 解锁卡（可解锁路径中的通用节点）。</summary>
    public const string Global = "buff_unlock_card.global";

    /// <summary>各技能专属 Buff 解锁卡 configId。</summary>
    public static class SkillCards
    {
        public const string Shoot = "buff_unlock_card.skill.shoot";
        public const string Lightning = "buff_unlock_card.skill.lightning";
        public const string Thunder = "buff_unlock_card.skill.thunder";
        public const string FireRain = "buff_unlock_card.skill.fire_rain";
        public const string WaterWave = "buff_unlock_card.skill.water_wave";
        public const string Ice = "buff_unlock_card.skill.ice";
        public const string Heal = "buff_unlock_card.skill.heal";
    }

    /// <summary>通用卡显示名。</summary>
    public const string GlobalDisplayName = "通用 Buff 解锁卡";

    /// <summary>按技能类型解析专属解锁卡 ID。</summary>
    public static string GetSkillCardId(SkillType skillType) =>
        skillType switch
        {
            SkillType.Shoot => SkillCards.Shoot,
            SkillType.Lightning => SkillCards.Lightning,
            SkillType.Thunder => SkillCards.Thunder,
            SkillType.FireRain => SkillCards.FireRain,
            SkillType.WaterWave => SkillCards.WaterWave,
            SkillType.Ice => SkillCards.Ice,
            SkillType.Heal => SkillCards.Heal,
            _ => string.Empty,
        };

    /// <summary>按技能 configId 解析专属解锁卡 ID。</summary>
    public static string GetSkillCardId(string skillConfigId) =>
        skillConfigId switch
        {
            GameConstants.ConfigIds.SkillShoot => SkillCards.Shoot,
            GameConstants.ConfigIds.SkillLightning => SkillCards.Lightning,
            GameConstants.ConfigIds.SkillThunder => SkillCards.Thunder,
            GameConstants.ConfigIds.SkillFireRain => SkillCards.FireRain,
            GameConstants.ConfigIds.SkillWaterWave => SkillCards.WaterWave,
            GameConstants.ConfigIds.SkillIce => SkillCards.Ice,
            GameConstants.ConfigIds.SkillHeal => SkillCards.Heal,
            _ => string.Empty,
        };
}
