/// <summary>
/// 资源变更来源，便于日志与后续埋点。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// </remarks>
public enum ResourceChangeReason
{
    Unknown = 0,
    // 局内结算奖励 金币
    RunSettlement = 1,
    // 技能升级奖励 技能升级卡 水晶
    UpgradeReward = 2,
    // 属性升级奖励 属性升级卡 水晶
    TalentUpgrade = 3,
    // 装备升级奖励 装备 水晶
    EquipmentEnhance = 4,
    // 商店购买奖励 金币 水晶
    ShopPurchase = 5,
    // 商店免费领取奖励 水晶
    ShopFreeDiamond = 6,
    // 签到奖励 金币 水晶
    DailyReward = 7,
    // 成就奖励 金币 水晶
    AchievementReward = 10,
    // 抽奖奖励 金币 水晶
    Lottery = 8,
    // 游戏时长奖励 金币 水晶
    GameTimeReward = 9,
    // 自然恢复（已废弃，保留枚举值兼容旧存档日志）
    EnergyRecover = 11,
    // 观看广告获得广告券
    AdReward = 12,
    // 开局消耗体力
    BattleStart = 13,
    Debug = 99,
}
