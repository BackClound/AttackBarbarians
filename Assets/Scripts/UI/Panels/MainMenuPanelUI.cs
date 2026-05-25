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

    [Header("Display")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;

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
    }

    private void OnEnable()
    {
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    protected override void OnShow()
    {
        RefreshMetaDisplay();
        if (titleText != null)
        {
            titleText.text = "启动防线";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

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

    private void OnEliteClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        Debug.Log("[MainMenuPanelUI] 精英模式入口待接 EliteController。");
    }

    private void OnSettingsClicked()
    {
        PlayUiSfx("audio.sfx.ui_click");
        Debug.Log("[MainMenuPanelUI] 设置面板待扩展 ui.settings。");
    }

    private void OnShopClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        GameEvents.RaiseUiPanelOpened(this, GameConstants.UiPanelIds.Shop);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanel(GameConstants.UiPanelIds.Shop);
        }
    }

    private void OnResourceChanged(GameEventContext ctx) => RefreshMetaDisplay();
}
