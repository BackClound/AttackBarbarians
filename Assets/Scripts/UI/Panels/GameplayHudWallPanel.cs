using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD 城墙区：生命条、生命值、护甲、攻击力数值。
/// </summary>
public class GameplayHudWallPanel : MonoBehaviour
{
    private static readonly Color WallHpGreen = new Color(0.18f, 0.8f, 0.44f, 1f);

    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthValueText;

    [Header("Stats")]
    [SerializeField] private TMP_Text armorValueText;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text playerNameText;

    [Header("Fallback")]
    [SerializeField] private string playerNameFallback = "守卫者";

    /// <summary>应用城墙血条配色。</summary>
    private void Awake()
    {
        if (healthFill != null)
        {
            healthFill.color = WallHpGreen;
        }
    }

    /// <summary>订阅生命与属性事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribePlayerHealthChanged(OnPlayerHealthChanged);
        GameEvents.SubscribePlayerStatsChanged(OnPlayerStatsChanged);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        RefreshAll();
    }

    /// <summary>取消事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribePlayerHealthChanged(OnPlayerHealthChanged);
        GameEvents.UnsubscribePlayerStatsChanged(OnPlayerStatsChanged);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
    }

    private void OnGameStarted(GameEventContext ctx) => RefreshAll();

    private void OnPlayerHealthChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not PlayerHealthEventArgs args)
        {
            return;
        }

        ApplyHealth(args.CurrentHp, args.MaxHp);
    }

    private void OnPlayerStatsChanged(GameEventContext ctx)
    {
        if (ctx.Payload is PlayerStatsChangedEventArgs args)
        {
            ApplyArmor(args.Snapshot.Get(StatType.Armor));
            ApplyAttack(args.Snapshot.Get(StatType.Damage));
        }
    }

    /// <summary>全量刷新城墙区数值。</summary>
    public void RefreshAll()
    {
        RefreshHealthFromPlayer();
        RefreshStatsFromPlayer();
        RefreshPlayerName();
    }

    private void RefreshHealthFromPlayer()
    {
        if (!PlayerSceneAccess.TryGetHealth(out Player_Health health))
        {
            return;
        }

        float maxHp = health.MaxHp > 0f ? health.MaxHp : 1f;
        ApplyHealth(health.CurrentHp, maxHp);
    }

    private void RefreshStatsFromPlayer()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller) || !controller.IsReady)
        {
            return;
        }

        StatRuntimeSnapshot snapshot = controller.RuntimeStats.Snapshot;
        ApplyArmor(snapshot.Get(StatType.Armor));
        ApplyAttack(snapshot.Get(StatType.Damage));
    }

    private void RefreshPlayerName()
    {
        if (playerNameText == null)
        {
            return;
        }

        playerNameText.text = playerNameFallback;
        playerNameText.color = UiTechWastelandPalette.TextPrimary;
    }

    private void ApplyHealth(float currentHp, float maxHp)
    {
        maxHp = maxHp > 0f ? maxHp : 1f;
        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (healthSlider != null)
        {
            healthSlider.value = ratio;
        }

        if (healthValueText != null)
        {
            healthValueText.text = Mathf.CeilToInt(currentHp).ToString();
            healthValueText.color = ratio < 0.25f
                ? UiTechWastelandPalette.DangerRed
                : UiTechWastelandPalette.TextPrimary;
        }
    }

    private void ApplyArmor(float armor)
    {
        if (armorValueText != null)
        {
            armorValueText.text = Mathf.CeilToInt(armor).ToString();
            armorValueText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

    private void ApplyAttack(float attack)
    {
        if (attackValueText != null)
        {
            attackValueText.text = Mathf.CeilToInt(attack).ToString();
            attackValueText.color = UiTechWastelandPalette.AccentAmber;
        }
    }
}
