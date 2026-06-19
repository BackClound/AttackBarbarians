using TMPro;
using UnityEngine;

/// <summary>
/// 肉鸽升级三选一：订阅 <see cref="GameEvents.UpgradeChoicesReady"/>，选择后调用 <see cref="RandomRewardManager.TrySelectChoice"/>。
/// </summary>
/// <remarks>
/// <para><b>时间缩放：</b>由 <see cref="GameStateMachine"/> 在 <see cref="GameState.UpgradeChoosing"/> 置 0，本面板不重复写 Time.timeScale。</para>
/// </remarks>
public class UpgradePanelUI : UiPanelBase
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pausedHintText;
    [SerializeField] private UpgradeChoiceCardView[] choiceCards;

    [Header("Ad Actions")]
    [SerializeField] private TMP_Text adQuotaHintText;
    [SerializeField] private UiRewardedAdButton refreshAdButton;
    [SerializeField] private UiRewardedAdButton selectAllAdButton;

    private RandomRewardManager randomRewardManager;
    private AdRewardService adRewardService;
    private bool isSubscribedToChoices;

    /// <summary>激活时订阅事件，避免 Show 之前错过 ChoicesReady。</summary>
    private void OnEnable()
    {
        TrySubscribeChoices();
    }

    /// <summary>停用时取消订阅。</summary>
    private void OnDisable()
    {
        UnsubscribeChoices();
    }

    /// <summary>显示时订阅升级选项事件并尝试展示待选卡片。</summary>
    protected override void OnShow()
    {
        TrySubscribeChoices();
        if (titleText != null)
        {
            titleText.text = "选择技能";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }

        if (pausedHintText != null)
        {
            pausedHintText.gameObject.SetActive(false);
        }

        ResolveManagers();
        BindAdButtons();
        TryDisplayPendingChoices();
        RefreshAdQuota();
    }

    /// <summary>隐藏时清空卡片。</summary>
    protected override void OnHide()
    {
        ClearCards();
    }

    /// <summary>解析 RandomRewardManager 与 AdRewardService 引用。</summary>
    private void ResolveManagers()
    {
        if (!ServiceLocator.TryGet(out randomRewardManager))
        {
            randomRewardManager = FindFirstObjectByType<RandomRewardManager>();
        }

        if (!ServiceLocator.TryGet(out adRewardService))
        {
            adRewardService = FindFirstObjectByType<AdRewardService>();
        }
    }

    /// <summary>绑定刷新与全选激励广告按钮的文案与点击事件。</summary>
    private void BindAdButtons()
    {
        if (refreshAdButton != null)
        {
            refreshAdButton.SetLabel("刷新");
            refreshAdButton.SetAdBadge("AD");
            refreshAdButton.BindClick(OnRefreshAdClicked);
        }

        if (selectAllAdButton != null)
        {
            selectAllAdButton.SetLabel("全选");
            selectAllAdButton.SetAdBadge("AD");
            selectAllAdButton.BindClick(OnSelectAllAdClicked);
        }
    }

    /// <summary>刷新广告按钮点击：请求重新抽取升级选项。</summary>
    private void OnRefreshAdClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        ResolveManagers();
        adRewardService?.TryShowRewardedForUpgradeReroll();
    }

    /// <summary>全选广告按钮点击：请求观看广告并领取全部选项。</summary>
    private void OnSelectAllAdClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        ResolveManagers();
        adRewardService?.TryShowRewardedForUpgradeSelectAll();
    }

    /// <summary>刷新当局剩余广告次数与按钮可交互状态。</summary>
    private void RefreshAdQuota()
    {
        ResolveManagers();
        if (randomRewardManager == null)
        {
            return;
        }

        if (adQuotaHintText != null)
        {
            adQuotaHintText.text =
                $"当局剩余观看次数 {randomRewardManager.RemainingAdRerolls}/{randomRewardManager.MaxAdRerollsPerRun}";
            adQuotaHintText.color = UiTechWastelandPalette.TextSecondary;
        }

        if (refreshAdButton != null)
        {
            refreshAdButton.SetInteractable(randomRewardManager.RemainingAdRerolls > 0);
        }

        if (selectAllAdButton != null)
        {
            selectAllAdButton.SetInteractable(randomRewardManager.RemainingAdSelectAll > 0);
        }
    }

    /// <summary>订阅升级选项就绪与广告奖励完成事件。</summary>
    private void TrySubscribeChoices()
    {
        if (isSubscribedToChoices)
        {
            return;
        }

        GameEvents.SubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        GameEvents.SubscribeAdRewardCompleted(OnAdRewardCompleted);
        isSubscribedToChoices = true;
    }

    /// <summary>取消升级选项与广告相关事件订阅。</summary>
    private void UnsubscribeChoices()
    {
        if (!isSubscribedToChoices)
        {
            return;
        }

        GameEvents.UnsubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        GameEvents.UnsubscribeAdRewardCompleted(OnAdRewardCompleted);
        isSubscribedToChoices = false;
    }

    /// <summary>广告奖励完成后刷新配额或清空卡片（全选时）。</summary>
    /// <param name="ctx">广告奖励事件上下文。</param>
    private void OnAdRewardCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is not AdRewardCompletedEventArgs args)
        {
            return;
        }

        if (args.Source != AdRewardSource.UpgradeReroll && args.Source != AdRewardSource.UpgradeSelectAll)
        {
            return;
        }

        RefreshAdQuota();
        if (args.Source == AdRewardSource.UpgradeSelectAll)
        {
            ClearCards();
            return;
        }

        TryDisplayPendingChoices();
    }

    /// <summary>升级选项就绪时展示三选一卡片。</summary>
    /// <param name="ctx">选项就绪事件上下文。</param>
    private void OnUpgradeChoicesReady(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeChoicesPayload payload)
        {
            return;
        }

        DisplayChoices(payload);
        RefreshAdQuota();
    }

    /// <summary>若已有待选 payload 则立即展示。</summary>
    private void TryDisplayPendingChoices()
    {
        if (randomRewardManager == null)
        {
            ResolveManagers();
        }

        if (randomRewardManager?.PendingChoices != null)
        {
            DisplayChoices(randomRewardManager.PendingChoices);
        }
    }

    /// <summary>将候选列表绑定到卡片视图。</summary>
    /// <param name="payload">三选一选项数据。</param>
    private void DisplayChoices(UpgradeChoicesPayload payload)
    {
        if (choiceCards == null)
        {
            return;
        }

        var choices = payload.Choices;
        for (int i = 0; i < choiceCards.Length; i++)
        {
            UpgradeChoiceCardView card = choiceCards[i];
            if (card == null)
            {
                continue;
            }

            if (i < choices.Count && choices[i] != null)
            {
                card.Bind(i, OnCardSelected);
                card.SetData(choices[i]);
            }
            else
            {
                card.Clear();
            }
        }
    }

    /// <summary>玩家选中卡片后提交选择并清空展示。</summary>
    /// <param name="index">卡片索引。</param>
    private void OnCardSelected(int index)
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        ResolveManagers();

        if (randomRewardManager != null && randomRewardManager.TrySelectChoice(index))
        {
            ClearCards();
        }
    }

    /// <summary>清空所有卡片绑定与显示。</summary>
    private void ClearCards()
    {
        if (choiceCards == null)
        {
            return;
        }

        for (int i = 0; i < choiceCards.Length; i++)
        {
            choiceCards[i]?.Clear();
        }
    }
}
