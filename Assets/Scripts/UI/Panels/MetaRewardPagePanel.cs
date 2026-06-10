using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Meta 奖励页 Presenter：在线、离线、抽奖、通关奖励领取入口。
/// </summary>
public class MetaRewardPagePanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("Reward Rows")]
    [SerializeField] private GeneralRewardCardPanel onlineReward;
    [SerializeField] private GeneralRewardCardPanel offlineReward;
    [SerializeField] private GeneralRewardCardPanel lotteryReward;
    [SerializeField] private GeneralRewardCardPanel stageReward;

    public event Action Closed;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        BindRow(onlineReward, MainSceneAction.OnlineReward);
        BindRow(offlineReward, MainSceneAction.OfflineReward);
        BindRow(lotteryReward, MainSceneAction.LuckyDraw);
        BindRow(stageReward, MainSceneAction.StageReward);
        Hide();
    }

    private void OnDestroy()
    {
        UnbindRow(onlineReward);
        UnbindRow(offlineReward);
        UnbindRow(lotteryReward);
        UnbindRow(stageReward);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        Refresh();
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Closed?.Invoke();
    }

    public void Refresh()
    {
        if (!ServiceLocator.TryGet(out MetaRewardService meta))
        {
            return;
        }

        bool canOnline = meta.CanClaimOnlineReward(out _);
        bool canOffline = meta.CanClaimOfflineReward(out _, out _);
        bool canLottery = meta.CanClaimLottery(out _);
        bool canStage = meta.CanClaimStageReward(out _);

        onlineReward?.SetDisplay("在线奖励", meta.GetOnlineTimerText(), canOnline ? "可领取" : "累计中");
        offlineReward?.SetDisplay("离线收益", meta.GetOfflineTimerText(), canOffline ? "可领取" : "累计中");
        lotteryReward?.SetDisplay("幸运抽奖", meta.GetLotteryTimerText(), canLottery ? "免费" : "冷却中");
        stageReward?.SetDisplay("通关奖励", meta.GetStageRewardTimerText(), canStage ? "可领取" : "冷却中");

        onlineReward?.SetRedDot(canOnline);
        offlineReward?.SetRedDot(canOffline);
        lotteryReward?.SetRedDot(canLottery);
        stageReward?.SetRedDot(canStage);
    }

    public bool TryHandleAction(MainSceneAction action)
    {
        if (!ServiceLocator.TryGet(out MetaRewardService meta))
        {
            SetStatus("奖励系统未就绪");
            return false;
        }

        bool success = action switch
        {
            MainSceneAction.OnlineReward => meta.TryClaimOnlineReward(),
            MainSceneAction.OfflineReward => meta.TryClaimOfflineReward(),
            MainSceneAction.LuckyDraw => meta.TryClaimLottery(),
            MainSceneAction.StageReward => meta.TryClaimStageReward(),
            _ => false,
        };

        if (!success)
        {
            SetStatus(action switch
            {
                MainSceneAction.OnlineReward => "在线时长不足，继续游玩后可领取",
                MainSceneAction.OfflineReward => "离线收益尚未达到领取条件",
                MainSceneAction.LuckyDraw => "抽奖冷却中",
                MainSceneAction.StageReward => "通关奖励冷却中",
                _ => "暂不可领取",
            });
            return true;
        }

        SetStatus("升级卡奖励已发放");
        Refresh();
        return true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    private void BindRow(GeneralRewardCardPanel row, MainSceneAction action)
    {
        if (row == null)
        {
            return;
        }

        row.Clicked += OnRowClicked;
    }

    private void UnbindRow(GeneralRewardCardPanel row)
    {
        if (row != null)
        {
            row.Clicked -= OnRowClicked;
        }
    }

    private void OnRowClicked(MainSceneAction action)
    {
        TryHandleAction(action);
    }
}
