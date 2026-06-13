using System;
using UnityEngine;

/// <summary>
/// MainScene UI 根 Presenter：底栏导航、页面切换，委托各子页面处理业务。
/// </summary>
/// <remarks>
/// <para><b>层级：</b></para>
/// <code>
/// MainSceneUI [MainSceneView]
/// ├── BattlePagePanelRoot [MainSceneBattlePageView]
/// │   ├── SpaceCityBackground（全屏，可出血刘海区）
/// │   └── SafeAreaRoot [UiSafeAreaFitter + 底栏预留 inset]
/// ├── ShopPageRoot [ShopSceneView]
/// │   ├── ShopPageBackground
/// │   └── SafeAreaRoot
/// │       ├── ShopTopBar（固定）
/// │       ├── ShopScrollHost（仅此区域 ScrollView）
/// │       └── ShopStatusBar（固定）
/// └── BottomNav（Canvas 同级最后子节点，BottomStretch，不随 ScrollView 滚动）
/// </code>
/// </remarks>
public class MainSceneView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Canvas canvas;

    [Header("Pages")]
    [SerializeField] private MainSceneBattlePageView battlePage;
    [SerializeField] private ShopSceneView shopPage;
    [SerializeField] private MainScenePage currentPage = MainScenePage.Home;

    [Header("Shared")]
    [SerializeField] private MainSceneCardPanel bottomNavCardPanel;

    /// <summary>页面内任意入口被点击时触发，供外部监听。</summary>
    public event Action<MainSceneAction> ActionClicked;

    private bool isSubscribed;

    /// <summary>绑定子页面与底栏事件，初始化默认页面显隐。</summary>
    private void Awake()
    {
        if (battlePage != null)
        {
            battlePage.ContentActionClicked += HandleAction;
        }

        if (bottomNavCardPanel != null)
        {
            bottomNavCardPanel.CardClicked += HandleAction;
        }

        if (shopPage != null)
        {
            shopPage.gameObject.SetActive(false);
        }

        if (battlePage != null)
        {
            battlePage.gameObject.SetActive(true);
        }

        ShowPage(currentPage, false);
    }

    /// <summary>解绑子页面与底栏事件。</summary>
    private void OnDestroy()
    {
        if (battlePage != null)
        {
            battlePage.ContentActionClicked -= HandleAction;
        }

        if (bottomNavCardPanel != null)
        {
            bottomNavCardPanel.CardClicked -= HandleAction;
        }
    }

    /// <summary>订阅游戏事件并全量刷新 UI。</summary>
    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    /// <summary>确保事件订阅并刷新展示。</summary>
    private void Start()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    /// <summary>取消所有 <see cref="GameEvents"/> 订阅。</summary>
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
        GameEvents.UnsubscribeAdRewardFailed(OnAdRewardFailed);
        GameEvents.UnsubscribeAdRewardCompleted(OnAdRewardCompleted);
        GameEvents.UnsubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = false;
    }

    /// <summary>刷新底栏选中态，并按当前页面刷新战斗页或商城页。</summary>
    public void RefreshAll()
    {
        bottomNavCardPanel?.SetNavTabSelected(MapPageToAction(currentPage));

        if (currentPage == MainScenePage.Shop)
        {
            shopPage?.RefreshAll();
            return;
        }

        battlePage?.RefreshAll();
    }

    /// <summary>切换底栏页面（战斗/商城等）并可选刷新。</summary>
    /// <param name="page">目标页面。</param>
    /// <param name="refresh">是否在切换后刷新数据。</param>
    public void ShowPage(MainScenePage page, bool refresh = true)
    {
        currentPage = page;
        bool isShop = page == MainScenePage.Shop;

        if (battlePage != null)
        {
            battlePage.gameObject.SetActive(!isShop);
        }

        if (shopPage != null)
        {
            shopPage.gameObject.SetActive(isShop);
        }

        bottomNavCardPanel?.SetNavTabSelected(MapPageToAction(page));

        if (!refresh)
        {
            return;
        }

        RefreshAll();

        if (!isShop)
        {
            battlePage?.SetStatus(string.Empty);
        }
    }

    /// <summary>统一处理底栏与内容区点击，分发页面切换或子页逻辑。</summary>
    private void HandleAction(MainSceneAction action)
    {
        ActionClicked?.Invoke(action);

        switch (action)
        {
            case MainSceneAction.Shop:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                ShowPage(MainScenePage.Shop);
                return;

            case MainSceneAction.Battle:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                ShowPage(MainScenePage.Home);
                return;

            case MainSceneAction.Characters:
            case MainSceneAction.Tech:
            case MainSceneAction.Base:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                ShowPage(MainScenePage.Home);
                bottomNavCardPanel?.SetNavTabSelected(action);
                battlePage?.SetStatus(GetPendingNavMessage(action));
                Debug.Log($"[MainSceneView] {action} clicked.");
                return;
        }

        if (battlePage != null && battlePage.TryHandleAction(action))
        {
            return;
        }

        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        battlePage?.SetStatus(GetPendingNavMessage(action));
        Debug.Log($"[MainSceneView] {action} clicked.");
    }

    /// <summary>延迟订阅 GameEvents，避免 ServiceLocator 未就绪。</summary>
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
        GameEvents.SubscribeAdRewardFailed(OnAdRewardFailed);
        GameEvents.SubscribeAdRewardCompleted(OnAdRewardCompleted);
        GameEvents.SubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = true;
    }

    /// <summary>资源/成就等事件触发时刷新当前可见页面。</summary>
    private void OnGameEventRefresh(GameEventContext ctx)
    {
        battlePage?.OnResourceChanged();
        if (currentPage == MainScenePage.Shop)
        {
            shopPage?.RefreshAll();
        }
    }

    /// <summary>转发签到成功事件到战斗页。</summary>
    private void OnDailyRewardClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimedEventArgs args)
        {
            battlePage?.OnDailyRewardClaimed(args);
        }
    }

    /// <summary>转发签到失败消息到战斗页。</summary>
    private void OnDailyRewardClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimFailedEventArgs args)
        {
            battlePage?.OnDailyRewardClaimFailed(args.Message);
        }
    }

    /// <summary>按当前页面刷新商城或战斗页并显示购买结果。</summary>
    private void OnShopPurchased(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseEventArgs args)
        {
            if (currentPage == MainScenePage.Shop)
            {
                shopPage?.RefreshAll();
            }
            else
            {
                battlePage?.OnShopPurchased(args);
            }
        }
    }

    /// <summary>按当前页面显示购买失败信息。</summary>
    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            if (currentPage == MainScenePage.Shop)
            {
                shopPage?.RefreshAll();
            }
            else
            {
                battlePage?.OnShopPurchaseFailed(args.Message);
            }
        }
    }

    /// <summary>处理广告奖励失败并更新状态栏。</summary>
    private void OnAdRewardFailed(GameEventContext ctx)
    {
        if (ctx.Payload is not AdRewardFailedEventArgs args)
        {
            return;
        }

        if (currentPage == MainScenePage.Shop && args.Source == AdRewardSource.Shop)
        {
            return;
        }

        if (currentPage == MainScenePage.Shop && args.Source == AdRewardSource.AdTicket)
        {
            shopPage?.RefreshAll();
            return;
        }

        battlePage?.SetStatus(args.Message);
        battlePage?.OnResourceChanged();
    }

    /// <summary>升级卡发放后刷新战斗页并提示。</summary>
    private void OnUpgradeCardGranted(GameEventContext ctx)
    {
        if (ctx.Payload is not UpgradeCardGrantedEventArgs args)
        {
            return;
        }

        battlePage?.RefreshAll();
        if (args.Grants != null && args.Grants.Count > 0)
        {
            battlePage?.SetStatus($"获得升级卡：{args.Grants[0].DisplayName}");
        }
    }

    /// <summary>非商城页广告奖励完成后更新状态栏。</summary>
    private void OnAdRewardCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is not AdRewardCompletedEventArgs args)
        {
            return;
        }

        if (currentPage == MainScenePage.Shop)
        {
            return;
        }

        string message = args.Source switch
        {
            AdRewardSource.AdTicket => "获得广告券",
            _ => "广告奖励已发放",
        };
        battlePage?.SetStatus(message);
        battlePage?.OnResourceChanged();
    }

    /// <summary>将页面枚举映射为底栏动作。</summary>
    private static MainSceneAction MapPageToAction(MainScenePage page) =>
        page switch
        {
            MainScenePage.Shop => MainSceneAction.Shop,
            MainScenePage.Characters => MainSceneAction.Characters,
            MainScenePage.Tech => MainSceneAction.Tech,
            MainScenePage.Base => MainSceneAction.Base,
            _ => MainSceneAction.Battle,
        };

    /// <summary>播放 UI 点击音效。</summary>
    private static void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(null, sfxId);
        }
    }

    /// <summary>返回尚未接入功能的占位提示文案。</summary>
    private static string GetPendingNavMessage(MainSceneAction action) =>
        action switch
        {
            MainSceneAction.Characters => "角色入口已接入，待绑定角色养成面板",
            MainSceneAction.Tech => "科技入口已接入，待绑定科技树系统",
            MainSceneAction.Base => "基地入口已接入，待绑定基地系统",
            _ => "功能入口已点击",
        };
}
