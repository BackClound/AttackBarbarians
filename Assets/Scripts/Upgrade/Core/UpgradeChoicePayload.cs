using System.Collections.Generic;

/// <summary>
/// 三选一候选列表事件负载。
/// </summary>
public sealed class UpgradeChoicesPayload
{
    private readonly List<UpgradeOptionSO> choices = new List<UpgradeOptionSO>(5);

    public UpgradeSelectionContext Context { get; }
    public string PoolConfigId { get; }
    public IReadOnlyList<UpgradeOptionSO> Choices => choices;

    public UpgradeChoicesPayload(UpgradeSelectionContext context, string poolConfigId)
    {
        Context = context;
        PoolConfigId = poolConfigId ?? string.Empty;
    }

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
    public string OptionConfigId { get; }
    public int NewStackCount { get; }
    public UpgradeTriggerSource TriggerSource { get; }

    public UpgradeChoiceAppliedPayload(string optionConfigId, int newStackCount, UpgradeTriggerSource triggerSource)
    {
        OptionConfigId = optionConfigId ?? string.Empty;
        NewStackCount = newStackCount;
        TriggerSource = triggerSource;
    }
}
