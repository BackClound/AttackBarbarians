using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就面板：展示进度、领取奖励，仅通过 <see cref="AchievementManager"/> 与事件驱动。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。PanelId = <see cref="GameConstants.UiPanelIds.Achievement"/>。</para>
/// </remarks>
public class AchievementPanelUI : UiPanelBase
{
    [Serializable]
    private class AchievementBinding
    {
        public string achievementConfigId;
        public TMP_Text labelText;
        public Button claimButton;
    }

    [Header("Display")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private AchievementBinding[] bindings;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (bindings != null)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                AchievementBinding binding = bindings[i];
                if (binding?.claimButton == null || string.IsNullOrWhiteSpace(binding.achievementConfigId))
                {
                    continue;
                }

                string capturedId = binding.achievementConfigId;
                binding.claimButton.onClick.AddListener(() => OnClaimClicked(capturedId));
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.SubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.SubscribeAchievementClaimed(OnAchievementClaimed);
        GameEvents.SubscribeAchievementClaimFailed(OnAchievementClaimFailed);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.UnsubscribeAchievementClaimed(OnAchievementClaimed);
        GameEvents.UnsubscribeAchievementClaimFailed(OnAchievementClaimFailed);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    protected override void OnShow()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (bindings == null)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out AchievementManager manager))
        {
            SetStatus("成就系统未就绪");
            return;
        }

        SetStatus(string.Empty);
        for (int i = 0; i < bindings.Length; i++)
        {
            RefreshBinding(bindings[i], manager);
        }
    }

    private void RefreshBinding(AchievementBinding binding, AchievementManager manager)
    {
        if (binding == null || string.IsNullOrWhiteSpace(binding.achievementConfigId))
        {
            return;
        }

        string id = binding.achievementConfigId;
        int progress = manager.GetProgress(id);
        bool completed = manager.IsCompleted(id);
        bool claimed = manager.IsClaimed(id);
        bool canClaim = completed && !claimed;

        if (binding.labelText != null)
        {
            string state = claimed ? "已领取" : completed ? "可领取" : "进行中";
            binding.labelText.text = $"{id} {progress} {state}";
        }

        if (binding.claimButton != null)
        {
            binding.claimButton.interactable = canClaim;
        }
    }

    private void OnClaimClicked(string configId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (ServiceLocator.TryGet(out AchievementManager manager))
        {
            manager.TryClaim(configId);
        }
    }

    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Achievement);
    }

    private void OnAchievementProgressChanged(GameEventContext ctx) => RefreshAll();

    private void OnAchievementClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is AchievementClaimedEventArgs args)
        {
            SetStatus($"领取成功：{args.ConfigId} +{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    private void OnAchievementClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is AchievementClaimFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }
}
