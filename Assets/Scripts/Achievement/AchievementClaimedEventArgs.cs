/// <summary>
/// 成就奖励领取成功事件负载。
/// </summary>
public readonly struct AchievementClaimedEventArgs
{
    public string ConfigId { get; }
    public ShopRewardType RewardType { get; }
    public long RewardAmount { get; }

    public AchievementClaimedEventArgs(string configId, ShopRewardType rewardType, long rewardAmount)
    {
        ConfigId = configId ?? string.Empty;
        RewardType = rewardType;
        RewardAmount = rewardAmount;
    }
}
