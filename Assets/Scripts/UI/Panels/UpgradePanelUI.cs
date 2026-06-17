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

    private void OnRefreshAdClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        ResolveManagers();
        adRewardService?.TryShowRewardedForUpgradeReroll();
    }

    private void OnSelectAllAdClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        ResolveManagers();
        adRewardService?.TryShowRewardedForUpgradeSelectAll();
    }

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

    private void OnUpgradeChoicesReady(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeChoicesPayload payload)
        {
            return;
        }

        DisplayChoices(payload);
        RefreshAdQuota();
    }

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

    private void OnCardSelected(int index)
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        ResolveManagers();

        if (randomRewardManager != null && randomRewardManager.TrySelectChoice(index))
        {
            ClearCards();
        }
    }

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
