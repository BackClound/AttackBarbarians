/// <summary>
/// 可纳入 <see cref="GameBootstrapper"/> 生命周期管理的系统接口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（接口）。实现类若为 MonoBehaviour 则需按各自文档挂载。</para>
/// <para><b>实现者：</b><see cref="ConfigManager"/>、<see cref="SaveManager"/>、<see cref="ResourceManager"/>、<see cref="ShopManager"/>、<see cref="PoolManager"/>、<see cref="GameManager"/>、<see cref="GameFlowManager"/>、<see cref="AudioManager"/>、<see cref="EventBus"/>。</para>
/// <para><b>调用顺序：</b>由 Bootstrapper 依次 <c>Initialize</c> → 每帧 <c>Tick</c> → 销毁时 <c>Shutdown</c>。</para>
/// </remarks>
public interface IGameSystem
{
    /// <summary>是否已完成 <see cref="Initialize"/>。</summary>
    bool IsInitialized { get; }

    /// <summary>Bootstrap 阶段的一次性初始化。</summary>
    void Initialize();

    /// <summary>每帧更新（由 <see cref="GameBootstrapper"/> 统一驱动）。</summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    void Tick(float deltaTime);

    /// <summary>系统关闭与资源释放。</summary>
    void Shutdown();
}
