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

    public void Refresh(bool canClaimDaily, bool canClaimFreeDiamond)
    {
        // 刷新在线奖励 
        //TODO 这应该是一个CoroutineScope，因为需要时时根据时间更新状态
        onlineReward?.SetDisplay(
            "在线奖励",
            canClaimDaily ? "00:00:00" : "00:15:30",
            canClaimDaily ? "可领取" : "倒计时");
        stageReward?.SetDisplay("通关奖励", string.Empty, "可领取");
        offlineReward?.SetDisplay(
            "离线收益",
            canClaimFreeDiamond ? "00:00:00" : "02:30:45",
            canClaimFreeDiamond ? "可领取" : "累计中");
    }

    public void ApplyRedDots(bool canClaimDaily, bool canClaimFreeDiamond)
    {
        onlineReward?.SetRedDot(canClaimDaily);
        stageReward?.SetRedDot(true);
        offlineReward?.SetRedDot(canClaimFreeDiamond);
    }

    private void OnRewardClicked(MainSceneAction action)
    {
        RewardClicked?.Invoke(action);
    }
}
