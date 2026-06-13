using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通过 <see cref="GameEvents"/> 刷新玩家血条，不依赖 <see cref="Player"/> 单例。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在含 <see cref="Slider"/> 的血条 UI 物体上。</para>
/// <para><b>推荐：</b>与 Player 子物体血条 Slider 同物体；可替代 <see cref="Player_Health"/> 内直接改 Slider 的逻辑。</para>
/// </remarks>
[RequireComponent(typeof(Slider))]
public class PlayerHealthBarView : GameEventSubscriberBase
{
    [SerializeField] private Slider healthSlider;

    /// <summary>自动绑定 Slider 组件。</summary>
    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }

    /// <summary>订阅玩家生命值变化事件。</summary>
    protected override void RegisterHandlers()
    {
        GameEvents.SubscribePlayerHealthChanged(OnPlayerHealthChanged);
    }

    /// <summary>取消订阅玩家生命值变化事件。</summary>
    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribePlayerHealthChanged(OnPlayerHealthChanged);
    }

    /// <summary>根据事件参数更新血条填充比例。</summary>
    private void OnPlayerHealthChanged(GameEventContext context)
    {
        if (context.Payload is not PlayerHealthEventArgs args || healthSlider == null)
        {
            return;
        }

        float maxHp = args.MaxHp > 0f ? args.MaxHp : 1f;
        healthSlider.value = Mathf.Clamp01(args.CurrentHp / maxHp);
    }
}
