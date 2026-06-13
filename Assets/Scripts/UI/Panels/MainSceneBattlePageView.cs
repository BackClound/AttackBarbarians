using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战斗/主城页面 Presenter：头像、资源、侧边入口、奖励与开始战斗。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>BattlePagePanelRoot</c>，与 <see cref="SpaceCityBackground"/>、<c>SafeAreaRoot</c> 同层或作为其父节点。</para>
/// </remarks>
public class MainSceneBattlePageView : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private bool loadBattleSceneOnStart = true;

    [Header("Root")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text statusText;

    [Header("Sub Panels")]
    [SerializeField] private MainSceneProfilePanel profilePanel;
    [SerializeField] private MainSceneResourcePanel resourcePanel;
    [SerializeField] private MainSceneCardPanel topSystemCardPanel;
    [SerializeField] private MainSceneCardPanel promotionCardPanel;
    [SerializeField] private MainSceneCardPanel leftSideCardPanel;
    [SerializeField] private MainSceneCardPanel rightSideCardPanel;
    [SerializeField] private GeneralCardPanel startBattleCard;
    [SerializeField] private MainSceneRewardPanel rewardPanel;
    [SerializeField] private MetaRewardPagePanel metaRewardPage;
    [SerializeField] private UpgradeCardRewardPopupPanel upgradeCardPopup;

    /// <summary>战斗页内容区入口被点击时向上层抛出。</summary>
    public event Action<MainSceneAction> ContentActionClicked;

    /// <summary>绑定各子 Panel 与开始战斗卡片回调。</summary>
    private void Awake()
    {
        BindPanelCallbacks();
        if (startBattleCard != null)
        {
            startBattleCard.Clicked += OnCardAction;
        }
    }

    /// <summary>解绑各子 Panel 回调。</summary>
    private void OnDestroy()
    {
        if (resourcePanel != null)
        {
            resourcePanel.AddClicked -= OnResourceAddClicked;
        }

        UnwireCardPanel(topSystemCardPanel);
        UnwireCardPanel(promotionCardPanel);
        UnwireCardPanel(leftSideCardPanel);
        UnwireCardPanel(rightSideCardPanel);

        if (rewardPanel != null)
        {
            rewardPanel.RewardClicked -= OnCardAction;
        }

        if (startBattleCard != null)
        {
            startBattleCard.Clicked -= OnCardAction;
        }
    }

    /// <summary>刷新头像、资源、奖励区与各入口红点。</summary>
    public void RefreshAll()
    {
        profilePanel?.Refresh();
        resourcePanel?.Refresh();
        RefreshRewardPanels();
        RefreshRedDots();
    }

    /// <summary>由 <see cref="MainSceneView"/> 转发底栏以外的入口点击。</summary>
    public bool TryHandleAction(MainSceneAction action)
    {
        switch (action)
        {
            case MainSceneAction.StartBattle:
                PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
                BeginBattle();
                return true;

            case MainSceneAction.DailySignIn:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                TryClaimDailyReward();
                return true;

            case MainSceneAction.OnlineReward:
            case MainSceneAction.OfflineReward:
            case MainSceneAction.StageReward:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                return TryClaimMetaReward(action);

            case MainSceneAction.LuckyDraw:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                metaRewardPage?.Show();
                return metaRewardPage != null && metaRewardPage.TryHandleAction(action);

            default:
                if (IsContentCardAction(action))
                {
                    PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                    SetStatus(GetPendingFeatureMessage(action));
                    Debug.Log($"[MainSceneBattlePageView] {action} clicked.");
                    return true;
                }

                return false;
        }
    }

    /// <summary>更新底部状态栏提示文案。</summary>
    /// <param name="message">状态消息。</param>
    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    /// <summary>商店购买成功后刷新并显示状态提示。</summary>
    /// <param name="args">购买事件参数。</param>
    public void OnShopPurchased(ShopPurchaseEventArgs args)
    {
        RefreshAll();
        SetStatus($"奖励领取成功：+{args.RewardAmount} {args.RewardType}");
    }

    /// <summary>商店购买失败后刷新并显示错误信息。</summary>
    /// <param name="message">失败原因。</param>
    public void OnShopPurchaseFailed(string message)
    {
        RefreshAll();
        SetStatus(message);
    }

    /// <summary>签到成功后更新状态栏并刷新 UI。</summary>
    /// <param name="args">签到事件参数。</param>
    public void OnDailyRewardClaimed(DailyRewardClaimedEventArgs args)
    {
        SetStatus($"签到成功：第 {args.DayIndex} 天 +{args.RewardAmount} {args.RewardType}");
        RefreshAll();
    }

    /// <summary>签到失败后显示错误并刷新 UI。</summary>
    /// <param name="message">失败原因。</param>
    public void OnDailyRewardClaimFailed(string message)
    {
        SetStatus(message);
        RefreshAll();
    }

    /// <summary>资源变化时全量刷新战斗页 UI。</summary>
    public void OnResourceChanged() => RefreshAll();

    /// <summary>绑定资源条与各卡片 Panel 的点击回调。</summary>
    private void BindPanelCallbacks()
    {
        if (resourcePanel != null)
        {
            resourcePanel.AddClicked += OnResourceAddClicked;
        }

        WireCardPanel(topSystemCardPanel);
        WireCardPanel(promotionCardPanel);
        WireCardPanel(leftSideCardPanel);
        WireCardPanel(rightSideCardPanel);

        if (rewardPanel != null)
        {
            rewardPanel.RewardClicked += OnCardAction;
        }
    }

    /// <summary>订阅卡片 Panel 的 CardClicked 事件。</summary>
    private void WireCardPanel(MainSceneCardPanel panel)
    {
        if (panel != null)
        {
            panel.CardClicked += OnCardAction;
        }
    }

    /// <summary>取消订阅卡片 Panel 事件。</summary>
    private void UnwireCardPanel(MainSceneCardPanel panel)
    {
        if (panel != null)
        {
            panel.CardClicked -= OnCardAction;
        }
    }

    /// <summary>将子 Panel 点击转发给根 Presenter。</summary>
    private void OnCardAction(MainSceneAction action)
    {
        ContentActionClicked?.Invoke(action);
    }

    /// <summary>处理资源加号点击（广告券/体力走广告服务）。</summary>
    private void OnResourceAddClicked(CurrencyType currency)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (currency == CurrencyType.AdTicket || currency == CurrencyType.Stamina)
        {
            if (!ServiceLocator.TryGet(out AdRewardService adService))
            {
                SetStatus("广告服务未就绪");
                return;
            }

            adService.TryShowRewardedForAdTicket();
            return;
        }

        string message = currency switch
        {
            CurrencyType.Diamond => "量子钻补充入口待接入",
            CurrencyType.Gold => "金币补充入口待接入",
            _ => "道具补充入口待接入",
        };
        SetStatus(message);
    }

    /// <summary>刷新主奖励行与 Meta 奖励页。</summary>
    private void RefreshRewardPanels()
    {
        rewardPanel?.Refresh();
        metaRewardPage?.Refresh();
    }

    /// <summary>根据各 Manager 状态刷新促销/侧边/系统入口红点。</summary>
    private void RefreshRedDots()
    {
        bool canClaimDaily = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
        bool hasAchievementReward = ServiceLocator.TryGet(out AchievementManager achievements) && achievements.HasClaimableRewards();
        bool canClaimLottery = ServiceLocator.TryGet(out MetaRewardService meta) && meta.CanClaimLottery(out _);

        promotionCardPanel?.SetRedDot(MainSceneAction.DailySignIn, canClaimDaily);
        leftSideCardPanel?.SetRedDot(MainSceneAction.DailySignIn, canClaimDaily);
        leftSideCardPanel?.SetRedDot(MainSceneAction.Achievements, hasAchievementReward);
        leftSideCardPanel?.SetRedDot(MainSceneAction.LuckyDraw, canClaimLottery);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Mail, true);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Social, true);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Settings, false);
        promotionCardPanel?.SetRedDot(MainSceneAction.FirstCharge, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.MonthlyCard, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.LimitedEvent, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.NewbieWelfare, true);

        rewardPanel?.ApplyRedDots();
    }

    /// <summary>扣除体力、初始化 Run 并加载战斗场景。</summary>
    private void BeginBattle()
    {
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            if (!resources.TrySpend(
                    CurrencyType.Stamina,
                    StaminaConstants.BattleEntryCost,
                    ResourceChangeReason.BattleStart,
                    out string failureReason))
            {
                SetStatus(failureReason ?? "体力不足");
                return;
            }
        }

        RunDifficultyContext.IsEliteMode = false;
        if (RunDifficultyContext.EliteConfig == null)
        {
            RunDifficultyContext.EliteConfig = Resources.Load<EliteModeConfigSO>(GameConstants.ResourcePaths.EliteModeConfig);
        }

        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.BeginRun(1, 1);
            save.SaveImmediate();
        }

        SetStatus("正在进入战场...");

        if (loadBattleSceneOnStart && !string.IsNullOrWhiteSpace(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
            return;
        }

        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.BeginLoading();
            gameManager.StartGame();
        }
    }

    /// <summary>尝试领取今日签到奖励。</summary>
    private void TryClaimDailyReward()
    {
        if (!ServiceLocator.TryGet(out DailyRewardManager daily))
        {
            SetStatus("签到系统未就绪");
            return;
        }

        if (!daily.TryClaimToday())
        {
            SetStatus(daily.HasClaimedToday() ? "今日已签到" : "暂无可领取签到奖励");
        }

        RefreshAll();
    }

    /// <summary>尝试领取在线/离线/通关 Meta 奖励。</summary>
    private bool TryClaimMetaReward(MainSceneAction action)
    {
        if (metaRewardPage != null && metaRewardPage.TryHandleAction(action))
        {
            RefreshAll();
            return true;
        }

        if (!ServiceLocator.TryGet(out MetaRewardService meta))
        {
            SetStatus("奖励系统未就绪");
            return true;
        }

        bool success = action switch
        {
            MainSceneAction.OnlineReward => meta.TryClaimOnlineReward(),
            MainSceneAction.OfflineReward => meta.TryClaimOfflineReward(),
            MainSceneAction.StageReward => meta.TryClaimStageReward(),
            _ => false,
        };

        if (!success)
        {
            SetStatus("暂不可领取，请继续累计时长");
        }
        else
        {
            SetStatus("升级卡奖励已发放");
        }

        RefreshAll();
        return true;
    }

    /// <summary>判断动作是否属于战斗页内容卡片。</summary>
    private static bool IsContentCardAction(MainSceneAction action) =>
        action switch
        {
            MainSceneAction.Shop or MainSceneAction.Battle or MainSceneAction.Characters or MainSceneAction.Tech or MainSceneAction.Base => false,
            _ => true,
        };

    /// <summary>播放 UI 音效。</summary>
    private static void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(null, sfxId);
        }
    }

    /// <summary>返回未接入功能的占位提示。</summary>
    private static string GetPendingFeatureMessage(MainSceneAction action) =>
        action switch
        {
            MainSceneAction.ActivityCenter => "活动中心入口已接入，待绑定活动面板",
            MainSceneAction.DailyTask => "每日任务入口已接入，待绑定任务面板",
            MainSceneAction.LuckyDraw => "幸运抽奖入口已接入",
            MainSceneAction.Achievements => "成就入口已接入，待绑定成就详情面板",
            MainSceneAction.FirstCharge => "首充入口已接入，待绑定充值系统",
            MainSceneAction.MonthlyCard => "月卡入口已接入，待绑定月卡系统",
            MainSceneAction.LimitedEvent => "限时活动入口已接入，待绑定活动系统",
            MainSceneAction.NewbieWelfare => "新人福利入口已接入，待绑定福利系统",
            MainSceneAction.Announcement => "公告入口已接入，待绑定公告面板",
            MainSceneAction.Ranking => "排行榜入口已接入，待绑定排行服务",
            MainSceneAction.ValuePack => "超值礼包入口已接入，待绑定礼包系统",
            MainSceneAction.StageReward => "通关奖励入口已接入",
            MainSceneAction.Characters => "角色入口已接入，待绑定角色养成面板",
            MainSceneAction.Tech => "科技入口已接入，待绑定科技树系统",
            MainSceneAction.Base => "基地入口已接入，待绑定基地系统",
            MainSceneAction.Mail => "邮件入口已接入，待绑定邮件系统",
            MainSceneAction.Social => "社交入口已接入，待绑定好友系统",
            MainSceneAction.Settings => "设置入口已接入，待绑定设置面板",
            _ => "功能入口已点击",
        };
}
