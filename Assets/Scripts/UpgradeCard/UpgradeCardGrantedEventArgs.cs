using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级卡发放成功事件负载。
/// </summary>
public readonly struct UpgradeCardGrantedEventArgs
{
    public UpgradeCardRewardSource Source { get; }
    public IReadOnlyList<UpgradeCardGrantEntry> Grants { get; }

    public UpgradeCardGrantedEventArgs(UpgradeCardRewardSource source, IReadOnlyList<UpgradeCardGrantEntry> grants)
    {
        Source = source;
        Grants = grants;
    }
}

/// <summary>单条升级卡发放记录。</summary>
public readonly struct UpgradeCardGrantEntry
{
    public string CardConfigId { get; }
    public string DisplayName { get; }
    public int Count { get; }

    public UpgradeCardGrantEntry(string cardConfigId, string displayName, int count)
    {
        CardConfigId = cardConfigId ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Count = Mathf.Max(1, count);
    }
}
