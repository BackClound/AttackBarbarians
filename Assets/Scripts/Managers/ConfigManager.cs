using UnityEngine;

/// <summary>
/// 全局配置加载器，负责在启动时提供 <see cref="GameConfig"/> 及运行时日志开关。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b>挂在 <c>GameSystems</c> 根物体上，或与 <see cref="GameBootstrapper"/> 同层级子物体。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、子弹 Prefab。</para>
/// <para><b>Inspector 配置：</b>将 <c>Assets/Resources/Config/GameConfig.asset</c> 拖入 <c>Game Config</c> 槽位；留空时从 Resources 路径 <see cref="GameConstants.ResourcePaths.GameConfig"/> 自动加载。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ConfigManager&gt;()</c>（须在 Bootstrap 之后）。</para>
/// </remarks>
public class ConfigManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private GameConfig gameConfig;

    public bool IsInitialized { get; private set; }
    public GameConfig GameConfig => gameConfig;

    public void Initialize()
    {
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>(GameConstants.ResourcePaths.GameConfig);
        }

        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        IsInitialized = false;
    }

    public bool ShouldLog()
    {
        return gameConfig != null && gameConfig.EnableRuntimeLogs;
    }
}
