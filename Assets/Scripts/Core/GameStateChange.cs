/// <summary>
/// 游戏状态切换事件负载，通过 <see cref="GameConstants.EventKeys.GameStateChanged"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct GameStateChange
{
    public GameState OldState { get; }
    public GameState NewState { get; }

    public GameStateChange(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    public override string ToString() => $"{OldState} -> {NewState}";
}
