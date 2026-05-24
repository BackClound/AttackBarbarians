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

    protected override void OnHide()
    {
        UnsubscribeChoices();
        ClearCards();
    }

    private void TrySubscribeChoices()
    {
        if (isSubscribedToChoices)
        {
            return;
        }

        GameEvents.SubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        isSubscribedToChoices = true;
    }

    private void UnsubscribeChoices()
    {
        if (!isSubscribedToChoices)
        {
            return;
        }

        GameEvents.UnsubscribeUpgradeChoicesReady(OnUpgradeChoicesReady);
        isSubscribedToChoices = false;
    }

    private void OnUpgradeChoicesReady(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeChoicesPayload payload)
        {
            return;
        }

        DisplayChoices(payload);
    }

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

        if (randomRewardManager == null)
        {
            ServiceLocator.TryGet(out randomRewardManager);
        }

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
