/// <summary>
/// 升级抽取时的运行时上下文（波次、等级、已选记录）。
/// </summary>
public readonly struct UpgradeSelectionContext
{
    public int WaveIndex { get; }
    public int PlayerLevel { get; }
    public UpgradeTriggerSource TriggerSource { get; }

    public UpgradeSelectionContext(int waveIndex, int playerLevel, UpgradeTriggerSource triggerSource)
    {
        WaveIndex = waveIndex;
        PlayerLevel = playerLevel;
        TriggerSource = triggerSource;
    }
}

/// <summary>触发三选一升级的来源。</summary>
public enum UpgradeTriggerSource
{
    WaveComplete = 0,
    LevelUp = 1,
    Debug = 2,
}
