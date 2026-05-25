using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 七日签到面板：今日签到与状态展示，仅调用 <see cref="DailyRewardManager"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。PanelId = <see cref="GameConstants.UiPanelIds.SignIn"/>。</para>
/// </remarks>
public class DailyRewardPanelUI : UiPanelBase
{
    [Header("Display")]
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text nextDayText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void OnEnable()
    {
        GameEvents.SubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.SubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.SubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.UnsubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.UnsubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    protected override void OnShow()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (!ServiceLocator.TryGet(out DailyRewardManager manager))
        {
            SetStatus("签到系统未就绪");
            if (claimButton != null)
            {
                claimButton.interactable = false;
            }

            return;
        }

        int streak = manager.GetStreakDay();
        int nextDay = manager.ResolveNextClaimDay();
        bool canClaim = manager.CanClaimToday();

        if (streakText != null)
        {
            streakText.text = manager.HasClaimedToday()
                ? $"今日已签到 · 第 {streak} 天"
                : $"连续签到 · 待领第 {nextDay} 天";
        }

        if (nextDayText != null && manager.TryGetEntry(nextDay, out DailyRewardEntrySO entry))
        {
            nextDayText.text = $"奖励：{entry.RewardType} x{entry.RewardAmount}";
        }
        else if (nextDayText != null)
        {
            nextDayText.text = $"第 {nextDay} 日奖励未配置";
        }

        if (claimButton != null)
        {
            claimButton.interactable = canClaim;
        }

        SetStatus(canClaim ? string.Empty : manager.HasClaimedToday() ? "今日已签到" : string.Empty);
    }

    private void OnClaimClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        if (ServiceLocator.TryGet(out DailyRewardManager manager))
        {
            manager.TryClaimToday();
        }
    }

    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.SignIn);
    }

    private void OnDailyRewardClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimedEventArgs args)
        {
            SetStatus($"签到成功：第 {args.DayIndex} 天 +{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    private void OnDailyRewardClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    private void OnDailyRewardStateChanged(GameEventContext ctx) => RefreshAll();

    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }
}
