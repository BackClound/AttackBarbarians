using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MainScene 主页面 Presenter：编排子 Panel，负责事件订阅、按钮分发与战斗入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂到 MainScene 的 UI 根 Canvas。</para>
/// <para><b>子 Panel：</b>Profile / Resource / Card / Reward 通过 Inspector 关联，控件在 Prefab 或场景中绑定。</para>
/// </remarks>
public class MainSceneView : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private bool loadBattleSceneOnStart = true;

    [Header("Root")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text statusText;

    [Header("Sub Panels")]
    [SerializeField] private MainSceneProfilePanel profilePanel;
    [SerializeField] private MainSceneResourcePanel resourcePanel;
    [SerializeField] private MainSceneCardPanel topSystemCardPanel;
    [SerializeField] private MainSceneCardPanel promotionCardPanel;
    [SerializeField] private MainSceneCardPanel leftSideCardPanel;
    [SerializeField] private MainSceneCardPanel rightSideCardPanel;
    [SerializeField] private MainSceneCardPanel bottomNavCardPanel;
    [SerializeField] private GeneralCardPanel startBattleCard;
    [SerializeField] private MainSceneRewardPanel rewardPanel;

    public event Action<MainSceneAction> ActionClicked;

    private bool isSubscribed;

    private void Awake()
    {
        BindPanelCallbacks();
        if (startBattleCard != null)
        {
            startBattleCard.Clicked += HandleAction;
        }
    }

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
        UnwireCardPanel(bottomNavCardPanel);

        if (rewardPanel != null)
        {
            rewardPanel.RewardClicked -= HandleAction;
        }

        if (startBattleCard != null)
        {
            startBattleCard.Clicked -= HandleAction;
        }
    }

    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    private void Start()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (!isSubscribed)
        {
            return;
        }

        GameEvents.UnsubscribeResourceChanged(OnGameEventRefresh);
        GameEvents.UnsubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.UnsubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.UnsubscribeDailyRewardStateChanged(OnGameEventRefresh);
        GameEvents.UnsubscribeAchievementProgressChanged(OnGameEventRefresh);
        GameEvents.UnsubscribeAchievementClaimed(OnGameEventRefresh);
        GameEvents.UnsubscribeShopPurchased(OnShopPurchased);
        GameEvents.UnsubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        isSubscribed = false;
    }

    public void RefreshAll()
    {
        profilePanel?.Refresh();
        resourcePanel?.Refresh();
        RefreshRewardPanels();
        RefreshRedDots();
        bottomNavCardPanel?.SetBattleTabSelected(true);
    }

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
        WireCardPanel(bottomNavCardPanel);

        if (rewardPanel != null)
        {
            rewardPanel.RewardClicked += HandleAction;
        }
    }

    private void WireCardPanel(MainSceneCardPanel panel)
    {
        if (panel != null)
        {
            panel.CardClicked += HandleAction;
        }
    }

    private void UnwireCardPanel(MainSceneCardPanel panel)
    {
        if (panel != null)
        {
            panel.CardClicked -= HandleAction;
        }
    }

    private void OnResourceAddClicked(CurrencyType currency)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        string message = currency switch
        {
            CurrencyType.Diamond => "量子钻补充入口待接入",
            CurrencyType.Gold => "金币补充入口待接入",
            CurrencyType.Energy => "体力补充入口待接入",
            _ => "道具补充入口待接入",
        };
        SetStatus(message);
    }

    private void TrySubscribeEvents()
    {
        if (isSubscribed || !ServiceLocator.TryGet(out EventBus _))
        {
            return;
        }

        GameEvents.SubscribeResourceChanged(OnGameEventRefresh);
        GameEvents.SubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.SubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.SubscribeDailyRewardStateChanged(OnGameEventRefresh);
        GameEvents.SubscribeAchievementProgressChanged(OnGameEventRefresh);
        GameEvents.SubscribeAchievementClaimed(OnGameEventRefresh);
        GameEvents.SubscribeShopPurchased(OnShopPurchased);
        GameEvents.SubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        isSubscribed = true;
    }

    private void RefreshRewardPanels()
    {
        bool canClaimDaily = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
        bool canClaimFreeDiamond = ServiceLocator.TryGet(out ShopManager shop) && shop.CanClaimFreeDiamond(out _, out _);
        rewardPanel?.Refresh(canClaimDaily, canClaimFreeDiamond);
    }

    // 刷新红点
    private void RefreshRedDots()
    {
        bool canClaimDaily = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
        bool hasAchievementReward = ServiceLocator.TryGet(out AchievementManager achievements) && achievements.HasClaimableRewards();
        bool canClaimFreeDiamond = ServiceLocator.TryGet(out ShopManager shop) && shop.CanClaimFreeDiamond(out _, out _);

        promotionCardPanel?.SetRedDot(MainSceneAction.DailySignIn, canClaimDaily);
        leftSideCardPanel?.SetRedDot(MainSceneAction.DailySignIn, canClaimDaily);
        leftSideCardPanel?.SetRedDot(MainSceneAction.Achievements, hasAchievementReward);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Mail, true);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Social, true);
        topSystemCardPanel?.SetRedDot(MainSceneAction.Settings, false);
        promotionCardPanel?.SetRedDot(MainSceneAction.FirstCharge, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.MonthlyCard, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.LimitedEvent, true);
        promotionCardPanel?.SetRedDot(MainSceneAction.NewbieWelfare, true);

        rewardPanel?.ApplyRedDots(canClaimDaily, canClaimFreeDiamond);
    }

    private void HandleAction(MainSceneAction action)
    {
        ActionClicked?.Invoke(action);

        switch (action)
        {
            case MainSceneAction.StartBattle:
            case MainSceneAction.Battle:
                PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
                BeginBattle();
                break;

            case MainSceneAction.DailySignIn:
            case MainSceneAction.OnlineReward:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                TryClaimDailyReward();
                break;

            case MainSceneAction.OfflineReward:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                TryClaimFreeDiamond();
                break;

            default:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                SetStatus(GetPendingFeatureMessage(action));
                Debug.Log($"[MainSceneView] {action} clicked.");
                break;
        }
    }

    private void BeginBattle()
    {
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

    private void TryClaimFreeDiamond()
    {
        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            SetStatus("离线收益系统未就绪");
            return;
        }

        if (!shop.TryClaimFreeDiamond())
        {
            shop.CanClaimFreeDiamond(out string reason, out _);
            SetStatus(reason);
        }

        RefreshAll();
    }

    private void OnGameEventRefresh(GameEventContext ctx) => RefreshAll();

    private void OnDailyRewardClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimedEventArgs args)
        {
            SetStatus($"签到成功：第 {args.DayIndex} 天 +{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    private void OnDailyRewardClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimFailedEventArgs args)
        {
            SetStatus(args.Message);
        }

        RefreshAll();
    }

    private void OnShopPurchased(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseEventArgs args)
        {
            SetStatus($"奖励领取成功：+{args.RewardAmount} {args.RewardType}");
        }

        RefreshAll();
    }

    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            SetStatus(args.Message);
        }

        RefreshAll();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    private void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }

    //TODO 获取待接入功能消息
    private static string GetPendingFeatureMessage(MainSceneAction action)
    {
        return action switch
        {
            MainSceneAction.ActivityCenter => "活动中心入口已接入，待绑定活动面板",
            MainSceneAction.DailyTask => "每日任务入口已接入，待绑定任务面板",
            MainSceneAction.LuckyDraw => "幸运抽奖入口已接入，待绑定抽奖系统",
            MainSceneAction.Achievements => "成就入口已接入，待绑定成就详情面板",
            MainSceneAction.FirstCharge => "首充入口已接入，待绑定充值系统",
            MainSceneAction.MonthlyCard => "月卡入口已接入，待绑定月卡系统",
            MainSceneAction.LimitedEvent => "限时活动入口已接入，待绑定活动系统",
            MainSceneAction.NewbieWelfare => "新人福利入口已接入，待绑定福利系统",
            MainSceneAction.Announcement => "公告入口已接入，待绑定公告面板",
            MainSceneAction.Ranking => "排行榜入口已接入，待绑定排行服务",
            MainSceneAction.ValuePack => "超值礼包入口已接入，待绑定礼包系统",
            MainSceneAction.StageReward => "通关奖励入口已接入，待绑定奖励面板",
            MainSceneAction.Shop => "商城入口已接入，待绑定商城面板",
            MainSceneAction.Characters => "角色入口已接入，待绑定角色养成面板",
            MainSceneAction.Tech => "科技入口已接入，待绑定科技树系统",
            MainSceneAction.Base => "基地入口已接入，待绑定基地系统",
            MainSceneAction.Mail => "邮件入口已接入，待绑定邮件系统",
            MainSceneAction.Social => "社交入口已接入，待绑定好友系统",
            MainSceneAction.Settings => "设置入口已接入，待绑定设置面板",
            _ => "功能入口已点击",
        };
    }
}
