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

    public event Action<MainSceneAction> RewardClicked;

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

    private void OnRewardClicked(MainSceneAction action)
    {
        RewardClicked?.Invoke(action);
    }
}
