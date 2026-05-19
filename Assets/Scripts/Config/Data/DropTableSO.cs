using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落表：按权重随机产出资源或物品 ID。
/// </summary>
[CreateAssetMenu(fileName = "DropTable", menuName = "Attack Barbarians/Config/Drop Table")]
public class DropTableSO : ConfigDataBase
{
    [SerializeField] private List<DropEntryConfig> entries = new List<DropEntryConfig>();

    public IReadOnlyList<DropEntryConfig> Entries => entries;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (entries == null || entries.Count == 0)
        {
            result.AddWarning(name, "掉落条目为空。");
            return;
        }

        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            DropEntryConfig entry = entries[i];
            if (entry == null)
            {
                result.AddError(name, $"entries[{i}] 为空。");
                continue;
            }

            if (entry.Weight < 0)
            {
                result.AddError(name, $"entries[{i}] 权重为负数。");
            }

            if (entry.MinCount > entry.MaxCount)
            {
                result.AddError(name, $"entries[{i}] MinCount 大于 MaxCount。");
            }

            totalWeight += Mathf.Max(0, entry.Weight);
        }

        if (totalWeight <= 0)
        {
            result.AddError(name, "总权重必须大于 0。");
        }
    }
}

/// <summary>单条掉落条目。</summary>
[System.Serializable]
public class DropEntryConfig
{
    [SerializeField] private string itemConfigId;
    [SerializeField] private int weight = 1;
    [SerializeField] private int minCount = 1;
    [SerializeField] private int maxCount = 1;

    public string ItemConfigId => itemConfigId;
    public int Weight => weight;
    public int MinCount => minCount;
    public int MaxCount => maxCount;
}
