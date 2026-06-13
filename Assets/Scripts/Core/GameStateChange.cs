/// <summary>
/// 游戏状态切换事件负载，通过 <see cref="GameConstants.EventKeys.GameStateChanged"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct GameStateChange
{
    /// <summary>切换前的状态。</summary>
    public GameState OldState { get; }

    /// <summary>切换后的状态。</summary>
    public GameState NewState { get; }

    /// <summary>构造状态变更记录。</summary>
    /// <param name="oldState">原状态。</param>
    /// <param name="newState">新状态。</param>
    public GameStateChange(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    /// <summary>返回可读的状态流转字符串。</summary>
    /// <returns>形如 <c>Playing -&gt; Paused</c> 的文本。</returns>
    public override string ToString() => $"{OldState} -> {NewState}";
}
