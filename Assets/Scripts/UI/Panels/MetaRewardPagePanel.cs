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

    /// <summary>弹层关闭时触发。</summary>
    public event Action Closed;

    /// <summary>绑定关闭按钮与各奖励行点击，默认隐藏。</summary>
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

    /// <summary>解绑关闭按钮与奖励行事件。</summary>
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

    /// <summary>显示 Meta 奖励页并刷新四条奖励状态。</summary>
    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        Refresh();
    }

    /// <summary>隐藏 Meta 奖励页并触发 <see cref="Closed"/>。</summary>
    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Closed?.Invoke();
    }

    /// <summary>刷新在线/离线/抽奖/通关奖励文案与红点。</summary>
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

    /// <summary>尝试领取指定 Meta 奖励并更新状态栏提示。</summary>
    /// <param name="action">奖励动作标识。</param>
    /// <returns>已处理返回 <c>true</c>，未识别动作返回 <c>false</c>。</returns>
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

    /// <summary>更新页内状态提示文案。</summary>
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    /// <summary>绑定奖励行点击事件。</summary>
    private void BindRow(GeneralRewardCardPanel row, MainSceneAction action)
    {
        if (row == null)
        {
            return;
        }

        row.Clicked += OnRowClicked;
    }

    /// <summary>解绑奖励行点击事件。</summary>
    private void UnbindRow(GeneralRewardCardPanel row)
    {
        if (row != null)
        {
            row.Clicked -= OnRowClicked;
        }
    }

    /// <summary>奖励行点击后尝试领取。</summary>
    private void OnRowClicked(MainSceneAction action)
    {
        TryHandleAction(action);
    }
}
