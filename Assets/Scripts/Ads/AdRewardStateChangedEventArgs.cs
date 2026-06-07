/// <summary>
/// 广告服务展示状态变化（用于 UI 禁用按钮）。
/// </summary>
public readonly struct AdRewardStateChangedEventArgs
{
    public bool IsShowing { get; }

    public AdRewardStateChangedEventArgs(bool isShowing)
    {
        IsShowing = isShowing;
    }
}
