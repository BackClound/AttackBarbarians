using System;
using System.Collections.Generic;

/// <summary>
/// 事件上下文，包含发送者与可选负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。由 <see cref="EventBus.Publish"/> 构造并传递给订阅者。</remarks>
public struct GameEventContext
{
    public object Sender { get; }
    public object Payload { get; }

    public GameEventContext(object sender, object payload)
    {
        Sender = sender;
        Payload = payload;
    }
}

/// <summary>
/// 全局事件总线，用于跨模块解耦通信（游戏状态、波次、伤害等）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="GameBootstrapper"/> 在代码中 <c>new EventBus()</c> 创建并注册到 <see cref="ServiceLocator"/>。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;EventBus&gt;()</c>（Bootstrap 之后）。</para>
/// <para><b>使用方式：</b>订阅 <c>Subscribe(GameConstants.EventKeys.xxx, handler)</c>；在 OnDestroy 或 Shutdown 时 <c>Unsubscribe</c>；发布 <c>Publish(key, sender, payload)</c>。</para>
/// <para><b>事件 Key：</b>使用 <see cref="GameConstants.EventKeys"/> 中的常量，避免魔法字符串。</para>
/// </remarks>
public sealed class EventBus : IGameSystem
{
    private readonly Dictionary<string, Action<GameEventContext>> listeners =
        new Dictionary<string, Action<GameEventContext>>(64);

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        listeners.Clear();
        IsInitialized = false;
    }

    public void Subscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            throw new ArgumentException("Event key is null or empty.", nameof(eventKey));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        listeners.TryGetValue(eventKey, out Action<GameEventContext> current);
        listeners[eventKey] = current + handler;
    }

    public void Unsubscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (string.IsNullOrEmpty(eventKey) || handler == null)
        {
            return;
        }

        if (!listeners.TryGetValue(eventKey, out Action<GameEventContext> current))
        {
            return;
        }

        current -= handler;
        if (current == null)
        {
            listeners.Remove(eventKey);
            return;
        }

        listeners[eventKey] = current;
    }

    public void Publish(string eventKey, object sender = null, object payload = null)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            return;
        }

        if (listeners.TryGetValue(eventKey, out Action<GameEventContext> handler))
        {
            handler.Invoke(new GameEventContext(sender, payload));
        }
    }
}
