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

    public event Action<MainSceneAction> ActionClicked;

    private bool isSubscribed;

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
        bottomNavCardPanel?.SetNavTabSelected(MapPageToAction(currentPage));

        if (currentPage == MainScenePage.Shop)
        {
            shopPage?.RefreshAll();
            return;
        }

        battlePage?.RefreshAll();
    }

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

    private void OnGameEventRefresh(GameEventContext ctx)
    {
        battlePage?.OnResourceChanged();
        if (currentPage == MainScenePage.Shop)
        {
            shopPage?.RefreshAll();
        }
    }

    private void OnDailyRewardClaimed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimedEventArgs args)
        {
            battlePage?.OnDailyRewardClaimed(args);
        }
    }

    private void OnDailyRewardClaimFailed(GameEventContext ctx)
    {
        if (ctx.Payload is DailyRewardClaimFailedEventArgs args)
        {
            battlePage?.OnDailyRewardClaimFailed(args.Message);
        }
    }

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

    private static MainSceneAction MapPageToAction(MainScenePage page) =>
        page switch
        {
            MainScenePage.Shop => MainSceneAction.Shop,
            MainScenePage.Characters => MainSceneAction.Characters,
            MainScenePage.Tech => MainSceneAction.Tech,
            MainScenePage.Base => MainSceneAction.Base,
            _ => MainSceneAction.Battle,
        };

    private static void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(null, sfxId);
        }
    }

    private static string GetPendingNavMessage(MainSceneAction action) =>
        action switch
        {
            MainSceneAction.Characters => "角色入口已接入，待绑定角色养成面板",
            MainSceneAction.Tech => "科技入口已接入，待绑定科技树系统",
            MainSceneAction.Base => "基地入口已接入，待绑定基地系统",
            _ => "功能入口已点击",
        };
}
