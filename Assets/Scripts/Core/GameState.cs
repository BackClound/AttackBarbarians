/// <summary>
/// 游戏流程状态枚举，由 <see cref="GameStateMachine"/> 维护，<see cref="GameManager"/> 对外暴露。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>典型流转：</b>Bootstrapping → MainMenu → Loading → Playing ↔ Paused；</para>
/// <para>Playing → WaveTransition → UpgradeChoosing → Playing；Playing → GameOver。</para>
/// </remarks>
public enum GameState
{
    Bootstrapping,
    MainMenu,
    Loading,
    Playing,
    Paused,
    WaveTransition,
    UpgradeChoosing,
    GameOver,
    Restarting,
    Exiting
}
