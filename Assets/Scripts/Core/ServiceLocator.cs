using System;
using System.Collections.Generic;

/// <summary>
/// 轻量服务定位器，在 Bootstrap 后提供 Manager 与 EventBus 的统一访问入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。纯静态类，无 MonoBehaviour。</para>
/// <para><b>注册时机：</b>仅由 <see cref="GameBootstrapper"/> 在 Bootstrap 流程中调用 <c>Register</c>；场景销毁时 <c>Clear</c>。</para>
/// <para><b>使用方式：</b>业务代码使用 <c>ServiceLocator.Get&lt;T&gt;()</c> 或 <c>TryGet&lt;T&gt;(out var service)</c>，避免 <c>FindObjectOfType</c> 与散落单例。</para>
/// <para><b>注意：</b>Bootstrap 完成前调用 <c>Get</c> 会抛异常；可选依赖请用 <c>TryGet</c>。</para>
/// </remarks>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(32);

    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        Services[typeof(T)] = service;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (Services.TryGetValue(typeof(T), out object value))
        {
            service = value as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public static T Get<T>() where T : class
    {
        if (TryGet(out T service))
        {
            return service;
        }

        throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
    }

    public static void Unregister<T>() where T : class
    {
        Services.Remove(typeof(T));
    }

    public static void Clear()
    {
        Services.Clear();
    }
}
