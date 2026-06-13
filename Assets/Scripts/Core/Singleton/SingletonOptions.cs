/// <summary>
/// 单例生命周期配置：跨场景、重复实例策略、是否注册到 <see cref="ServiceLocator"/>。
/// </summary>
/// <remarks>纯数据结构，无需挂载。由 <see cref="MonoSingleton{T}"/> 或 <see cref="SingletonHost{T}"/> 消费。</remarks>
public struct SingletonOptions
{
    /// <summary>是否对宿主 GameObject 调用 DontDestroyOnLoad。</summary>
    public bool PersistAcrossScenes;

    /// <summary>出现重复实例时的处理策略。</summary>
    public SingletonDuplicatePolicy DuplicatePolicy;

    /// <summary>认领成功后是否自动 Register 到 ServiceLocator。</summary>
    public bool RegisterWithServiceLocator;

    /// <summary>场景内单例默认选项：不跨场景、销毁新实例。</summary>
    public static SingletonOptions SceneDefault => new SingletonOptions
    {
        PersistAcrossScenes = false,
        DuplicatePolicy = SingletonDuplicatePolicy.DestroyNewest,
        RegisterWithServiceLocator = false
    };

    /// <summary>常驻单例默认选项：跨场景、销毁新实例。</summary>
    public static SingletonOptions PersistentDefault => new SingletonOptions
    {
        PersistAcrossScenes = true,
        DuplicatePolicy = SingletonDuplicatePolicy.DestroyNewest,
        RegisterWithServiceLocator = false
    };
}
