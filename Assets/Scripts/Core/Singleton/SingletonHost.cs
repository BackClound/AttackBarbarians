using UnityEngine;

/// <summary>
/// 通用单例宿主：供无法继承 <see cref="MonoSingleton{T}"/> 的类（如 <see cref="Entity"/> 子类）在 Awake/OnDestroy 中复用同一套去重与释放逻辑。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态泛型类。</para>
/// <para><b>使用方式：</b>在 Awake 调用 <see cref="TryClaim"/>，在 OnDestroy 调用 <see cref="Release"/>；访问 <see cref="Instance"/> / <see cref="TryGet"/>。</para>
/// <para><b>禁止：</b>默认不使用 <c>FindObjectOfType</c> 懒查找；实例必须在场景中显式存在并由 Awake 注册。</para>
/// </remarks>
public static class SingletonHost<T> where T : class
{
    private static T instance;
    private static bool registeredWithServiceLocator;

    /// <summary>当前单例实例。</summary>
    public static T Instance => instance;

    /// <summary>是否已注册实例。</summary>
    public static bool HasInstance => instance != null;

    /// <summary>尝试获取实例。</summary>
    /// <param name="value">输出实例。</param>
    /// <returns>存在时返回 true。</returns>
    public static bool TryGet(out T value)
    {
        value = instance;
        return value != null;
    }

    /// <summary>判断 candidate 是否为当前持有实例。</summary>
    /// <param name="candidate">待检查的候选实例。</param>
    /// <returns>是 Owner 时返回 true。</returns>
    public static bool IsOwner(T candidate)
    {
        return !ReferenceEquals(candidate, null) && ReferenceEquals(instance, candidate);
    }

    /// <summary>
    /// 尝试将 <paramref name="candidate"/> 注册为当前单例。
    /// </summary>
    /// <param name="candidate">候选实例。</param>
    /// <param name="owner">用于 <c>DontDestroyOnLoad</c> 与销毁重复实例的 MonoBehaviour（通常为 candidate 自身）。</param>
    /// <param name="options">单例生命周期选项。</param>
    /// <param name="destroyedOwner">为 true 时表示本物体已被销毁，调用方应中止后续初始化。</param>
    /// <returns>认领成功时返回 true。</returns>
    public static bool TryClaim(T candidate, MonoBehaviour owner, SingletonOptions options, out bool destroyedOwner)
    {
        destroyedOwner = false;

        if (ReferenceEquals(candidate, null))
        {
            return false;
        }

        if (instance != null && !ReferenceEquals(instance, candidate))
        {
            HandleDuplicate(candidate, owner, options, out destroyedOwner);
            return false;
        }

        instance = candidate;

        if (options.PersistAcrossScenes && owner != null)
        {
            Object.DontDestroyOnLoad(owner.gameObject);
        }

        if (options.RegisterWithServiceLocator)
        {
            ServiceLocator.Register(candidate);
            registeredWithServiceLocator = true;
        }

        return true;
    }

    /// <summary>释放单例；仅 Owner 调用有效。</summary>
    /// <param name="candidate">待释放的实例。</param>
    public static void Release(T candidate)
    {
        if (!ReferenceEquals(instance, candidate))
        {
            return;
        }

        if (registeredWithServiceLocator)
        {
            ServiceLocator.Unregister<T>();
            registeredWithServiceLocator = false;
        }

        instance = null;
    }

    /// <summary>按 DuplicatePolicy 处理重复实例。</summary>
    /// <param name="candidate">新实例。</param>
    /// <param name="owner">新实例的 MonoBehaviour 宿主。</param>
    /// <param name="options">单例选项。</param>
    /// <param name="destroyedOwner">新实例被销毁时为 true。</param>
    private static void HandleDuplicate(T candidate, MonoBehaviour owner, SingletonOptions options, out bool destroyedOwner)
    {
        destroyedOwner = false;

        switch (options.DuplicatePolicy)
        {
            case SingletonDuplicatePolicy.DestroyOldest:
                if (instance is MonoBehaviour previous && previous != null)
                {
                    Object.Destroy(previous.gameObject);
                }

                instance = candidate;

                if (options.PersistAcrossScenes && owner != null)
                {
                    Object.DontDestroyOnLoad(owner.gameObject);
                }

                if (options.RegisterWithServiceLocator)
                {
                    ServiceLocator.Register(candidate);
                    registeredWithServiceLocator = true;
                }

                break;

            case SingletonDuplicatePolicy.DestroyNewest:
            default:
                if (owner != null)
                {
                    Object.Destroy(owner.gameObject);
                    destroyedOwner = true;
                }

                break;
        }
    }
}
