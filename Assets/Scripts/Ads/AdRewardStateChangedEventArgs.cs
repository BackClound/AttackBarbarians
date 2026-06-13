/// <summary>
/// 广告服务展示状态变化（用于 UI 禁用按钮）。
/// </summary>
public readonly struct AdRewardStateChangedEventArgs
{
    /// <summary>当前是否正在展示广告。</summary>
    public bool IsShowing { get; }

    /// <summary>
    /// 创建广告展示状态变化事件参数。
    /// </summary>
    /// <param name="isShowing">是否正在展示广告。</param>
    public AdRewardStateChangedEventArgs(bool isShowing)
    {
        IsShowing = isShowing;
    }
}
