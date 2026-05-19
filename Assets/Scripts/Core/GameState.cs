/// <summary>
/// 游戏流程状态枚举，由 <see cref="GameManager"/> 维护并通过事件广播变更。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>典型流转：</b>Bootstrapping → Playing ↔ Paused → GameOver / Restarting → Playing。</para>
/// </remarks>
public enum GameState
{
    Bootstrapping,
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Restarting,
    Exiting
}
