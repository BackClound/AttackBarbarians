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

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }

    protected override void RegisterHandlers()
    {
        GameEvents.SubscribePlayerHealthChanged(OnPlayerHealthChanged);
    }

    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribePlayerHealthChanged(OnPlayerHealthChanged);
    }

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
