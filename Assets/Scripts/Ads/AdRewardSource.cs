/// <summary>
/// 激励广告发奖业务来源，用于埋点与 UI 提示。
/// </summary>
public enum AdRewardSource
{
    /// <summary>商城广告补给领取。</summary>
    Shop = 0,

    /// <summary>观看广告获取广告券。</summary>
    AdTicket = 1,

    /// <summary>每日奖励相关广告。</summary>
    DailyReward = 2,

    /// <summary>通用或未分类来源。</summary>
    Generic = 3,

    /// <summary>局内三选一刷新候选。</summary>
    UpgradeReroll = 4,

    /// <summary>局内三选一全选。</summary>
    UpgradeSelectAll = 5,
}
