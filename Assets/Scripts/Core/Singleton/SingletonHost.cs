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

    public static T Instance => instance;

    public static bool HasInstance => instance != null;

    public static bool TryGet(out T value)
    {
        value = instance;
        return value != null;
    }

    public static bool IsOwner(T candidate)
    {
        return !ReferenceEquals(candidate, null) && ReferenceEquals(instance, candidate);
    }

    /// <summary>
    /// 尝试将 <paramref name="candidate"/> 注册为当前单例。
    /// </summary>
    /// <param name="owner">用于 <c>DontDestroyOnLoad</c> 与销毁重复实例的 MonoBehaviour（通常为 candidate 自身）。</param>
    /// <param name="destroyedOwner">为 true 时表示本物体已被销毁，调用方应中止后续初始化。</param>
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
