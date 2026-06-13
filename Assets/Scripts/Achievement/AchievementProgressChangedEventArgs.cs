/// <summary>
/// 成就进度变更事件负载。
/// </summary>
public readonly struct AchievementProgressChangedEventArgs
{
    /// <summary>成就配置 ID。</summary>
    public string ConfigId { get; }
    /// <summary>当前进度值。</summary>
    public int CurrentProgress { get; }
    /// <summary>目标进度值。</summary>
    public int TargetValue { get; }
    /// <summary>是否已完成。</summary>
    public bool IsCompleted { get; }

    /// <summary>
    /// 创建成就进度变更事件负载。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="currentProgress">当前进度值。</param>
    /// <param name="targetValue">目标进度值。</param>
    /// <param name="isCompleted">是否已完成。</param>
    public AchievementProgressChangedEventArgs(string configId, int currentProgress, int targetValue, bool isCompleted)
    {
        ConfigId = configId ?? string.Empty;
        CurrentProgress = currentProgress;
        TargetValue = targetValue;
        IsCompleted = isCompleted;
    }
}
