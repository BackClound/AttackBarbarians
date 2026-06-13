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

    /// <summary>绑定签到与关闭按钮。</summary>
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

    /// <summary>订阅签到与资源变化事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.SubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.SubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>取消签到事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.UnsubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.UnsubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>显示时刷新连续签到与今日奖励状态。</summary>
    protected override void OnShow()
    {
        RefreshAll();
    }

    /// <summary>刷新连续签到天数、今日奖励与领取按钮状态。</summary>
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

    /// <summary>今日签到领取按钮回调。</summary>
    private void OnClaimClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        if (ServiceLocator.TryGet(out DailyRewardManager manager))
        {
            manager.TryClaimToday();
        }
    }

    /// <summary>关闭签到面板。</summary>
    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.SignIn);
    }

    /// <summary>签到成功后更新状态并刷新。</summary>
    private void OnDailyRewardClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimedEventArgs args)
        {
            SetStatus($"签到成功：第 {args.DayIndex} 天 +{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    /// <summary>签到失败后显示错误信息。</summary>
    private void OnDailyRewardClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    /// <summary>签到状态变化时全量刷新。</summary>
    private void OnDailyRewardStateChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>资源变化时刷新面板。</summary>
    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>更新状态提示文案。</summary>
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }
}
