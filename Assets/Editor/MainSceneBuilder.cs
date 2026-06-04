using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainSceneBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";

    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Color PanelBlue = Hex("#0C2A5A");
    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonOrange = Hex("#FF7A24");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color NeonPurple = Hex("#A95CFF");
    private static readonly Color TextWhite = Hex("#F5FAFF");

    [MenuItem("Attack Barbarians/UI/Create MainScene")]
    public static void CreateMainScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";

        CreateCamera();
        CreateEventSystem();
        CreateGameSystems();
        CreateMainSceneUi();

        EditorSceneManager.SaveScene(scene, MainScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.Refresh();

        Debug.Log($"[MainSceneBuilder] MainScene created: {MainScenePath}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.08f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 9.6f;
    }

    private static void CreateEventSystem()
    {
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

    private static void CreateGameSystems()
    {
        GameObject systemsObject = new GameObject("GameSystems");
        GameBootstrapper bootstrapper = systemsObject.AddComponent<GameBootstrapper>();

        SerializedObject serializedBootstrapper = new SerializedObject(bootstrapper);
        serializedBootstrapper.FindProperty("initializeOnAwake").boolValue = true;
        serializedBootstrapper.FindProperty("dontDestroyOnLoad").boolValue = false;
        serializedBootstrapper.FindProperty("postBootstrapFlow").enumValueIndex = (int)BootstrapPostFlow.OpenMainMenu;
        serializedBootstrapper.FindProperty("managerSet").enumValueIndex = (int)BootstrapManagerSet.MainScene;
        serializedBootstrapper.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateMainSceneUi()
    {
        GameObject canvasObject = new GameObject(
            "MainSceneUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(UiTmpChineseFontBootstrap),
            typeof(MainSceneView));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();

        GameObject battleRootGo = new GameObject("BattlePagePanelRoot", typeof(RectTransform), typeof(MainSceneBattlePageView));
        battleRootGo.transform.SetParent(root, false);
        StretchToParent(battleRootGo.GetComponent<RectTransform>());
        MainSceneBattlePageView battlePage = battleRootGo.GetComponent<MainSceneBattlePageView>();

        Image background = CreatePanel(battleRootGo.transform as RectTransform, "SpaceCityBackground", Vector2.zero, ReferenceResolution, Hex("#06112F"));
        StretchToParent(background.rectTransform);

        GameObject safeAreaGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(UiSafeAreaFitter), typeof(UiRectLayout));
        safeAreaGo.transform.SetParent(battleRootGo.transform, false);
        StretchToParent(safeAreaGo.GetComponent<RectTransform>());
        UiPageStructureEditorUtility.SetLayout(
            safeAreaGo.GetComponent<UiRectLayout>(),
            UiRectLayout.LayoutPreset.FullStretch,
            new RectOffset(0, 0, 0, UiPageStructureEditorUtility.SafeAreaBottomInset));
        RectTransform safeArea = safeAreaGo.GetComponent<RectTransform>();

        RectTransform profileRoot = CreatePanelHost(safeArea, "TopProfile", Vector2.zero, Vector2.zero);
        MainSceneProfilePanel profilePanel = profileRoot.gameObject.AddComponent<MainSceneProfilePanel>();
        CreateProfile(profileRoot, profilePanel);

        RectTransform resourceRoot = CreatePanelHost(safeArea, "TopResources", Vector2.zero, Vector2.zero);
        MainSceneResourcePanel resourcePanel = resourceRoot.gameObject.AddComponent<MainSceneResourcePanel>();
        CreateTopResources(resourceRoot, resourcePanel);

        MainSceneCardPanel topSystemPanel = CreateCardPanelHost(safeArea, "TopSystemIcons");
        CreateTopButtons(topSystemPanel);

        MainSceneCardPanel promotionPanel = CreateCardPanelHost(safeArea, "PromotionIcons");
        CreatePromotionArea(promotionPanel);

        CreateCenterVisual(safeArea);

        MainSceneCardPanel leftPanel = CreateCardPanelHost(safeArea, "LeftSideIcons");
        CreateLeftSideButtons(leftPanel);

        MainSceneCardPanel rightPanel = CreateCardPanelHost(safeArea, "RightSideIcons");
        CreateRightSideButtons(rightPanel);

        RectTransform rewardRoot = CreatePanelHost(safeArea, "RewardRow", Vector2.zero, Vector2.zero);
        MainSceneRewardPanel rewardPanel = rewardRoot.gameObject.AddComponent<MainSceneRewardPanel>();
        CreateRewardPanels(rewardRoot, rewardPanel);

        GeneralCardPanel startBattle = CreatePrimaryAction(safeArea);

        TMP_Text status = CreateText(safeArea, "StatusText", "准备开始战斗", 24, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -610f), new Vector2(720f, 48f));

        MainSceneCardPanel bottomPanel = CreateCardPanelHost(root, "BottomNav");
        CreateBottomNavigation(bottomPanel);
        UiPageStructureEditorUtility.PinBottomNavToCanvas(root, bottomPanel.transform);

        SerializedObject battleSo = new SerializedObject(battlePage);
        battleSo.FindProperty("battleSceneName").stringValue = "BattleScene";
        battleSo.FindProperty("loadBattleSceneOnStart").boolValue = true;
        battleSo.FindProperty("backgroundImage").objectReferenceValue = background;
        battleSo.FindProperty("statusText").objectReferenceValue = status;
        battleSo.FindProperty("profilePanel").objectReferenceValue = profilePanel;
        battleSo.FindProperty("resourcePanel").objectReferenceValue = resourcePanel;
        battleSo.FindProperty("topSystemCardPanel").objectReferenceValue = topSystemPanel;
        battleSo.FindProperty("promotionCardPanel").objectReferenceValue = promotionPanel;
        battleSo.FindProperty("leftSideCardPanel").objectReferenceValue = leftPanel;
        battleSo.FindProperty("rightSideCardPanel").objectReferenceValue = rightPanel;
        battleSo.FindProperty("startBattleCard").objectReferenceValue = startBattle;
        battleSo.FindProperty("rewardPanel").objectReferenceValue = rewardPanel;
        battleSo.ApplyModifiedPropertiesWithoutUndo();

        MainSceneView view = canvasObject.GetComponent<MainSceneView>();
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("canvas").objectReferenceValue = canvas;
        serializedView.FindProperty("battlePage").objectReferenceValue = battlePage;
        serializedView.FindProperty("bottomNavCardPanel").objectReferenceValue = bottomPanel;
        serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateProfile(RectTransform root, MainSceneProfilePanel profilePanel)
    {
        Image avatarFrame = CreatePanel(root, "AvatarFrame", new Vector2(-440f, 810f), new Vector2(150f, 150f), PanelDeep);
        Image avatar = CreatePanel(avatarFrame.rectTransform, "AvatarImage", Vector2.zero, new Vector2(124f, 124f), Hex("#1F5FC8"));
        TMP_Text level = CreateText(root, "PlayerLevelText", "60", 24, TextWhite, TextAlignmentOptions.Center, new Vector2(-500f, 725f), new Vector2(70f, 36f));
        TMP_Text playerName = CreateText(root, "PlayerNameText", "涛王不可", 36, TextWhite, TextAlignmentOptions.Left, new Vector2(-270f, 850f), new Vector2(300f, 52f));
        TMP_Text vip = CreateText(root, "VipText", "VIP6", 24, NeonGold, TextAlignmentOptions.Left, new Vector2(-300f, 810f), new Vector2(160f, 40f));
        Slider exp = CreateSlider(root, "ExpSlider", new Vector2(-170f, 775f), new Vector2(360f, 18f), NeonBlue);
        TMP_Text expText = CreateText(root, "ExpText", "EXP 4560 / 9800", 18, Hex("#9EC8FF"), TextAlignmentOptions.Left, new Vector2(-170f, 800f), new Vector2(300f, 28f));

        SerializedObject serialized = new SerializedObject(profilePanel);
        serialized.FindProperty("avatarImage").objectReferenceValue = avatar;
        serialized.FindProperty("playerNameText").objectReferenceValue = playerName;
        serialized.FindProperty("vipText").objectReferenceValue = vip;
        serialized.FindProperty("playerLevelText").objectReferenceValue = level;
        serialized.FindProperty("expSlider").objectReferenceValue = exp;
        serialized.FindProperty("expText").objectReferenceValue = expText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTopResources(RectTransform root, MainSceneResourcePanel resourcePanel)
    {
        UI_ItemSlot diamond = CreateResourcePill(root, "DiamondResource", new Vector2(-135f, 760f), "120", NeonBlue, CurrencyType.Diamond);
        UI_ItemSlot gold = CreateResourcePill(root, "GoldResource", new Vector2(120f, 760f), "1200", NeonGold, CurrencyType.Gold);
        UI_ItemSlot energy = CreateResourcePill(root, "EnergyResource", new Vector2(370f, 760f), "30/30", Hex("#FF5E7E"), CurrencyType.Energy);
        UI_ItemSlot ticket = CreateResourcePill(root, "TicketResource", new Vector2(570f, 760f), "85", Hex("#5BE7FF"), CurrencyType.Diamond);

        SerializedObject serialized = new SerializedObject(resourcePanel);
        serialized.FindProperty("diamondSlot").objectReferenceValue = diamond;
        serialized.FindProperty("goldSlot").objectReferenceValue = gold;
        serialized.FindProperty("energySlot").objectReferenceValue = energy;
        serialized.FindProperty("ticketSlot").objectReferenceValue = ticket;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTopButtons(MainSceneCardPanel cardPanel)
    {
        GeneralCardPanel[] cards =
        {
            CreateIconButton(cardPanel.transform as RectTransform, "MailButton", new Vector2(780f, 850f), new Vector2(72f, 72f), "信", string.Empty, NeonBlue, MainSceneAction.Mail),
            CreateIconButton(cardPanel.transform as RectTransform, "SocialButton", new Vector2(870f, 850f), new Vector2(72f, 72f), "友", string.Empty, NeonBlue, MainSceneAction.Social),
            CreateIconButton(cardPanel.transform as RectTransform, "SettingsButton", new Vector2(960f, 850f), new Vector2(72f, 72f), "设", string.Empty, NeonBlue, MainSceneAction.Settings),
        };
        AssignCardPanel(cardPanel, cards);
    }

    private static void CreatePromotionArea(MainSceneCardPanel cardPanel)
    {
        RectTransform parent = cardPanel.transform as RectTransform;
        var cards = new List<GeneralCardPanel>(8)
        {
            CreateIconButton(parent, "SignInBannerButton", new Vector2(-300f, 590f), new Vector2(430f, 135f), "七日登录", "送S级英雄", NeonPurple, MainSceneAction.DailySignIn),
        };

        MainSceneAction[] actions = { MainSceneAction.FirstCharge, MainSceneAction.MonthlyCard, MainSceneAction.LimitedEvent, MainSceneAction.NewbieWelfare };
        string[] names = { "firstChargeButton", "monthlyCardButton", "limitedEventButton", "newbieWelfareButton" };
        string[] titles = { "首充", "月卡", "限时活动", "新人福利" };
        for (int i = 0; i < actions.Length; i++)
        {
            cards.Add(CreateIconButton(parent, names[i], new Vector2(410f + i * 135f, 600f), new Vector2(118f, 118f), titles[i], string.Empty, NeonGold, actions[i]));
        }

        AssignCardPanel(cardPanel, cards.ToArray());
    }

    private static void CreateCenterVisual(RectTransform root)
    {
        Image skyline = CreatePanel(root, "CentralSkylinePanel", new Vector2(0f, 115f), new Vector2(520f, 560f), new Color(0.02f, 0.12f, 0.32f, 0.46f));
        CreatePanel(skyline.rectTransform, "EnergyCoreGlow", new Vector2(0f, -50f), new Vector2(260f, 260f), new Color(0.08f, 0.6f, 1f, 0.42f));
        CreatePanel(skyline.rectTransform, "EnergyCore", new Vector2(0f, -50f), new Vector2(145f, 145f), NeonBlue);
        CreateText(skyline.rectTransform, "CityTowerText", "STELLAR CORE", 28, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 205f), new Vector2(300f, 42f));
        CreateText(skyline.rectTransform, "FlightShipText", ">>", 54, NeonBlue, TextAlignmentOptions.Center, new Vector2(210f, 115f), new Vector2(120f, 72f));
    }

    private static void CreateLeftSideButtons(MainSceneCardPanel cardPanel)
    {
        RectTransform parent = cardPanel.transform as RectTransform;
        GeneralCardPanel[] cards =
        {
            CreateIconButton(parent, "ActivityCenterButton", new Vector2(-455f, 385f), new Vector2(130f, 130f), "活动\n中心", string.Empty, NeonBlue, MainSceneAction.ActivityCenter),
            CreateIconButton(parent, "DailyTaskButton", new Vector2(-455f, 220f), new Vector2(130f, 130f), "每日\n任务", string.Empty, NeonBlue, MainSceneAction.DailyTask),
            CreateIconButton(parent, "DailySignInButton", new Vector2(-455f, 55f), new Vector2(130f, 130f), "签到\n奖励", string.Empty, NeonBlue, MainSceneAction.DailySignIn),
            CreateIconButton(parent, "LuckyDrawButton", new Vector2(-455f, -110f), new Vector2(130f, 130f), "幸运\n抽奖", string.Empty, NeonPurple, MainSceneAction.LuckyDraw),
            CreateIconButton(parent, "AchievementsButton", new Vector2(-455f, -275f), new Vector2(130f, 130f), "成就\n系统", string.Empty, NeonGold, MainSceneAction.Achievements),
        };
        AssignCardPanel(cardPanel, cards);
    }

    private static void CreateRightSideButtons(MainSceneCardPanel cardPanel)
    {
        RectTransform parent = cardPanel.transform as RectTransform;
        GeneralCardPanel[] cards =
        {
            CreateIconButton(parent, "AnnouncementButton", new Vector2(455f, 130f), new Vector2(130f, 130f), "公告", string.Empty, NeonBlue, MainSceneAction.Announcement),
            CreateIconButton(parent, "RankingButton", new Vector2(455f, -35f), new Vector2(130f, 130f), "排行榜", string.Empty, NeonGold, MainSceneAction.Ranking),
            CreateIconButton(parent, "ValuePackButton", new Vector2(455f, -200f), new Vector2(130f, 130f), "超值\n礼包", string.Empty, NeonPurple, MainSceneAction.ValuePack),
        };
        AssignCardPanel(cardPanel, cards);
    }

    private static void CreateRewardPanels(RectTransform root, MainSceneRewardPanel rewardPanel)
    {
        GeneralRewardCardPanel online = CreateRewardPanel(root, "OnlineRewardPanel", new Vector2(-245f, -460f), "在线奖励", "00:15:30", "可领取", NeonBlue, MainSceneAction.OnlineReward);
        GeneralRewardCardPanel stage = CreateRewardPanel(root, "StageRewardPanel", new Vector2(0f, -460f), "通关奖励", string.Empty, "可领取", NeonOrange, MainSceneAction.StageReward);
        GeneralRewardCardPanel offline = CreateRewardPanel(root, "OfflineRewardPanel", new Vector2(245f, -460f), "离线收益", "02:30:45", "累计中", NeonGold, MainSceneAction.OfflineReward);

        SerializedObject serialized = new SerializedObject(rewardPanel);
        serialized.FindProperty("onlineReward").objectReferenceValue = online;
        serialized.FindProperty("stageReward").objectReferenceValue = stage;
        serialized.FindProperty("offlineReward").objectReferenceValue = offline;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GeneralCardPanel CreatePrimaryAction(RectTransform root)
    {
        GeneralCardPanel start = CreateIconButton(root, "StartBattleButton", new Vector2(0f, -675f), new Vector2(720f, 170f), "开始战斗", "消耗 5", NeonOrange, MainSceneAction.StartBattle);
        SerializedObject serialized = new SerializedObject(start);
        TMP_Text title = serialized.FindProperty("titleText").objectReferenceValue as TMP_Text;
        TMP_Text subtitle = serialized.FindProperty("subtitleText").objectReferenceValue as TMP_Text;
        if (title != null)
        {
            title.fontSize = 58;
        }

        if (subtitle != null)
        {
            subtitle.fontSize = 22;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return start;
    }

    private static void CreateBottomNavigation(MainSceneCardPanel cardPanel)
    {
        MainSceneAction[] actions = { MainSceneAction.Shop, MainSceneAction.Characters, MainSceneAction.Battle, MainSceneAction.Tech, MainSceneAction.Base };
        string[] titles = { "商城", "角色", "战斗", "科技", "基地" };
        RectTransform parent = cardPanel.transform as RectTransform;
        var cards = new GeneralCardPanel[actions.Length];
        for (int i = 0; i < actions.Length; i++)
        {
            Color accent = actions[i] == MainSceneAction.Battle ? NeonOrange : NeonBlue;
            cards[i] = CreateIconButton(parent, actions[i] + "NavButton", new Vector2(-420f + i * 210f, -855f), new Vector2(180f, 140f), titles[i], string.Empty, accent, actions[i]);
        }

        AssignCardPanel(cardPanel, cards);
    }

    private static MainSceneCardPanel CreateCardPanelHost(RectTransform parent, string name)
    {
        RectTransform host = CreatePanelHost(parent, name, Vector2.zero, Vector2.zero);
        return host.gameObject.AddComponent<MainSceneCardPanel>();
    }

    private static void AssignCardPanel(MainSceneCardPanel panel, GeneralCardPanel[] cards)
    {
        SerializedObject serialized = new SerializedObject(panel);
        SerializedProperty array = serialized.FindProperty("cards");
        array.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform CreatePanelHost(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreatePanel(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
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
        outline.effectColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);
        return image;
    }

    private static TMP_Text CreateText(RectTransform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size)
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
        label.raycastTarget = false;

        TMP_FontAsset chineseFont = UiChineseTmpFontBuilder.CreateOrLoadChineseFontAsset();
        if (chineseFont != null)
        {
            label.font = chineseFont;
        }

        return label;
    }

    private static Slider CreateSlider(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image background = CreatePanel(rect, "Background", Vector2.zero, size, Hex("#03102A"));
        Image fill = CreatePanel(rect, "Fill", Vector2.zero, size, fillColor);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        Slider slider = go.GetComponent<Slider>();
        slider.targetGraphic = background;
        slider.fillRect = fill.rectTransform;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 45f;
        return slider;
    }

    private static UI_ItemSlot CreateResourcePill(RectTransform parent, string name, Vector2 position, string value, Color accent, CurrencyType currency)
    {
        Image panel = CreatePanel(parent, name, position, new Vector2(205f, 58f), PanelDeep);
        Button add = panel.gameObject.AddComponent<Button>();
        Image icon = CreatePanel(panel.rectTransform, "Icon", new Vector2(-72f, 0f), new Vector2(42f, 42f), accent);
        TMP_Text valueText = CreateText(panel.rectTransform, "ValueText", value, 25, TextWhite, TextAlignmentOptions.Left, new Vector2(5f, 0f), new Vector2(110f, 42f));
        CreateText(panel.rectTransform, "AddText", "+", 30, NeonBlue, TextAlignmentOptions.Center, new Vector2(78f, 0f), new Vector2(34f, 42f));

        UI_ItemSlot slot = panel.gameObject.AddComponent<UI_ItemSlot>();
        SerializedObject serialized = new SerializedObject(slot);
        serialized.FindProperty("currency").enumValueIndex = (int)currency;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("valueText").objectReferenceValue = valueText;
        serialized.FindProperty("addButton").objectReferenceValue = add;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    private static GeneralCardPanel CreateIconButton(RectTransform parent, string name, Vector2 position, Vector2 size, string title, string subtitle, Color accent, MainSceneAction action)
    {
        Image background = CreatePanel(parent, name, position, size, PanelBlue);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Image icon = CreatePanel(background.rectTransform, "IconImage", new Vector2(0f, size.y * 0.18f), new Vector2(Mathf.Min(64f, size.x * 0.35f), Mathf.Min(64f, size.y * 0.35f)), accent);
        TMP_Text titleText = CreateText(background.rectTransform, "TitleText", title, 24, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -size.y * 0.18f), new Vector2(size.x - 18f, size.y * 0.42f));
        TMP_Text subtitleText = CreateText(background.rectTransform, "SubtitleText", subtitle, 20, NeonGold, TextAlignmentOptions.Center, new Vector2(0f, -size.y * 0.36f), new Vector2(size.x - 18f, size.y * 0.28f));
        UI_RedDot redDot = CreateRedDot(background.rectTransform, new Vector2(size.x * 0.40f, size.y * 0.38f));

        GeneralCardPanel card = background.gameObject.AddComponent<GeneralCardPanel>();
        SerializedObject serialized = new SerializedObject(card);
        serialized.FindProperty("action").enumValueIndex = (int)action;
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.FindProperty("backgroundImage").objectReferenceValue = background;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("titleText").objectReferenceValue = titleText;
        serialized.FindProperty("subtitleText").objectReferenceValue = subtitleText;
        serialized.FindProperty("costText").objectReferenceValue = subtitleText;
        serialized.FindProperty("redDot").objectReferenceValue = redDot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return card;
    }

    private static GeneralRewardCardPanel CreateRewardPanel(RectTransform parent, string name, Vector2 position, string title, string timer, string status, Color accent, MainSceneAction action)
    {
        Image background = CreatePanel(parent, name, position, new Vector2(225f, 105f), PanelDeep);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        Image icon = CreatePanel(background.rectTransform, "IconImage", new Vector2(-72f, 5f), new Vector2(60f, 60f), accent);
        TMP_Text titleText = CreateText(background.rectTransform, "TitleText", title, 22, TextWhite, TextAlignmentOptions.Center, new Vector2(28f, 25f), new Vector2(130f, 30f));
        TMP_Text timerText = CreateText(background.rectTransform, "TimerText", timer, 20, Hex("#65FF9A"), TextAlignmentOptions.Center, new Vector2(28f, -6f), new Vector2(130f, 30f));
        TMP_Text statusText = CreateText(background.rectTransform, "StatusText", status, 18, NeonGold, TextAlignmentOptions.Center, new Vector2(28f, -34f), new Vector2(130f, 28f));
        UI_RedDot redDot = CreateRedDot(background.rectTransform, new Vector2(95f, 38f));

        GeneralRewardCardPanel reward = background.gameObject.AddComponent<GeneralRewardCardPanel>();
        SerializedObject serialized = new SerializedObject(reward);
        serialized.FindProperty("action").enumValueIndex = (int)action;
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.FindProperty("backgroundImage").objectReferenceValue = background;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("titleText").objectReferenceValue = titleText;
        serialized.FindProperty("timerText").objectReferenceValue = timerText;
        serialized.FindProperty("statusText").objectReferenceValue = statusText;
        serialized.FindProperty("redDot").objectReferenceValue = redDot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return reward;
    }

    private static UI_RedDot CreateRedDot(RectTransform parent, Vector2 position)
    {
        Image dot = CreatePanel(parent, "RedDot", position, new Vector2(22f, 22f), Hex("#FF2238"));
        UI_RedDot redDot = dot.gameObject.AddComponent<UI_RedDot>();
        SerializedObject serialized = new SerializedObject(redDot);
        serialized.FindProperty("dotRoot").objectReferenceValue = dot.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return redDot;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(scene => scene != null && !string.IsNullOrWhiteSpace(scene.path))
            .ToList();

        UpsertScene(scenes, MainScenePath, insertAtStart: true);
        UpsertScene(scenes, BattleScenePath, insertAtStart: false);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void UpsertScene(List<EditorBuildSettingsScene> scenes, string path, bool insertAtStart)
    {
        int existingIndex = scenes.FindIndex(scene => scene.path == path);
        if (existingIndex >= 0)
        {
            scenes[existingIndex].enabled = true;
            if (insertAtStart && existingIndex != 0)
            {
                EditorBuildSettingsScene scene = scenes[existingIndex];
                scenes.RemoveAt(existingIndex);
                scenes.Insert(0, scene);
            }

            return;
        }

        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(path, enabled: true);
        if (insertAtStart)
        {
            scenes.Insert(0, newScene);
        }
        else
        {
            scenes.Add(newScene);
        }
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }

}
