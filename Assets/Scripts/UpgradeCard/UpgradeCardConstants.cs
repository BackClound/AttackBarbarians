/// <summary>
/// 升级卡与奖励池 configId 常量。
/// </summary>
public static class UpgradeCardConstants
{
    public static class CardIds
    {
        public const string SkillShoot = "upgrade_card.skill.shoot";
        public const string SkillFireRain = "upgrade_card.skill.fire_rain";
        public const string SkillIce = "upgrade_card.skill.ice";
        public const string SkillLightning = "upgrade_card.skill.lightning";
        public const string SkillThunder = "upgrade_card.skill.thunder";
        public const string SkillWaterWave = "upgrade_card.skill.water_wave";
        public const string SkillHeal = "upgrade_card.skill.heal";
        public const string SkillGeneric = "upgrade_card.skill.generic";

        public const string AttrMaxHp = "upgrade_card.attr.max_hp";
        public const string AttrDamage = "upgrade_card.attr.damage";
        public const string AttrMoveSpeed = "upgrade_card.attr.move_speed";
        public const string AttrAttackSpeed = "upgrade_card.attr.attack_speed";
        public const string AttrGeneric = "upgrade_card.attr.generic";
    }

    public static class PoolIds
    {
        public const string ShopCrateCommon = "upgrade_card_pool.shop_crate_common";
        public const string ShopCratePremium = "upgrade_card_pool.shop_crate_premium";
        public const string OfflineReward = "upgrade_card_pool.offline_reward";
        public const string OnlineReward = "upgrade_card_pool.online_reward";
        public const string Lottery = "upgrade_card_pool.lottery";
        public const string DailyReward = "upgrade_card_pool.daily_reward";
        public const string RunSettlement = "upgrade_card_pool.run_settlement";
        public const string StageReward = "upgrade_card_pool.stage_reward";
    }

    public static readonly string[] AllSkillConfigIds =
    {
        GameConstants.ConfigIds.SkillShoot,
        GameConstants.ConfigIds.SkillFireRain,
        GameConstants.ConfigIds.SkillIce,
        GameConstants.ConfigIds.SkillLightning,
        GameConstants.ConfigIds.SkillThunder,
        GameConstants.ConfigIds.SkillWaterWave,
        GameConstants.ConfigIds.SkillHeal,
    };

    public static readonly StatType[] CoreAttributeStats =
    {
        StatType.MaxHp,
        StatType.Damage,
        StatType.MoveSpeed,
        StatType.AttackSpeed,
    };

    public static string GetAttributeConfigId(StatType statType) =>
        statType switch
        {
            StatType.MaxHp => CardIds.AttrMaxHp,
            StatType.Damage => CardIds.AttrDamage,
            StatType.MoveSpeed => CardIds.AttrMoveSpeed,
            StatType.AttackSpeed => CardIds.AttrAttackSpeed,
            _ => string.Empty,
        };

    public static string GetSkillCardConfigId(string skillConfigId) =>
        skillConfigId switch
        {
            GameConstants.ConfigIds.SkillShoot => CardIds.SkillShoot,
            GameConstants.ConfigIds.SkillFireRain => CardIds.SkillFireRain,
            GameConstants.ConfigIds.SkillIce => CardIds.SkillIce,
            GameConstants.ConfigIds.SkillLightning => CardIds.SkillLightning,
            GameConstants.ConfigIds.SkillThunder => CardIds.SkillThunder,
            GameConstants.ConfigIds.SkillWaterWave => CardIds.SkillWaterWave,
            GameConstants.ConfigIds.SkillHeal => CardIds.SkillHeal,
            _ => string.Empty,
        };
}
