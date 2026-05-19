using UnityEngine;

/// <summary>
/// 场景内 MonoBehaviour 单例基类：Awake 去重、OnDestroy 释放，提供 <see cref="Instance"/> / <see cref="TryGet"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（抽象基类）。由具体 Manager 继承并挂在场景物体上。</para>
/// <para><b>适用：</b><see cref="GameManager"/> 等不继承 <see cref="Entity"/> 的 MonoBehaviour。</para>
/// <para><b>不适用：</b><see cref="Player"/> 等已有基类层次时，请用 <see cref="SingletonHost{T}"/>。</para>
/// <para><b>获取方式：</b>优先 Bootstrap 后的 <c>ServiceLocator.Get&lt;T&gt;()</c>；其次 <see cref="Instance"/>。</para>
/// </remarks>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance => SingletonHost<T>.Instance;

    public static bool HasInstance => SingletonHost<T>.HasInstance;

    public static bool TryGet(out T value) => SingletonHost<T>.TryGet(out value);

    [SerializeField] private SingletonOptions singletonOptions = SingletonOptions.SceneDefault;

    protected virtual SingletonOptions Options => singletonOptions;

    protected virtual void Awake()
    {
        if (!SingletonHost<T>.TryClaim((T)this, this, Options, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        OnSingletonAwake();
    }

    protected virtual void OnDestroy()
    {
        if (!SingletonHost<T>.IsOwner((T)this))
        {
            return;
        }

        OnSingletonDestroy();
        SingletonHost<T>.Release((T)this);
    }

    /// <summary>单例认领成功后的初始化入口（替代子类直接写 Awake 去重逻辑）。</summary>
    protected virtual void OnSingletonAwake() { }

    /// <summary>单例释放前的清理入口。</summary>
    protected virtual void OnSingletonDestroy() { }
}
