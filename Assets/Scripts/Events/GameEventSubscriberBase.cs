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

    protected virtual void OnEnable()
    {
        TrySubscribe();
    }

    protected virtual void Start()
    {
        TrySubscribe();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeAll();
    }

    protected abstract void RegisterHandlers(EventBus bus);

    protected abstract void UnregisterHandlers(EventBus bus);

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

        RegisterHandlers(bus);
        isSubscribed = true;
    }

    private void UnsubscribeAll()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (ServiceLocator.TryGet(out EventBus bus))
        {
            UnregisterHandlers(bus);
        }

        isSubscribed = false;
    }
}
