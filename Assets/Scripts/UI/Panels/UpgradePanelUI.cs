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

    private RandomRewardManager randomRewardManager;
    private bool isSubscribedToChoices;

    /// <summary>显示时订阅升级选项事件并尝试展示待选卡片。</summary>
    protected override void OnShow()
    {
        TrySubscribeChoices();
        if (titleText != null)
        {
            titleText.text = "模块重组 // SELECT 1";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }

        if (pausedHintText != null)
        {
            pausedHintText.text = "SYSTEM PAUSED";
            pausedHintText.color = UiTechWastelandPalette.TextTerminal;
        }

        if (!ServiceLocator.TryGet(out randomRewardManager))
        {
            randomRewardManager = FindFirstObjectByType<RandomRewardManager>();
        }

        TryDisplayPendingChoices();
    }

    /// <summary>隐藏时取消订阅并清空卡片。</summary>
    protected override void OnHide()
    {
        UnsubscribeChoices();
        ClearCards();
    }

    /// <summary>订阅升级三选一就绪事件。</summary>
    private void TrySubscribeChoices()
    {
        if (isSubscribedToChoices)
        {
            return;
        }

        GameEvents.SubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        isSubscribedToChoices = true;
    }

    /// <summary>取消升级选项事件订阅。</summary>
    private void UnsubscribeChoices()
    {
        if (!isSubscribedToChoices)
        {
            return;
        }

        GameEvents.UnsubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        isSubscribedToChoices = false;
    }

    /// <summary>收到升级选项后展示卡片。</summary>
    private void OnUpgradeChoicesReady(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeChoicesPayload payload)
        {
            return;
        }

        DisplayChoices(payload);
    }

    /// <summary>显示 RandomRewardManager 中待选选项。</summary>
    private void TryDisplayPendingChoices()
    {
        if (randomRewardManager == null)
        {
            ServiceLocator.TryGet(out randomRewardManager);
        }

        if (randomRewardManager?.PendingChoices != null)
        {
            DisplayChoices(randomRewardManager.PendingChoices);
        }
    }

    /// <summary>将选项数据绑定到三张卡片 View。</summary>
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

    /// <summary>玩家选中卡片后调用 RandomRewardManager 确认。</summary>
    private void OnCardSelected(int index)
    {
        PlayUiSfx("audio.sfx.ui_confirm");

        if (randomRewardManager == null)
        {
            ServiceLocator.TryGet(out randomRewardManager);
        }

        if (randomRewardManager != null && randomRewardManager.TrySelectChoice(index))
        {
            ClearCards();
        }
    }

    /// <summary>清空并隐藏所有选项卡片。</summary>
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
