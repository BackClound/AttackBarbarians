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

    public int DayIndex => Mathf.Clamp(dayIndex, 1, 7);
    public ShopRewardType RewardType => rewardType;
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);
        if (dayIndex < 1 || dayIndex > 7)
        {
            result.AddError(name, "dayIndex 必须在 1～7 之间。");
        }

        if (rewardAmount <= 0)
        {
            result.AddError(name, "rewardAmount 必须大于 0。");
        }
    }
}
