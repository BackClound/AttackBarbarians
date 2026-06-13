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
    /// <summary>当前单例实例；未 Awake 前可能为 null。</summary>
    public static T Instance => SingletonHost<T>.Instance;

    /// <summary>是否已有有效实例。</summary>
    public static bool HasInstance => SingletonHost<T>.HasInstance;

    /// <summary>尝试获取实例而不抛异常。</summary>
    /// <param name="value">输出实例。</param>
    /// <returns>存在时返回 true。</returns>
    public static bool TryGet(out T value) => SingletonHost<T>.TryGet(out value);

    [SerializeField] private SingletonOptions singletonOptions = SingletonOptions.SceneDefault;

    /// <summary>子类可覆写单例选项；默认读 Inspector 序列化字段。</summary>
    protected virtual SingletonOptions Options => singletonOptions;

    /// <summary>Unity Awake：认领单例并调用 <see cref="OnSingletonAwake"/>。</summary>
    protected virtual void Awake()
    {
        if (!SingletonHost<T>.TryClaim((T)this, this, Options, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        OnSingletonAwake();
    }

    /// <summary>Unity OnDestroy：释放单例并调用 <see cref="OnSingletonDestroy"/>。</summary>
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
