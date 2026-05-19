/// <summary>
/// 单例生命周期配置：跨场景、重复实例策略、是否注册到 <see cref="ServiceLocator"/>。
/// </summary>
/// <remarks>纯数据结构，无需挂载。由 <see cref="MonoSingleton{T}"/> 或 <see cref="SingletonHost{T}"/> 消费。</remarks>
public struct SingletonOptions
{
    public bool PersistAcrossScenes;
    public SingletonDuplicatePolicy DuplicatePolicy;
    public bool RegisterWithServiceLocator;

    public static SingletonOptions SceneDefault => new SingletonOptions
    {
        PersistAcrossScenes = false,
        DuplicatePolicy = SingletonDuplicatePolicy.DestroyNewest,
        RegisterWithServiceLocator = false
    };

    public static SingletonOptions PersistentDefault => new SingletonOptions
    {
        PersistAcrossScenes = true,
        DuplicatePolicy = SingletonDuplicatePolicy.DestroyNewest,
        RegisterWithServiceLocator = false
    };
}
