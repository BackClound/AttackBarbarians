using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级卡奖励池：按权重随机抽取升级卡。
/// </summary>
/// <remarks>
/// <para><b>路径：</b><c>Assets/Resources/Config/UpgradeCard/Pools/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "UpgradeCardRewardPool", menuName = "Attack Barbarians/Config/Upgrade Card Reward Pool")]
public class UpgradeCardRewardPoolSO : ConfigDataBase
{
    [Header("Pool")]
    [SerializeField] private int drawCount = 1;
    [SerializeField] private List<UpgradeCardPoolEntryConfig> entries = new List<UpgradeCardPoolEntryConfig>(16);

    /// <summary>每次抽取的卡片数量。</summary>
    public int DrawCount => Mathf.Max(1, drawCount);
    /// <summary>奖池条目列表。</summary>
    public IReadOnlyList<UpgradeCardPoolEntryConfig> Entries => entries;

    /// <summary>收集升级卡奖励池配置校验错误。</summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (entries == null || entries.Count == 0)
        {
            result.AddError(name, "entries 不能为空。");
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UpgradeCardPoolEntryConfig entry = entries[i];
            if (entry == null || entry.Card == null)
            {
                result.AddError(name, $"entries[{i}] 或 Card 为空。");
            }
        }
    }
}

/// <summary>升级卡池单条：卡片引用与权重。</summary>
[System.Serializable]
public class UpgradeCardPoolEntryConfig
{
    [SerializeField] private UpgradeCardSO card;
    [SerializeField] private int weight = 100;

    /// <summary>关联的升级卡。</summary>
    public UpgradeCardSO Card => card;
    /// <summary>抽取权重。</summary>
    public int Weight => Mathf.Max(1, weight);
}
