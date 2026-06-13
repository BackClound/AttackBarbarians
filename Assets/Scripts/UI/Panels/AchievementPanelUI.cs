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
    /// <summary>成就行 Inspector 绑定：配置 ID、标签与领取按钮。</summary>
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

    /// <summary>绑定关闭与各成就领取按钮。</summary>
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

    /// <summary>订阅成就进度与领取事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.SubscribeAchievementClaimed(OnAchievementClaimed);
        GameEvents.SubscribeAchievementClaimFailed(OnAchievementClaimFailed);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>取消成就事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.UnsubscribeAchievementClaimed(OnAchievementClaimed);
        GameEvents.UnsubscribeAchievementClaimFailed(OnAchievementClaimFailed);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>显示时刷新各成就进度与可领取状态。</summary>
    protected override void OnShow()
    {
        RefreshAll();
    }

    /// <summary>刷新所有成就行进度与可领取状态。</summary>
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

    /// <summary>刷新单条成就绑定行的文案与按钮。</summary>
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

    /// <summary>成就领取按钮回调。</summary>
    private void OnClaimClicked(string configId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (ServiceLocator.TryGet(out AchievementManager manager))
        {
            manager.TryClaim(configId);
        }
    }

    /// <summary>关闭成就面板。</summary>
    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Achievement);
    }

    /// <summary>成就进度变化时全量刷新。</summary>
    private void OnAchievementProgressChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>成就领取成功后提示并刷新。</summary>
    private void OnAchievementClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is AchievementClaimedEventArgs args)
        {
            SetStatus($"领取成功：{args.ConfigId} +{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    /// <summary>成就领取失败后显示错误。</summary>
    private void OnAchievementClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is AchievementClaimFailedEventArgs args)
        {
            SetStatus(args.Message);
        }
    }

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
