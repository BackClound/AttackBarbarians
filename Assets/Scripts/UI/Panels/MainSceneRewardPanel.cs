using System;
using UnityEngine;

/// <summary>
/// MainScene 奖励行：在线、通关、离线收益。
/// </summary>
public class MainSceneRewardPanel : MonoBehaviour
{
    [SerializeField] private GeneralRewardCardPanel onlineReward;
    [SerializeField] private GeneralRewardCardPanel stageReward;
    [SerializeField] private GeneralRewardCardPanel offlineReward;

    /// <summary>奖励行卡片被点击时触发。</summary>
    public event Action<MainSceneAction> RewardClicked;

    /// <summary>绑定各奖励 Widget 点击事件。</summary>
    private void Awake()
    {
        if (onlineReward != null)
        {
            onlineReward.Clicked += OnRewardClicked;
        }

        if (stageReward != null)
        {
            stageReward.Clicked += OnRewardClicked;
        }

        if (offlineReward != null)
        {
            offlineReward.Clicked += OnRewardClicked;
        }
    }

    /// <summary>解绑各奖励 Widget 点击事件。</summary>
    private void OnDestroy()
    {
        if (onlineReward != null)
        {
            onlineReward.Clicked -= OnRewardClicked;
        }

        if (stageReward != null)
        {
            stageReward.Clicked -= OnRewardClicked;
        }

        if (offlineReward != null)
        {
            offlineReward.Clicked -= OnRewardClicked;
        }
    }

    /// <summary>从 <see cref="MetaRewardService"/> 刷新在线/通关/离线奖励文案。</summary>
    public void Refresh()
    {
        if (!ServiceLocator.TryGet(out MetaRewardService meta))
        {
            onlineReward?.SetDisplay("在线奖励", "--:--:--", "未就绪");
            stageReward?.SetDisplay("通关奖励", "--:--:--", "未就绪");
            offlineReward?.SetDisplay("离线收益", "--:--:--", "未就绪");
            return;
        }

        bool canOnline = meta.CanClaimOnlineReward(out _);
        bool canOffline = meta.CanClaimOfflineReward(out _, out _);
        bool canStage = meta.CanClaimStageReward(out _);

        onlineReward?.SetDisplay("在线奖励", meta.GetOnlineTimerText(), canOnline ? "可领取" : "累计中");
        stageReward?.SetDisplay("通关奖励", meta.GetStageRewardTimerText(), canStage ? "可领取" : "冷却中");
        offlineReward?.SetDisplay("离线收益", meta.GetOfflineTimerText(), canOffline ? "可领取" : "累计中");
    }

    /// <summary>根据可领取状态刷新三条奖励的红点。</summary>
    public void ApplyRedDots()
    {
        if (!ServiceLocator.TryGet(out MetaRewardService meta))
        {
            return;
        }

        onlineReward?.SetRedDot(meta.CanClaimOnlineReward(out _));
        stageReward?.SetRedDot(meta.CanClaimStageReward(out _));
        offlineReward?.SetRedDot(meta.CanClaimOfflineReward(out _, out _));
    }

    /// <summary>将 Widget 点击转发为奖励动作事件。</summary>
    private void OnRewardClicked(MainSceneAction action)
    {
        RewardClicked?.Invoke(action);
    }
}
