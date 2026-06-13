/// <summary>
/// 音频播放通道，用于音量分组与并发限制策略。
/// </summary>
public enum AudioChannel
{
    /// <summary>背景音乐通道。</summary>
    Bgm = 0,

    /// <summary>音效通道。</summary>
    Sfx = 1,

    /// <summary>界面交互音效通道。</summary>
    Ui = 2,

    /// <summary>环境氛围音通道。</summary>
    Ambient = 3,

    /// <summary>Boss 战专用音乐通道。</summary>
    Boss = 4,
}
