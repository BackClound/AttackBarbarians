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
    /// <summary>引导初始化中。</summary>
    Bootstrapping,
    /// <summary>主菜单。</summary>
    MainMenu,
    /// <summary>加载关卡或资源。</summary>
    Loading,
    /// <summary>正常战斗进行中。</summary>
    Playing,
    /// <summary>暂停（timeScale = 0）。</summary>
    Paused,
    /// <summary>波次间过渡。</summary>
    WaveTransition,
    /// <summary>升级三选一界面。</summary>
    UpgradeChoosing,
    /// <summary>本局失败。</summary>
    GameOver,
    /// <summary>重开流程中。</summary>
    Restarting,
    /// <summary>退出游戏。</summary>
    Exiting
}
