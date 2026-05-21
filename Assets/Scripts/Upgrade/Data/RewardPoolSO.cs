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

    public int ChoiceCount => Mathf.Clamp(choiceCount, 1, 5);
    public int MinWave => Mathf.Max(1, minWave);
    public int MaxWave => maxWave;
    public IReadOnlyList<RewardPoolEntryConfig> Entries => entries;
    public IReadOnlyList<string> BlockedMutualGroups => blockedMutualGroups;

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

    public UpgradeOptionSO Option => option;
    public int WeightOverride => weightOverride;

    public int ResolveWeight()
    {
        if (option == null)
        {
            return 0;
        }

        return weightOverride > 0 ? weightOverride : option.Weight;
    }
}
