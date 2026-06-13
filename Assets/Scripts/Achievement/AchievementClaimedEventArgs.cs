/// <summary>
/// 成就奖励领取成功事件负载。
/// </summary>
public readonly struct AchievementClaimedEventArgs
{
    /// <summary>成就配置 ID。</summary>
    public string ConfigId { get; }
    /// <summary>发放的奖励类型。</summary>
    public ShopRewardType RewardType { get; }
    /// <summary>发放的奖励数量。</summary>
    public long RewardAmount { get; }

    /// <summary>
    /// 创建成就奖励领取成功事件负载。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="rewardType">奖励类型。</param>
    /// <param name="rewardAmount">奖励数量。</param>
    public AchievementClaimedEventArgs(string configId, ShopRewardType rewardType, long rewardAmount)
    {
        ConfigId = configId ?? string.Empty;
        RewardType = rewardType;
        RewardAmount = rewardAmount;
    }
}
