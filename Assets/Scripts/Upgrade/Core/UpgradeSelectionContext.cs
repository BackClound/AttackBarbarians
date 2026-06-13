/// <summary>
/// 升级抽取时的运行时上下文（波次、等级、已选记录）。
/// </summary>
public readonly struct UpgradeSelectionContext
{
    /// <summary>当前波次索引。</summary>
    public int WaveIndex { get; }
    /// <summary>当前玩家等级。</summary>
    public int PlayerLevel { get; }
    /// <summary>触发升级选择的来源。</summary>
    public UpgradeTriggerSource TriggerSource { get; }

    /// <summary>创建升级抽取上下文。</summary>
    /// <param name="waveIndex">当前波次索引。</param>
    /// <param name="playerLevel">当前玩家等级。</param>
    /// <param name="triggerSource">触发来源。</param>
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
    /// <summary>波次完成。</summary>
    WaveComplete = 0,
    /// <summary>玩家升级。</summary>
    LevelUp = 1,
    /// <summary>调试入口。</summary>
    Debug = 2,
}
