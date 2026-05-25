/// <summary>
/// 成就进度变更事件负载。
/// </summary>
public readonly struct AchievementProgressChangedEventArgs
{
    public string ConfigId { get; }
    public int CurrentProgress { get; }
    public int TargetValue { get; }
    public bool IsCompleted { get; }

    public AchievementProgressChangedEventArgs(string configId, int currentProgress, int targetValue, bool isCompleted)
    {
        ConfigId = configId ?? string.Empty;
        CurrentProgress = currentProgress;
        TargetValue = targetValue;
        IsCompleted = isCompleted;
    }
}
