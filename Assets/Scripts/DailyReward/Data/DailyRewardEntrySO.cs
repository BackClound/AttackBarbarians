using UnityEngine;

/// <summary>
/// 七日签到单日奖励配置。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/DailyReward/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "DailyRewardEntry", menuName = "Attack Barbarians/Daily Reward/Daily Reward Entry")]
public class DailyRewardEntrySO : ConfigDataBase
{
    [Header("Schedule")]
    [SerializeField] private int dayIndex = 1;

    [Header("Reward")]
    [SerializeField] private ShopRewardType rewardType = ShopRewardType.Gold;
    [SerializeField] private long rewardAmount = 100;
    [SerializeField] private string upgradeCardPoolConfigId;
    [SerializeField] private int upgradeCardDrawCount = 1;

    /// <summary>签到天数索引（1～7）。</summary>
    public int DayIndex => Mathf.Clamp(dayIndex, 1, 7);
    /// <summary>奖励类型。</summary>
    public ShopRewardType RewardType => rewardType;
    /// <summary>奖励数量。</summary>
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);
    /// <summary>升级卡奖池配置 ID（可选）。</summary>
    public string UpgradeCardPoolConfigId => upgradeCardPoolConfigId;
    /// <summary>升级卡抽取次数。</summary>
    public int UpgradeCardDrawCount => Mathf.Max(0, upgradeCardDrawCount);
    /// <summary>是否包含升级卡奖励。</summary>
    public bool HasUpgradeCardReward => !string.IsNullOrWhiteSpace(upgradeCardPoolConfigId) && upgradeCardDrawCount > 0;

    /// <summary>
    /// 收集单日签到奖励配置校验错误。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);
        if (dayIndex < 1 || dayIndex > 7)
        {
            result.AddError(name, "dayIndex 必须在 1～7 之间。");
        }

        if (!HasUpgradeCardReward && rewardAmount <= 0)
        {
            result.AddError(name, "rewardAmount 必须大于 0，或配置 upgradeCardPoolConfigId。");
        }
    }
}
