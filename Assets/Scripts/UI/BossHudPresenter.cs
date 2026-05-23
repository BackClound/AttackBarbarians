using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss 血条与阶段提示（最小 HUD）：订阅 Boss 事件并刷新可选 UI 引用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在战斗 UI Canvas 子物体上。</para>
/// <para><b>Inspector：</b>绑定 <c>bossPanel</c>、<c>hpFill</c>、<c>phaseText</c>、<c>nameText</c>；未绑定时仅输出 Debug 日志。</para>
/// </remarks>
public class BossHudPresenter : GameEventSubscriberBase
{
    [Header("UI (optional)")]
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private Image hpFill;
    [SerializeField] private Text phaseText;
    [SerializeField] private Text nameText;

    private GameObject trackedBoss;
    private Enemy_Health trackedHealth;
    private string bossDisplayName = string.Empty;
    private int phaseCount = 1;
    private int currentPhase;

    protected override void RegisterHandlers()
    {
        GameEvents.SubscribeBossSpawned(OnBossSpawned);
        GameEvents.SubscribeBossPhaseChanged(OnBossPhaseChanged);
        GameEvents.SubscribeBossDefeated(OnBossDefeated);
    }

    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribeBossSpawned(OnBossSpawned);
        GameEvents.UnsubscribeBossPhaseChanged(OnBossPhaseChanged);
        GameEvents.UnsubscribeBossDefeated(OnBossDefeated);
    }

    private void Update()
    {
        if (trackedHealth == null || hpFill == null)
        {
            return;
        }

        float maxHp = trackedHealth.MaxHp;
        hpFill.fillAmount = maxHp > 0f ? trackedHealth.CurrentHp / maxHp : 0f;
    }

    private void OnBossSpawned(GameEventContext ctx)
    {
        if (ctx.Payload is not BossSpawnedEventArgs args || args.BossObject == null)
        {
            return;
        }

        trackedBoss = args.BossObject;
        trackedHealth = trackedBoss.GetComponent<Enemy_Health>();
        phaseCount = args.PhaseCount;
        currentPhase = 0;
        bossDisplayName = args.BossConfigId;

        if (ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.TryGetBoss(args.BossConfigId, out BossDataSO bossData))
        {
            bossDisplayName = bossData.DisplayName;
        }

        if (bossPanel != null)
        {
            bossPanel.SetActive(true);
        }

        if (nameText != null)
        {
            nameText.text = bossDisplayName;
        }

        RefreshPhaseLabel();
        Debug.Log($"[BossHud] Boss 出现：{bossDisplayName} HP={args.MaxHp}");
    }

    private void OnBossPhaseChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not BossPhaseChangedEventArgs args)
        {
            return;
        }

        currentPhase = args.NewPhaseIndex;
        RefreshPhaseLabel();
        Debug.Log($"[BossHud] 阶段 {args.PreviousPhaseIndex} → {args.NewPhaseIndex}");
    }

    private void OnBossDefeated(GameEventContext ctx)
    {
        trackedBoss = null;
        trackedHealth = null;

        if (bossPanel != null)
        {
            bossPanel.SetActive(false);
        }

        if (ctx.Payload is BossDefeatedEventArgs args)
        {
            Debug.Log($"[BossHud] Boss 击败：{args.BossConfigId} 奖励经验+{args.BonusExperience}");
        }
    }

    private void RefreshPhaseLabel()
    {
        if (phaseText == null)
        {
            return;
        }

        phaseText.text = $"Phase {currentPhase + 1}/{phaseCount}";
    }
}
