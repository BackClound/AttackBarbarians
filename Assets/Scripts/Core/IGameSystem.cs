/// <summary>
/// 可纳入 <see cref="GameBootstrapper"/> 生命周期管理的系统接口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（接口）。实现类若为 MonoBehaviour 则需按各自文档挂载。</para>
/// <para><b>实现者：</b><see cref="ConfigManager"/>、<see cref="PoolManager"/>、<see cref="GameManager"/>、<see cref="EventBus"/>。</para>
/// <para><b>调用顺序：</b>由 Bootstrapper 依次 <c>Initialize</c> → 每帧 <c>Tick</c> → 销毁时 <c>Shutdown</c>。</para>
/// </remarks>
public interface IGameSystem
{
    bool IsInitialized { get; }

    void Initialize();

    void Tick(float deltaTime);

    void Shutdown();
}
