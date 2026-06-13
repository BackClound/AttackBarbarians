using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励池：候选升级列表、波次范围、权重覆盖与互斥组规则。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// </remarks>
[CreateAssetMenu(fileName = "RewardPool", menuName = "Attack Barbarians/Config/Reward Pool")]
public class RewardPoolSO : ConfigDataBase
{
    [Header("Pool")]
    [SerializeField] private int choiceCount = 3;
    [SerializeField] private int minWave = 1;
    [SerializeField] private int maxWave;
    [SerializeField] private List<RewardPoolEntryConfig> entries = new List<RewardPoolEntryConfig>(16);
    [SerializeField] private List<string> blockedMutualGroups = new List<string>(4);

    /// <summary>每次展示的候选数量（1–5）。</summary>
    public int ChoiceCount => Mathf.Clamp(choiceCount, 1, 5);
    /// <summary>奖励池生效的最小波次。</summary>
    public int MinWave => Mathf.Max(1, minWave);
    /// <summary>奖励池生效的最大波次（0 表示无上限）。</summary>
    public int MaxWave => maxWave;
    /// <summary>奖励池条目列表。</summary>
    public IReadOnlyList<RewardPoolEntryConfig> Entries => entries;
    /// <summary>与已选互斥组冲突时需屏蔽的互斥组 ID 列表。</summary>
    public IReadOnlyList<string> BlockedMutualGroups => blockedMutualGroups;

    /// <summary>收集奖励池配置校验错误。</summary>
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
            RewardPoolEntryConfig entry = entries[i];
            if (entry == null || entry.Option == null)
            {
                result.AddError(name, $"entries[{i}] 或 Option 为空。");
            }
        }
    }
}

/// <summary>奖励池单条：选项引用与可选权重覆盖。</summary>
[System.Serializable]
public class RewardPoolEntryConfig
{
    [SerializeField] private UpgradeOptionSO option;
    [SerializeField] private int weightOverride = -1;

    /// <summary>关联的升级选项。</summary>
    public UpgradeOptionSO Option => option;
    /// <summary>权重覆盖值（≤0 时使用选项默认权重）。</summary>
    public int WeightOverride => weightOverride;

    /// <summary>解析最终用于抽取的有效权重。</summary>
    /// <returns>有效权重；选项为空时返回 0。</returns>
    public int ResolveWeight()
    {
        if (option == null)
        {
            return 0;
        }

        return weightOverride > 0 ? weightOverride : option.Weight;
    }
}
