using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级卡发放成功事件负载。
/// </summary>
public readonly struct UpgradeCardGrantedEventArgs
{
    /// <summary>奖励来源。</summary>
    public UpgradeCardRewardSource Source { get; }
    /// <summary>发放条目列表。</summary>
    public IReadOnlyList<UpgradeCardGrantEntry> Grants { get; }

    /// <summary>创建升级卡发放事件负载。</summary>
    /// <param name="source">奖励来源。</param>
    /// <param name="grants">发放条目列表。</param>
    public UpgradeCardGrantedEventArgs(UpgradeCardRewardSource source, IReadOnlyList<UpgradeCardGrantEntry> grants)
    {
        Source = source;
        Grants = grants;
    }
}

/// <summary>单条升级卡发放记录。</summary>
public readonly struct UpgradeCardGrantEntry
{
    /// <summary>卡片配置 ID。</summary>
    public string CardConfigId { get; }
    /// <summary>展示名称。</summary>
    public string DisplayName { get; }
    /// <summary>发放数量。</summary>
    public int Count { get; }

    /// <summary>创建单条发放记录。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <param name="displayName">展示名称。</param>
    /// <param name="count">发放数量。</param>
    public UpgradeCardGrantEntry(string cardConfigId, string displayName, int count)
    {
        CardConfigId = cardConfigId ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Count = Mathf.Max(1, count);
    }
}
