using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单：启动防线、精英模式占位。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。PanelId = <see cref="GameConstants.UiPanelIds.MainMenu"/>。</para>
/// </remarks>
public class MainMenuPanelUI : UiPanelBase
{
    [Header("Actions")]
    [SerializeField] private Button deployButton;
    [SerializeField] private Button eliteModeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button signInButton;
    [SerializeField] private Button achievementButton;

    [Header("Red Dot")]
    [SerializeField] private GameObject signInRedDot;
    [SerializeField] private GameObject achievementRedDot;

    [Header("Display")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;

    /// <summary>绑定主菜单各功能按钮回调。</summary>
    private void Awake()
    {
        if (deployButton != null)
        {
            deployButton.onClick.AddListener(OnDeployClicked);
        }

        if (eliteModeButton != null)
        {
            eliteModeButton.onClick.AddListener(OnEliteClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopClicked);
        }

        if (signInButton != null)
        {
            signInButton.onClick.AddListener(OnSignInClicked);
        }

        if (achievementButton != null)
        {
            achievementButton.onClick.AddListener(OnAchievementClicked);
        }
    }

    /// <summary>订阅资源与签到/成就状态事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.SubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.SubscribeAchievementClaimed(OnAchievementClaimed);
    }

    /// <summary>取消主菜单事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.UnsubscribeAchievementProgressChanged(OnAchievementProgressChanged);
        GameEvents.UnsubscribeAchievementClaimed(OnAchievementClaimed);
    }

    /// <summary>显示时刷新 Meta 资源、红点与标题。</summary>
    protected override void OnShow()
    {
        RefreshMetaDisplay();
        RefreshRedDots();
        if (titleText != null)
        {
            titleText.text = "启动防线";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

    /// <summary>刷新金币、水晶与体力展示文案。</summary>
    public void RefreshMetaDisplay()
    {
        long gold = 0;
        long diamonds = 0;
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
            diamonds = resources.GetAmount(CurrencyType.Diamond);
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
            diamonds = save.Current.diamonds;
        }

        if (goldText != null)
        {
            goldText.text = gold.ToString();
            goldText.color = UiTechWastelandPalette.TextTerminal;
        }

        if (diamondText != null)
        {
            diamondText.text = diamonds.ToString();
            diamondText.color = UiTechWastelandPalette.PrimaryCyan;
        }

        if (staminaText != null)
        {
            staminaText.text = "STAMINA --";
            staminaText.color = UiTechWastelandPalette.TextSecondary;
        }
    }

    /// <summary>开始战斗按钮回调。</summary>
    private void OnDeployClicked()
    {
        PlayUiSfx("audio.sfx.ui_confirm");

        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.BeginRun(1, 1);
        }

        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.StartGame();
        }
    }

    /// <summary>精英模式入口按钮回调（占位）。</summary>
    private void OnEliteClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        Debug.Log("[MainMenuPanelUI] 精英模式入口待接 EliteController。");
    }

    /// <summary>设置按钮回调（占位）。</summary>
    private void OnSettingsClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        Debug.Log("[MainMenuPanelUI] 设置面板待扩展 ui.settings。");
    }

    /// <summary>打开商城面板。</summary>
    private void OnShopClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Shop);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanel(GameConstants.UiPanelIds.Shop);
        }
    }

    /// <summary>打开签到面板。</summary>
    private void OnSignInClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.SignIn);
        UIManager.Instance?.ShowPanel(GameConstants.UiPanelIds.SignIn);
    }

    /// <summary>打开成就面板。</summary>
    private void OnAchievementClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Achievement);
        UIManager.Instance?.ShowPanel(GameConstants.UiPanelIds.Achievement);
    }

    /// <summary>刷新签到与成就入口红点。</summary>
    private void RefreshRedDots()
    {
        bool canSignIn = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
        bool canClaimAchievement = ServiceLocator.TryGet(out AchievementManager achievements) &&
                                   achievements.HasClaimableRewards();

        if (signInRedDot != null)
        {
            signInRedDot.SetActive(canSignIn);
        }

        if (achievementRedDot != null)
        {
            achievementRedDot.SetActive(canClaimAchievement);
        }
    }

    /// <summary>资源变化时刷新 Meta 与红点。</summary>
    private void OnResourceChanged(GameEventContext ctx)
    {
        RefreshMetaDisplay();
        RefreshRedDots();
    }

    /// <summary>签到状态变化时刷新红点。</summary>
    private void OnDailyRewardStateChanged(GameEventContext ctx) => RefreshRedDots();

    /// <summary>成就进度变化时刷新红点。</summary>
    private void OnAchievementProgressChanged(GameEventContext ctx) => RefreshRedDots();

    /// <summary>成就领取后刷新 Meta 与红点。</summary>
    private void OnAchievementClaimed(GameEventContext ctx)
    {
        RefreshMetaDisplay();
        RefreshRedDots();
    }
}
