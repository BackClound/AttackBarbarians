/// <summary>
/// 升级卡与奖励池 configId 常量。
/// </summary>
public static class UpgradeCardConstants
{
    /// <summary>升级卡配置 ID 常量。</summary>
    public static class CardIds
    {
        /// <summary>射击技能升级卡。</summary>
        public const string SkillShoot = "upgrade_card.skill.shoot";
        /// <summary>火雨技能升级卡。</summary>
        public const string SkillFireRain = "upgrade_card.skill.fire_rain";
        /// <summary>冰冻技能升级卡。</summary>
        public const string SkillIce = "upgrade_card.skill.ice";
        /// <summary>闪电技能升级卡。</summary>
        public const string SkillLightning = "upgrade_card.skill.lightning";
        /// <summary>雷鸣技能升级卡。</summary>
        public const string SkillThunder = "upgrade_card.skill.thunder";
        /// <summary>水波技能升级卡。</summary>
        public const string SkillWaterWave = "upgrade_card.skill.water_wave";
        /// <summary>治疗技能升级卡。</summary>
        public const string SkillHeal = "upgrade_card.skill.heal";
        /// <summary>通用技能升级卡。</summary>
        public const string SkillGeneric = "upgrade_card.skill.generic";

        /// <summary>最大生命值属性升级卡。</summary>
        public const string AttrMaxHp = "upgrade_card.attr.max_hp";
        /// <summary>攻击力属性升级卡。</summary>
        public const string AttrDamage = "upgrade_card.attr.damage";
        /// <summary>移动速度属性升级卡。</summary>
        public const string AttrMoveSpeed = "upgrade_card.attr.move_speed";
        /// <summary>攻击速度属性升级卡。</summary>
        public const string AttrAttackSpeed = "upgrade_card.attr.attack_speed";
        /// <summary>通用属性升级卡。</summary>
        public const string AttrGeneric = "upgrade_card.attr.generic";
    }

    /// <summary>升级卡奖励池配置 ID 常量。</summary>
    public static class PoolIds
    {
        /// <summary>商店普通宝箱奖池。</summary>
        public const string ShopCrateCommon = "upgrade_card_pool.shop_crate_common";
        /// <summary>商店高级宝箱奖池。</summary>
        public const string ShopCratePremium = "upgrade_card_pool.shop_crate_premium";
        /// <summary>离线巡逻奖励奖池。</summary>
        public const string OfflineReward = "upgrade_card_pool.offline_reward";
        /// <summary>在线时长奖励奖池。</summary>
        public const string OnlineReward = "upgrade_card_pool.online_reward";
        /// <summary>抽奖奖池。</summary>
        public const string Lottery = "upgrade_card_pool.lottery";
        /// <summary>每日奖励奖池。</summary>
        public const string DailyReward = "upgrade_card_pool.daily_reward";
        /// <summary>局内结算奖励奖池。</summary>
        public const string RunSettlement = "upgrade_card_pool.run_settlement";
        /// <summary>关卡/阶段奖励奖池。</summary>
        public const string StageReward = "upgrade_card_pool.stage_reward";
    }

    /// <summary>所有可映射为技能升级卡的技能配置 ID。</summary>
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

    /// <summary>核心属性类型列表（用于通用属性卡解析）。</summary>
    public static readonly StatType[] CoreAttributeStats =
    {
        StatType.MaxHp,
        StatType.Damage,
        StatType.MoveSpeed,
        StatType.AttackSpeed,
    };

    /// <summary>根据属性类型获取对应的升级卡配置 ID。</summary>
    /// <param name="statType">属性类型。</param>
    /// <returns>升级卡配置 ID；不支持时返回空字符串。</returns>
    public static string GetAttributeConfigId(StatType statType) =>
        statType switch
        {
            StatType.MaxHp => CardIds.AttrMaxHp,
            StatType.Damage => CardIds.AttrDamage,
            StatType.MoveSpeed => CardIds.AttrMoveSpeed,
            StatType.AttackSpeed => CardIds.AttrAttackSpeed,
            _ => string.Empty,
        };

    /// <summary>根据技能配置 ID 获取对应的技能升级卡配置 ID。</summary>
    /// <param name="skillConfigId">技能配置 ID。</param>
    /// <returns>技能升级卡配置 ID；不支持时返回空字符串。</returns>
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
