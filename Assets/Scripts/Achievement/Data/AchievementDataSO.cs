using UnityEngine;

/// <summary>
/// Meta 单条成就配置：定义目标条件、完成阈值与奖励内容。
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

    /// <summary>成就描述文本。</summary>
    public string Description => description;
    /// <summary>成就目标类型。</summary>
    public AchievementTargetType TargetType => targetType;
    /// <summary>达成目标所需的进度值。</summary>
    public int TargetValue => Mathf.Max(1, targetValue);
    /// <summary>完成成就后发放的奖励类型。</summary>
    public ShopRewardType RewardType => rewardType;
    /// <summary>完成成就后发放的奖励数量。</summary>
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);

    /// <summary>
    /// 收集成就配置校验错误。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
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
