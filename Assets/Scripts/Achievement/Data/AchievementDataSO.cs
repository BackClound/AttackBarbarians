using UnityEngine;

/// <summary>
/// 单条成就配置：目标类型、目标值与奖励。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Achievement/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "Achievement", menuName = "Attack Barbarians/Achievement/Achievement Data")]
public class AchievementDataSO : ConfigDataBase
{
    [Header("Presentation")]
    [SerializeField] private string description;

    [Header("Target")]
    [SerializeField] private AchievementTargetType targetType = AchievementTargetType.TotalEnemyKills;
    [SerializeField] private int targetValue = 10;

    [Header("Reward")]
    [SerializeField] private ShopRewardType rewardType = ShopRewardType.Gold;
    [SerializeField] private long rewardAmount = 100;

    public string Description => description;
    public AchievementTargetType TargetType => targetType;
    public int TargetValue => Mathf.Max(1, targetValue);
    public ShopRewardType RewardType => rewardType;
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);
        if (targetValue <= 0)
        {
            result.AddError(name, "targetValue 必须大于 0。");
        }

        if (rewardAmount <= 0)
        {
            result.AddError(name, "rewardAmount 必须大于 0。");
        }
    }
}
