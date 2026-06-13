using System.Collections.Generic;

/// <summary>
/// 三选一候选列表事件负载。
/// </summary>
public sealed class UpgradeChoicesPayload
{
    private readonly List<UpgradeOptionSO> choices = new List<UpgradeOptionSO>(5);

    /// <summary>升级抽取上下文。</summary>
    public UpgradeSelectionContext Context { get; }
    /// <summary>使用的奖励池配置 ID。</summary>
    public string PoolConfigId { get; }
    /// <summary>当前候选升级选项列表。</summary>
    public IReadOnlyList<UpgradeOptionSO> Choices => choices;

    /// <summary>创建三选一候选负载。</summary>
    /// <param name="context">升级抽取上下文。</param>
    /// <param name="poolConfigId">奖励池配置 ID。</param>
    public UpgradeChoicesPayload(UpgradeSelectionContext context, string poolConfigId)
    {
        Context = context;
        PoolConfigId = poolConfigId ?? string.Empty;
    }

    /// <summary>设置候选升级选项列表。</summary>
    /// <param name="source">源选项列表。</param>
    public void SetChoices(IReadOnlyList<UpgradeOptionSO> source)
    {
        choices.Clear();
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                choices.Add(source[i]);
            }
        }
    }
}

/// <summary>玩家确认升级选项后的事件负载。</summary>
public readonly struct UpgradeChoiceAppliedPayload
{
    /// <summary>已选升级选项配置 ID。</summary>
    public string OptionConfigId { get; }
    /// <summary>选中后的叠加层数。</summary>
    public int NewStackCount { get; }
    /// <summary>触发升级选择的来源。</summary>
    public UpgradeTriggerSource TriggerSource { get; }

    /// <summary>创建升级确认事件负载。</summary>
    /// <param name="optionConfigId">升级选项配置 ID。</param>
    /// <param name="newStackCount">新的叠加层数。</param>
    /// <param name="triggerSource">触发来源。</param>
    public UpgradeChoiceAppliedPayload(string optionConfigId, int newStackCount, UpgradeTriggerSource triggerSource)
    {
        OptionConfigId = optionConfigId ?? string.Empty;
        NewStackCount = newStackCount;
        TriggerSource = triggerSource;
    }
}
