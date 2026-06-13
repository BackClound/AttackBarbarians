/// <summary>
/// 升级卡奖励来源，用于埋点与 UI 展示。
/// </summary>
public enum UpgradeCardRewardSource
{
    /// <summary>未知来源。</summary>
    Unknown = 0,
    /// <summary>商店宝箱。</summary>
    ShopCrate = 1,
    /// <summary>离线巡逻奖励。</summary>
    OfflineReward = 2,
    /// <summary>在线时长奖励。</summary>
    OnlineReward = 3,
    /// <summary>抽奖。</summary>
    Lottery = 4,
    /// <summary>每日奖励。</summary>
    DailyReward = 5,
    /// <summary>局内结算奖励。</summary>
    RunSettlement = 6,
    /// <summary>关卡/阶段奖励。</summary>
    StageReward = 7,
}
