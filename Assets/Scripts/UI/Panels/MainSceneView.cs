using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MainSceneAction
{
    DailySignIn,
    NormalMode,
    EliteMode,
    GachaDraw,
    Achievements,
    StartGame,
    OnlineReward,
    OfflineReward,
    Shop,
    Characters,
    Battle,
    TechLab,
    Leaderboard,
    PreviousCommand,
    NextCommand,
}

/// <summary>
/// MainScene 主界面视图：按参考图动态生成竖屏大厅 UI、资源展示、按钮事件与系统回调。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂到 MainScene 的 <c>MainSceneUI</c> 物体即可。</para>
/// <para><b>场景搭建：</b>可通过菜单 Attack Barbarians/UI/Create MainScene 自动生成。</para>
/// </remarks>
public class MainSceneView : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private bool loadBattleSceneOnStart = true;

    [Header("Profile Fallback")]
    [SerializeField] private string playerName = "NovaX";
    [SerializeField] private int playerLevel = 28;
    [SerializeField] private int currentExp = 4560;
    [SerializeField] private int requiredExp = 9800;
    [SerializeField] private int currentEnergy = 126;
    [SerializeField] private int maxEnergy = 120;

    [Header("Build")]
    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField] private TMP_FontAsset fontOverride;
    [SerializeField] private Canvas canvas;

    public event Action<MainSceneAction> ActionClicked;

    private readonly Dictionary<MainSceneAction, Button> buttons = new Dictionary<MainSceneAction, Button>(16);
    private readonly List<GameObject> redDots = new List<GameObject>(8);

    private TMP_Text playerNameText;
    private TMP_Text levelText;
    private TMP_Text expText;
    private Image expFill;
    private TMP_Text diamondText;
    private TMP_Text goldText;
    private TMP_Text energyText;
    private TMP_Text commandText;
    private TMP_Text onlineRewardText;
    private TMP_Text offlineRewardText;
    private TMP_Text statusText;
    private GameObject signInRedDot;
    private GameObject achievementRedDot;
    private GameObject onlineRewardRedDot;
    private GameObject offlineRewardRedDot;

    private int commandLevel = 6;
    private bool isSubscribed;

    private static readonly Color BackgroundBottom = Hex("#101946");
    private static readonly Color NeonBlue = Hex("#50A7FF");
    private static readonly Color NeonPurple = Hex("#B65CFF");
    private static readonly Color NeonPink = Hex("#FF5CFF");
    private static readonly Color Gold = Hex("#FFCC5C");

    private void Awake()
    {
        if (buildOnAwake)
        {
            Build();
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

        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.UnsubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.UnsubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.UnsubscribeAchievementProgressChanged(OnAchievementStateChanged);
        GameEvents.UnsubscribeAchievementClaimed(OnAchievementStateChanged);
        GameEvents.UnsubscribeShopPurchased(OnShopPurchased);
        GameEvents.UnsubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        isSubscribed = false;
    }

    private void TrySubscribeEvents()
    {
        if (isSubscribed || !ServiceLocator.TryGet(out EventBus _))
        {
            return;
        }

        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeDailyRewardClaimed(OnDailyRewardClaimed);
        GameEvents.SubscribeDailyRewardClaimFailed(OnDailyRewardClaimFailed);
        GameEvents.SubscribeDailyRewardStateChanged(OnDailyRewardStateChanged);
        GameEvents.SubscribeAchievementProgressChanged(OnAchievementStateChanged);
        GameEvents.SubscribeAchievementClaimed(OnAchievementStateChanged);
        GameEvents.SubscribeShopPurchased(OnShopPurchased);
        GameEvents.SubscribeShopPurchaseFailed(OnShopPurchaseFailed);
        isSubscribed = true;
    }

    [ContextMenu("Rebuild MainScene UI")]
    public void Build()
    {
        buttons.Clear();
        redDots.Clear();

        canvas = ResolveCanvas();
        ClearGeneratedChildren(canvas.transform);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        CreateBackground(canvasRect);
        CreateTopBar(canvasRect);
        CreateFeatureRow(canvasRect);
        CreateCommandStage(canvasRect);
        CreateRewardPanels(canvasRect);
        CreateStartButton(canvasRect);
        CreateBottomNavigation(canvasRect);
        EnsureEventSystem();
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshProfile();
        RefreshCurrencies();
        RefreshRewardTimers();
        RefreshRedDots();
    }

    private Canvas ResolveCanvas()
    {
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = new GameObject("MainSceneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas newCanvas = canvasObject.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return newCanvas;
    }

    private static void ClearGeneratedChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreateBackground(RectTransform parent)
    {
        ImageWithOutline background = CreatePanel("SpaceBackground", parent, Vector2.zero, referenceResolution, BackgroundBottom);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        for (int i = 0; i < 36; i++)
        {
            float x = UnityEngine.Random.Range(-500f, 500f);
            float y = UnityEngine.Random.Range(-880f, 880f);
            float size = UnityEngine.Random.Range(3f, 9f);
            ImageWithOutline star = CreatePanel("Star", parent, new Vector2(x, y), new Vector2(size, size), Color.white);
            star.color = new Color(0.65f, 0.86f, 1f, UnityEngine.Random.Range(0.22f, 0.72f));
        }

        ImageWithOutline planet = CreatePanel("LeftPlanetGlow", parent, new Vector2(-550f, 270f), new Vector2(360f, 360f), new Color(0.25f, 0.45f, 1f, 0.18f));
        planet.transform.SetAsLastSibling();

        ImageWithOutline bottomGlow = CreatePanel("BottomPurpleGlow", parent, new Vector2(0f, -760f), new Vector2(880f, 420f), new Color(0.55f, 0.15f, 1f, 0.22f));
        bottomGlow.transform.SetAsLastSibling();
    }

    private void CreateTopBar(RectTransform parent)
    {
        ImageWithOutline avatar = CreatePanel("AvatarFrame", parent, new Vector2(-420f, 810f), new Vector2(170f, 170f), new Color(0.09f, 0.11f, 0.22f, 0.92f));
        avatar.outlineColor = NeonPurple;
        avatar.outlineWidth = 5f;
        CreateText("AvatarMark", avatar.rectTransform, "V", 72, NeonPurple, TextAlignmentOptions.Center, Vector2.zero, new Vector2(120f, 120f));

        playerNameText = CreateText("PlayerName", parent, playerName, 44, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Left, new Vector2(-300f, 850f), new Vector2(260f, 60f));
        levelText = CreateText("Level", parent, "28", 26, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Center, new Vector2(-465f, 735f), new Vector2(80f, 42f));

        ImageWithOutline expBar = CreatePanel("ExpBar", parent, new Vector2(-190f, 780f), new Vector2(330f, 18f), new Color(0.02f, 0.05f, 0.12f, 0.9f));
        expFill = CreatePanel("ExpFill", expBar.rectTransform, Vector2.zero, new Vector2(330f, 18f), NeonBlue);
        expFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        expFill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        expFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        expFill.rectTransform.offsetMin = Vector2.zero;
        expFill.rectTransform.offsetMax = Vector2.zero;
        expText = CreateText("ExpText", parent, string.Empty, 22, UiTechWastelandPalette.TextSecondary, TextAlignmentOptions.Left, new Vector2(-182f, 812f), new Vector2(300f, 36f));

        diamondText = CreateResourcePill(parent, "DiamondPill", new Vector2(280f, 850f), "12,450", NeonBlue);
        goldText = CreateResourcePill(parent, "GoldPill", new Vector2(540f, 850f), "78,630", Gold);
        energyText = CreateResourcePill(parent, "EnergyPill", new Vector2(420f, 780f), "126/120", NeonPurple);
    }

    private void CreateFeatureRow(RectTransform parent)
    {
        float y = 590f;
        float startX = -430f;
        float gap = 215f;

        signInRedDot = CreateFeatureButton(parent, MainSceneAction.DailySignIn, "✓", "DAILY\nSIGN-IN", new Vector2(startX, y), NeonBlue);
        CreateFeatureButton(parent, MainSceneAction.NormalMode, "◎", "NORMAL\nMODE", new Vector2(startX + gap, y), NeonBlue);
        CreateFeatureButton(parent, MainSceneAction.EliteMode, "◆", "ELITE\nMODE", new Vector2(startX + gap * 2f, y), NeonPurple);
        CreateFeatureButton(parent, MainSceneAction.GachaDraw, "◇", "GACHA\nDRAW", new Vector2(startX + gap * 3f, y), NeonPurple);
        achievementRedDot = CreateFeatureButton(parent, MainSceneAction.Achievements, "★", "ACHIEVEMENTS", new Vector2(startX + gap * 4f, y), Gold);
    }

    private void CreateCommandStage(RectTransform parent)
    {
        ImageWithOutline tower = CreatePanel("CommandTower", parent, new Vector2(0f, 35f), new Vector2(470f, 680f), new Color(0.03f, 0.08f, 0.20f, 0.62f));
        tower.outlineColor = new Color(0.35f, 0.75f, 1f, 0.55f);
        tower.outlineWidth = 3f;

        CreateText("TowerIcon", tower.rectTransform, "A", 160, NeonBlue, TextAlignmentOptions.Center, new Vector2(0f, 165f), new Vector2(260f, 210f));
        CreateText("TowerSubtitle", tower.rectTransform, "STELLAR COMMAND", 24, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Center, new Vector2(0f, -115f), new Vector2(300f, 50f));
        commandText = CreateText("CommandLevel", tower.rectTransform, "LEVEL 6", 22, NeonBlue, TextAlignmentOptions.Center, new Vector2(0f, -155f), new Vector2(240f, 44f));

        CreateArrowButton(parent, MainSceneAction.PreviousCommand, "<", new Vector2(-500f, 85f));
        CreateArrowButton(parent, MainSceneAction.NextCommand, ">", new Vector2(500f, 85f));
    }

    private void CreateRewardPanels(RectTransform parent)
    {
        ImageWithOutline online = CreatePanel("OnlineRewards", parent, new Vector2(-315f, -455f), new Vector2(340f, 265f), new Color(0.02f, 0.13f, 0.30f, 0.85f));
        online.outlineColor = NeonBlue;
        online.outlineWidth = 3f;
        CreateText("OnlineTitle", online.rectTransform, "ONLINE REWARDS", 24, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Center, new Vector2(0f, 88f), new Vector2(270f, 40f));
        CreateText("OnlineChest", online.rectTransform, "▣", 74, NeonBlue, TextAlignmentOptions.Center, new Vector2(-95f, -8f), new Vector2(120f, 120f));
        onlineRewardText = CreateText("OnlineTimer", online.rectTransform, "00:12:45\nCan be claimed!", 28, NeonBlue, TextAlignmentOptions.Center, new Vector2(45f, -48f), new Vector2(200f, 90f));
        onlineRewardRedDot = CreateRedDot(online.rectTransform, new Vector2(145f, 105f));
        AddInvisibleButton(online.gameObject, MainSceneAction.OnlineReward);

        ImageWithOutline offline = CreatePanel("OfflineRewards", parent, new Vector2(315f, -455f), new Vector2(340f, 265f), new Color(0.04f, 0.10f, 0.23f, 0.85f));
        offline.outlineColor = NeonBlue;
        offline.outlineWidth = 3f;
        CreateText("OfflineTitle", offline.rectTransform, "OFFLINE REWARDS", 24, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Center, new Vector2(0f, 88f), new Vector2(280f, 40f));
        CreateText("OfflineChest", offline.rectTransform, "▣", 74, Gold, TextAlignmentOptions.Center, new Vector2(-95f, -8f), new Vector2(120f, 120f));
        offlineRewardText = CreateText("OfflineTimer", offline.rectTransform, "08:00:00\nMax Rewards", 28, NeonBlue, TextAlignmentOptions.Center, new Vector2(45f, -48f), new Vector2(200f, 90f));
        offlineRewardRedDot = CreateRedDot(offline.rectTransform, new Vector2(145f, 105f));
        AddInvisibleButton(offline.gameObject, MainSceneAction.OfflineReward);
    }

    private void CreateStartButton(RectTransform parent)
    {
        Button startButton = CreateButton(parent, MainSceneAction.StartGame, "START\nGAME", new Vector2(0f, -500f), new Vector2(310f, 310f), NeonPink, 48);
        startButton.image.color = new Color(0.15f, 0.05f, 0.35f, 0.94f);

        ImageWithOutline ring = CreatePanel("StartButtonOuterGlow", startButton.transform as RectTransform, Vector2.zero, new Vector2(370f, 370f), new Color(0.6f, 0.2f, 1f, 0.16f));
        ring.transform.SetAsFirstSibling();

        statusText = CreateText("StatusText", parent, "选择模式，准备出击", 24, UiTechWastelandPalette.TextSecondary, TextAlignmentOptions.Center, new Vector2(0f, -285f), new Vector2(650f, 48f));
    }

    private void CreateBottomNavigation(RectTransform parent)
    {
        float y = -820f;
        float startX = -430f;
        float gap = 215f;
        CreateNavButton(parent, MainSceneAction.Shop, "SHOP", "□", new Vector2(startX, y));
        CreateNavButton(parent, MainSceneAction.Characters, "CHARACTERS", "◈", new Vector2(startX + gap, y));
        CreateNavButton(parent, MainSceneAction.Battle, "BATTLE", "⚔", new Vector2(startX + gap * 2f, y));
        CreateNavButton(parent, MainSceneAction.TechLab, "TECH LAB", "⌬", new Vector2(startX + gap * 3f, y));
        CreateNavButton(parent, MainSceneAction.Leaderboard, "LEADERBOARD", "⌂", new Vector2(startX + gap * 4f, y));
    }

    private TMP_Text CreateResourcePill(RectTransform parent, string name, Vector2 anchoredPosition, string text, Color accent)
    {
        ImageWithOutline panel = CreatePanel(name, parent, anchoredPosition, new Vector2(220f, 58f), new Color(0.04f, 0.07f, 0.18f, 0.92f));
        panel.outlineColor = new Color(accent.r, accent.g, accent.b, 0.7f);
        panel.outlineWidth = 2f;
        CreateText(name + "Icon", panel.rectTransform, "+", 26, accent, TextAlignmentOptions.Center, new Vector2(82f, 0f), new Vector2(40f, 40f));
        return CreateText(name + "Text", panel.rectTransform, text, 26, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.MidlineLeft, new Vector2(-20f, 0f), new Vector2(145f, 42f));
    }

    private GameObject CreateFeatureButton(RectTransform parent, MainSceneAction action, string icon, string label, Vector2 position, Color accent)
    {
        Button button = CreateButton(parent, action, label, position, new Vector2(185f, 205f), accent, 24);
        CreateText(action + "Icon", button.transform as RectTransform, icon, 62, accent, TextAlignmentOptions.Center, new Vector2(0f, 38f), new Vector2(110f, 86f));
        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>();
        if (labelText != null)
        {
            labelText.rectTransform.anchoredPosition = new Vector2(0f, -52f);
            labelText.rectTransform.sizeDelta = new Vector2(160f, 80f);
        }

        return CreateRedDot(button.transform as RectTransform, new Vector2(78f, 83f));
    }

    private void CreateArrowButton(RectTransform parent, MainSceneAction action, string label, Vector2 position)
    {
        Button button = CreateButton(parent, action, label, position, new Vector2(84f, 100f), NeonBlue, 54);
        button.image.color = new Color(0.02f, 0.08f, 0.22f, 0.55f);
    }

    private void CreateNavButton(RectTransform parent, MainSceneAction action, string label, string icon, Vector2 position)
    {
        Button button = CreateButton(parent, action, label, position, new Vector2(178f, 150f), action == MainSceneAction.Battle ? NeonPurple : NeonBlue, 22);
        CreateText(action + "Icon", button.transform as RectTransform, icon, 48, action == MainSceneAction.Battle ? NeonPurple : NeonBlue, TextAlignmentOptions.Center, new Vector2(0f, 30f), new Vector2(100f, 58f));
        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>();
        if (labelText != null)
        {
            labelText.rectTransform.anchoredPosition = new Vector2(0f, -47f);
            labelText.rectTransform.sizeDelta = new Vector2(150f, 42f);
        }
    }

    private Button CreateButton(RectTransform parent, MainSceneAction action, string label, Vector2 anchoredPosition, Vector2 size, Color accent, int fontSize)
    {
        ImageWithOutline panel = CreatePanel(action.ToString(), parent, anchoredPosition, size, new Color(0.04f, 0.09f, 0.23f, 0.9f));
        panel.outlineColor = new Color(accent.r, accent.g, accent.b, 0.78f);
        panel.outlineWidth = 3f;
        Image image = panel;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.72f, 0.78f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(() => HandleAction(action));
        buttons[action] = button;

        CreateText(action + "Label", image.rectTransform, label, fontSize, UiTechWastelandPalette.TextPrimary, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(20f, 20f));
        return button;
    }

    private void AddInvisibleButton(GameObject target, MainSceneAction action)
    {
        Button button = target.AddComponent<Button>();
        button.targetGraphic = target.GetComponent<Graphic>();
        button.onClick.AddListener(() => HandleAction(action));
        buttons[action] = button;
    }

    private ImageWithOutline CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.35f);
        outline.effectDistance = new Vector2(2f, -2f);

        return new ImageWithOutline(image, outline);
    }

    private TMP_Text CreateText(string name, RectTransform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        if (fontOverride != null)
        {
            label.font = fontOverride;
        }

        return label;
    }

    private GameObject CreateRedDot(RectTransform parent, Vector2 anchoredPosition)
    {
        ImageWithOutline dot = CreatePanel("RedDot", parent, anchoredPosition, new Vector2(26f, 26f), UiTechWastelandPalette.DangerRed);
        dot.outlineColor = Color.white;
        dot.outlineWidth = 2f;
        redDots.Add(dot.gameObject);
        return dot.gameObject;
    }

    private void RefreshProfile()
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (levelText != null)
        {
            levelText.text = playerLevel.ToString(CultureInfo.InvariantCulture);
        }

        if (expText != null)
        {
            expText.text = $"EXP {currentExp} / {requiredExp}";
        }

        if (expFill != null)
        {
            float ratio = requiredExp > 0 ? Mathf.Clamp01((float)currentExp / requiredExp) : 0f;
            expFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        }

        if (commandText != null)
        {
            commandText.text = $"LEVEL {commandLevel}";
        }
    }

    private void RefreshCurrencies()
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

        if (diamondText != null)
        {
            diamondText.text = FormatNumber(diamonds);
        }

        if (goldText != null)
        {
            goldText.text = FormatNumber(gold);
        }

        if (energyText != null)
        {
            energyText.text = $"{currentEnergy}/{maxEnergy}";
        }
    }

    private void RefreshRewardTimers()
    {
        if (onlineRewardText != null)
        {
            bool canClaim = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
            onlineRewardText.text = canClaim ? "00:00:00\nCan be claimed!" : "00:12:45\nNext reward";
        }

        if (offlineRewardText != null)
        {
            offlineRewardText.text = "08:00:00\nMax Rewards";
        }
    }

    private void RefreshRedDots()
    {
        bool canSignIn = ServiceLocator.TryGet(out DailyRewardManager daily) && daily.CanClaimToday();
        bool canClaimAchievement = ServiceLocator.TryGet(out AchievementManager achievements) && achievements.HasClaimableRewards();
        bool canClaimFreeDiamond = ServiceLocator.TryGet(out ShopManager shop) && shop.CanClaimFreeDiamond(out _, out _);

        SetActive(signInRedDot, canSignIn);
        SetActive(achievementRedDot, canClaimAchievement);
        SetActive(onlineRewardRedDot, canSignIn);
        SetActive(offlineRewardRedDot, canClaimFreeDiamond);
    }

    private void HandleAction(MainSceneAction action)
    {
        ActionClicked?.Invoke(action);

        switch (action)
        {
            case MainSceneAction.StartGame:
            case MainSceneAction.NormalMode:
            case MainSceneAction.Battle:
                PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
                BeginBattle(eliteMode: false);
                break;

            case MainSceneAction.EliteMode:
                PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
                BeginBattle(eliteMode: true);
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

            case MainSceneAction.PreviousCommand:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                commandLevel = Mathf.Max(1, commandLevel - 1);
                RefreshProfile();
                SetStatus($"已切换至指挥中心 Lv.{commandLevel}");
                break;

            case MainSceneAction.NextCommand:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                commandLevel++;
                RefreshProfile();
                SetStatus($"已切换至指挥中心 Lv.{commandLevel}");
                break;

            default:
                PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
                SetStatus(GetPendingFeatureMessage(action));
                Debug.Log($"[MainSceneView] {action} clicked.");
                break;
        }
    }

    private void BeginBattle(bool eliteMode)
    {
        RunDifficultyContext.IsEliteMode = eliteMode;
        if (RunDifficultyContext.EliteConfig == null)
        {
            RunDifficultyContext.EliteConfig = Resources.Load<EliteModeConfigSO>(GameConstants.ResourcePaths.EliteModeConfig);
        }

        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.BeginRun(1, 1);
            save.SaveImmediate();
        }

        SetStatus(eliteMode ? "精英模式启动，正在进入战场..." : "普通模式启动，正在进入战场...");

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
            SetStatus("离线奖励系统未就绪");
            return;
        }

        if (!shop.TryClaimFreeDiamond())
        {
            shop.CanClaimFreeDiamond(out string reason, out _);
            SetStatus(reason);
        }

        RefreshAll();
    }

    private void OnResourceChanged(GameEventContext ctx) => RefreshAll();

    private void OnDailyRewardStateChanged(GameEventContext ctx) => RefreshAll();

    private void OnAchievementStateChanged(GameEventContext ctx) => RefreshAll();

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
            statusText.color = string.IsNullOrWhiteSpace(message)
                ? UiTechWastelandPalette.TextSecondary
                : UiTechWastelandPalette.TextPrimary;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static string GetPendingFeatureMessage(MainSceneAction action)
    {
        return action switch
        {
            MainSceneAction.GachaDraw => "抽卡入口已接入按钮回调，待接入卡池系统",
            MainSceneAction.Achievements => "成就入口已接入，可继续绑定成就详情面板",
            MainSceneAction.Shop => "商城入口已接入，可继续绑定商城面板",
            MainSceneAction.Characters => "角色入口已接入，可继续绑定角色养成面板",
            MainSceneAction.TechLab => "科技实验室入口已接入，可继续绑定科技树系统",
            MainSceneAction.Leaderboard => "排行榜入口已接入，可继续绑定排行服务",
            _ => "功能入口已点击",
        };
    }

    private static string FormatNumber(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            eventSystemObject.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }

    private readonly struct ImageWithOutline
    {
        private readonly Image image;
        private readonly Outline outline;

        public ImageWithOutline(Image image, Outline outline)
        {
            this.image = image;
            this.outline = outline;
        }

        public RectTransform rectTransform => image.rectTransform;
        public GameObject gameObject => image.gameObject;
        public Transform transform => image.transform;
        public Color color
        {
            get => image.color;
            set => image.color = value;
        }

        public Color outlineColor
        {
            set => outline.effectColor = value;
        }

        public float outlineWidth
        {
            set => outline.effectDistance = new Vector2(value, -value);
        }

        public static implicit operator Image(ImageWithOutline value) => value.image;
    }
}
