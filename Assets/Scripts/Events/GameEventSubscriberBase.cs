using UnityEngine;

/// <summary>
/// MonoBehaviour 事件订阅基类：在 OnEnable/Start 订阅，OnDisable 取消订阅。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在需要监听 <see cref="GameEvents"/> 的 UI 或表现物体上。</para>
/// <para><b>用法：</b>继承本类并实现 <see cref="RegisterHandlers"/> / <see cref="UnregisterHandlers"/>，在方法内调用 <c>GameEvents.SubscribeXxx</c> 或使用传入的 <see cref="EventBus"/>。</para>
/// </remarks>
public abstract class GameEventSubscriberBase : MonoBehaviour
{
    private bool isSubscribed;

    /// <summary>启用时尝试订阅（Bootstrap 后 EventBus 可用）。</summary>
    protected virtual void OnEnable()
    {
        TrySubscribe();
    }

    /// <summary>Start 时再次尝试订阅，覆盖 EventBus 晚于 OnEnable 就绪的情况。</summary>
    protected virtual void Start()
    {
        TrySubscribe();
    }

    /// <summary>禁用时取消全部订阅。</summary>
    protected virtual void OnDisable()
    {
        UnsubscribeAll();
    }

    /// <summary>子类注册具体事件 handler。</summary>
    protected abstract void RegisterHandlers();

    /// <summary>子类取消先前注册的 handler。</summary>
    protected abstract void UnregisterHandlers();

    /// <summary>在 EventBus 就绪后执行一次性订阅。</summary>
    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out EventBus bus))
        {
            return;
        }

        RegisterHandlers();
        isSubscribed = true;
    }

    /// <summary>取消订阅并重置标志。</summary>
    private void UnsubscribeAll()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (ServiceLocator.TryGet(out EventBus bus))
        {
            UnregisterHandlers();
        }

        isSubscribed = false;
    }
}
