/// <summary>
/// 资源变更来源，便于日志与后续埋点。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>关联系统：</b><see cref="ResourceManager"/>、<see cref="ResourceChangedEventArgs"/>。</para>
/// </remarks>
public enum ResourceChangeReason
{
    /// <summary>未知或未分类来源。</summary>
    Unknown = 0,
    /// <summary>局内结算奖励（金币）。</summary>
    RunSettlement = 1,
    /// <summary>技能升级奖励（技能升级卡、水晶）。</summary>
    UpgradeReward = 2,
    /// <summary>属性升级奖励（属性升级卡、水晶）。</summary>
    TalentUpgrade = 3,
    /// <summary>装备强化奖励（装备、水晶）。</summary>
    EquipmentEnhance = 4,
    /// <summary>商店购买（金币、水晶）。</summary>
    ShopPurchase = 5,
    /// <summary>商店免费领取（水晶）。</summary>
    ShopFreeDiamond = 6,
    /// <summary>签到奖励（金币、水晶）。</summary>
    DailyReward = 7,
    /// <summary>抽奖奖励（金币、水晶）。</summary>
    Lottery = 8,
    /// <summary>游戏时长奖励（金币、水晶）。</summary>
    GameTimeReward = 9,
    /// <summary>成就奖励（金币、水晶）。</summary>
    AchievementReward = 10,
    /// <summary>体力自然恢复。</summary>
    EnergyRecover = 11,
    /// <summary>观看广告获得广告券或体力。</summary>
    AdReward = 12,
    /// <summary>开局消耗体力。</summary>
    BattleStart = 13,
    /// <summary>调试或开发工具触发的变更。</summary>
    Debug = 99,
}
