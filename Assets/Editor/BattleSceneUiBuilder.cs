#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 搭建 BattleScene 局内 UI：GameplayHUD 子 Panel、三选一弹窗及基础流程面板。
/// </summary>
public static class BattleSceneUiBuilder
{
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

    private static readonly Color PanelSteel = Hex("#1E2228");
    private static readonly Color PanelDeep = Hex("#121416");
    private static readonly Color PrimaryCyan = Hex("#2EC4B6");
    private static readonly Color AccentAmber = Hex("#FF9F1C");
    private static readonly Color TextWhite = Hex("#E8EDF2");
    private static readonly Color TextSecondary = Hex("#8B9AAB");
    private static readonly Color WallHpGreen = Hex("#2ECC71");
    private static readonly Color CardHeader = Hex("#6B4A2E");
    private static readonly Color Scrim = new Color(0f, 0f, 0f, 0.62f);

    /// <summary>菜单：打开 BattleScene 并重建局内 UI 层级。</summary>
    [MenuItem("Attack Barbarians/UI/Build BattleScene UI")]
    public static void BuildBattleSceneUi()
    {
        Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[BattleSceneUiBuilder] 无法打开场景: {BattleScenePath}");
            return;
        }

        EnsureEventSystem();
        Transform existing = GameObject.Find("UICanvas")?.transform;
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        CreateUiCanvas();
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[BattleSceneUiBuilder] BattleScene UI 已搭建完成。");
    }

    /// <summary>确保场景中存在 EventSystem。</summary>
    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
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

    /// <summary>创建 UICanvas、UIManager 及全部局内面板。</summary>
    private static void CreateUiCanvas()
    {
        GameObject canvasObject = new GameObject(
            "UICanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(UiTmpChineseFontBootstrap),
            typeof(UIManager));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
        StretchFull(canvasRoot);

        GameplayHudPresenter hud = CreateGameplayHud(canvasRoot);
        UpgradePanelUI upgradePanel = CreateUpgradePanel(canvasRoot);
        PausePanelUI pausePanel = CreateSimpleOverlayPanel<PausePanelUI>(canvasRoot, "PausePanel", GameConstants.UiPanelIds.Pause, "战术暂停");
        GameOverPanelUI gameOverPanel = CreateSimpleOverlayPanel<GameOverPanelUI>(canvasRoot, "GameOverPanel", GameConstants.UiPanelIds.GameOver, "作战结束");
        WaveTransitionPanelUI wavePanel = CreateWaveTransitionPanel(canvasRoot);

        UIManager uiManager = canvasObject.GetComponent<UIManager>();
        SerializedObject uiSo = new SerializedObject(uiManager);
        uiSo.FindProperty("gameplayHud").objectReferenceValue = hud;
        uiSo.FindProperty("upgradePanel").objectReferenceValue = upgradePanel;
        uiSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        uiSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        uiSo.FindProperty("waveTransitionPanel").objectReferenceValue = wavePanel;
        uiSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>创建 GameplayHUD 及 TopBar/SkillRail/WallStats 子面板。</summary>
    /// <param name="canvasRoot">Canvas 根节点。</param>
    private static GameplayHudPresenter CreateGameplayHud(RectTransform canvasRoot)
    {
        GameObject hudRoot = new GameObject("GameplayHUD", typeof(RectTransform), typeof(GameplayHudPresenter));
        hudRoot.transform.SetParent(canvasRoot, false);
        StretchFull(hudRoot.GetComponent<RectTransform>());

        GameObject safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(UiSafeAreaFitter));
        safeGo.transform.SetParent(hudRoot.transform, false);
        StretchFull(safeGo.GetComponent<RectTransform>());

        GameplayHudTopPanel topPanel = CreateTopPanel(safeGo.transform);
        GameplayHudSkillRailPanel skillPanel = CreateSkillRail(safeGo.transform);
        GameplayHudWallPanel wallPanel = CreateWallPanel(safeGo.transform);

        GameplayHudPresenter presenter = hudRoot.GetComponent<GameplayHudPresenter>();
        SerializedObject so = new SerializedObject(presenter);
        so.FindProperty("topPanel").objectReferenceValue = topPanel;
        so.FindProperty("skillRailPanel").objectReferenceValue = skillPanel;
        so.FindProperty("wallPanel").objectReferenceValue = wallPanel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return presenter;
    }

    /// <summary>创建顶部 HUD：暂停、计时、经验与波次。</summary>
    /// <param name="parent">SafeArea 父节点。</param>
    private static GameplayHudTopPanel CreateTopPanel(Transform parent)
    {
        GameObject root = new GameObject("TopBar", typeof(RectTransform), typeof(UiRectLayout), typeof(GameplayHudTopPanel));
        root.transform.SetParent(parent, false);
        UiPageStructureEditorUtility.SetLayout(
            root.GetComponent<UiRectLayout>(),
            UiRectLayout.LayoutPreset.TopStretch,
            new RectOffset(24, 24, 24, 0),
            new Vector2(0f, 280f));

        RectTransform rect = root.GetComponent<RectTransform>();
        GameplayHudTopPanel panel = root.GetComponent<GameplayHudTopPanel>();

        Button pauseButton = CreateButton(rect, "PauseButton", new Vector2(-470f, -40f), new Vector2(72f, 72f), "||", PrimaryCyan);
        TMP_Text timerText = CreateText(rect, "TimerText", "00:00", 34, TextWhite, TextAlignmentOptions.MidlineLeft, new Vector2(-360f, -40f), new Vector2(180f, 56f));

        TMP_Text stageNameText = CreateText(rect, "StageNameText", "废墟车厂", 40, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -20f), new Vector2(700f, 56f));
        Slider expSlider = CreateSlider(rect, "ExpSlider", new Vector2(0f, -78f), new Vector2(760f, 22f), PrimaryCyan);
        TMP_Text levelText = CreateText(rect, "LevelText", "1级", 28, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -112f), new Vector2(200f, 40f));

        TMP_Text waveText = CreateText(rect, "WaveText", "波次: 1/20", 30, TextWhite, TextAlignmentOptions.MidlineRight, new Vector2(430f, -40f), new Vector2(260f, 56f));

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        so.FindProperty("timerText").objectReferenceValue = timerText;
        so.FindProperty("stageNameText").objectReferenceValue = stageNameText;
        so.FindProperty("expSlider").objectReferenceValue = expSlider;
        so.FindProperty("expFill").objectReferenceValue = expSlider.fillRect.GetComponent<Image>();
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.FindProperty("waveText").objectReferenceValue = waveText;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    /// <summary>创建右侧技能栏与 4 个技能槽位。</summary>
    /// <param name="parent">SafeArea 父节点。</param>
    private static GameplayHudSkillRailPanel CreateSkillRail(Transform parent)
    {
        GameObject root = new GameObject("SkillRail", typeof(RectTransform), typeof(UiRectLayout), typeof(GameplayHudSkillRailPanel));
        root.transform.SetParent(parent, false);
        UiPageStructureEditorUtility.SetLayout(
            root.GetComponent<UiRectLayout>(),
            UiRectLayout.LayoutPreset.RightStretch,
            new RectOffset(0, 16, 320, 360),
            new Vector2(148f, 0f));

        RectTransform rect = root.GetComponent<RectTransform>();
        GameplayHudSkillRailPanel panel = root.GetComponent<GameplayHudSkillRailPanel>();
        var slots = new GameplaySkillSlotView[4];
        float startY = 220f;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = CreateSkillSlot(rect, $"SkillSlot_{i}", new Vector2(0f, startY - i * 150f));
        }

        SerializedObject so = new SerializedObject(panel);
        SerializedProperty array = so.FindProperty("skillSlots");
        array.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }

        so.FindProperty("maxVisibleSlots").intValue = 4;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    /// <summary>创建单个技能槽视图。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    private static GameplaySkillSlotView CreateSkillSlot(RectTransform parent, string name, Vector2 position)
    {
        Image background = CreatePanel(parent, name, position, new Vector2(120f, 132f), PanelSteel);
        Image icon = CreatePanel(background.rectTransform, "IconImage", new Vector2(0f, 14f), new Vector2(72f, 72f), PrimaryCyan);
        TMP_Text levelText = CreateText(background.rectTransform, "LevelText", "1级", 22, TextWhite, TextAlignmentOptions.BottomLeft, new Vector2(-18f, -42f), new Vector2(80f, 30f));
        Image cooldownOverlay = CreatePanel(background.rectTransform, "CooldownOverlay", Vector2.zero, new Vector2(120f, 132f), new Color(0f, 0f, 0f, 0.55f));
        cooldownOverlay.type = Image.Type.Filled;
        cooldownOverlay.fillMethod = Image.FillMethod.Vertical;
        cooldownOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
        cooldownOverlay.fillAmount = 0f;
        cooldownOverlay.raycastTarget = false;
        TMP_Text cooldownText = CreateText(background.rectTransform, "CooldownText", "3", 24, TextWhite, TextAlignmentOptions.Center, Vector2.zero, new Vector2(80f, 40f));
        cooldownText.gameObject.SetActive(false);

        GameplaySkillSlotView view = background.gameObject.AddComponent<GameplaySkillSlotView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("backgroundImage").objectReferenceValue = background;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.FindProperty("cooldownOverlay").objectReferenceValue = cooldownOverlay;
        so.FindProperty("cooldownText").objectReferenceValue = cooldownText;
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return view;
    }

    /// <summary>创建底部城墙/玩家属性面板。</summary>
    /// <param name="parent">SafeArea 父节点。</param>
    private static GameplayHudWallPanel CreateWallPanel(Transform parent)
    {
        GameObject root = new GameObject("WallStats", typeof(RectTransform), typeof(UiRectLayout), typeof(GameplayHudWallPanel));
        root.transform.SetParent(parent, false);
        UiPageStructureEditorUtility.SetLayout(
            root.GetComponent<UiRectLayout>(),
            UiRectLayout.LayoutPreset.BottomStretch,
            new RectOffset(20, 20, 0, 24),
            new Vector2(0f, 220f));

        RectTransform rect = root.GetComponent<RectTransform>();
        GameplayHudWallPanel panel = root.GetComponent<GameplayHudWallPanel>();

        Slider healthSlider = CreateSlider(rect, "HealthSlider", new Vector2(-80f, 48f), new Vector2(700f, 34f), WallHpGreen);
        TMP_Text healthValueText = CreateText(rect, "HealthValueText", "3800", 30, TextWhite, TextAlignmentOptions.MidlineLeft, new Vector2(300f, 48f), new Vector2(140f, 40f));
        TMP_Text armorValueText = CreateText(rect, "ArmorValueText", "0", 28, TextWhite, TextAlignmentOptions.MidlineLeft, new Vector2(430f, 78f), new Vector2(100f, 36f));
        TMP_Text attackValueText = CreateText(rect, "AttackValueText", "4", 28, AccentAmber, TextAlignmentOptions.MidlineLeft, new Vector2(430f, 36f), new Vector2(100f, 36f));
        TMP_Text playerNameText = CreateText(rect, "PlayerNameText", "守卫者", 26, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 4f), new Vector2(400f, 36f));

        CreateText(rect, "ArmorLabel", "护甲", 22, TextSecondary, TextAlignmentOptions.MidlineRight, new Vector2(360f, 78f), new Vector2(60f, 30f));
        CreateText(rect, "AttackLabel", "攻击", 22, TextSecondary, TextAlignmentOptions.MidlineRight, new Vector2(360f, 36f), new Vector2(60f, 30f));

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("healthSlider").objectReferenceValue = healthSlider;
        so.FindProperty("healthFill").objectReferenceValue = healthSlider.fillRect.GetComponent<Image>();
        so.FindProperty("healthValueText").objectReferenceValue = healthValueText;
        so.FindProperty("armorValueText").objectReferenceValue = armorValueText;
        so.FindProperty("attackValueText").objectReferenceValue = attackValueText;
        so.FindProperty("playerNameText").objectReferenceValue = playerNameText;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    /// <summary>创建三选一升级弹窗面板。</summary>
    /// <param name="canvasRoot">Canvas 根节点。</param>
    private static UpgradePanelUI CreateUpgradePanel(RectTransform canvasRoot)
    {
        GameObject panelRoot = new GameObject("UpgradePanel", typeof(RectTransform), typeof(UpgradePanelUI), typeof(UiPanelTransition));
        panelRoot.transform.SetParent(canvasRoot, false);
        StretchFull(panelRoot.GetComponent<RectTransform>());

        UpgradePanelUI panel = panelRoot.GetComponent<UpgradePanelUI>();
        Image scrim = CreatePanel(panelRoot.transform as RectTransform, "Scrim", Vector2.zero, ReferenceResolution, Scrim);
        StretchFull(scrim.rectTransform);
        scrim.raycastTarget = true;

        TMP_Text titleText = CreateText(panelRoot.transform as RectTransform, "TitleText", "选择技能", 44, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 620f), new Vector2(600f, 72f));
        Image titleBanner = CreatePanel(titleText.rectTransform.parent as RectTransform, "TitleBanner", new Vector2(0f, 620f), new Vector2(520f, 88f), AccentAmber);
        titleBanner.transform.SetSiblingIndex(titleText.transform.GetSiblingIndex());
        titleText.transform.SetParent(titleBanner.rectTransform, false);
        StretchFull(titleText.rectTransform);

        var cards = new UpgradeChoiceCardView[3];
        for (int i = 0; i < 3; i++)
        {
            cards[i] = CreateUpgradeChoiceCard(panelRoot.transform as RectTransform, $"ChoiceCard_{i}", new Vector2(-320f + i * 320f, 120f));
        }

        TMP_Text adQuotaHintText = CreateText(panelRoot.transform as RectTransform, "AdQuotaHint", "当局剩余观看次数 2/2", 24, TextSecondary, TextAlignmentOptions.Center, new Vector2(0f, -620f), new Vector2(520f, 40f));
        UiRewardedAdButton refreshButton = CreateRewardedAdButton(panelRoot.transform as RectTransform, "RefreshAdButton", new Vector2(-180f, -700f), new Vector2(300f, 88f), "刷新");
        UiRewardedAdButton selectAllButton = CreateRewardedAdButton(panelRoot.transform as RectTransform, "SelectAllAdButton", new Vector2(180f, -700f), new Vector2(300f, 88f), "全选");

        SerializedObject panelSo = new SerializedObject(panel);
        panelSo.FindProperty("panelId").stringValue = GameConstants.UiPanelIds.Upgrade;
        panelSo.FindProperty("root").objectReferenceValue = panelRoot;
        panelSo.FindProperty("transition").objectReferenceValue = panelRoot.GetComponent<UiPanelTransition>();
        panelSo.FindProperty("titleText").objectReferenceValue = titleText;
        panelSo.FindProperty("adQuotaHintText").objectReferenceValue = adQuotaHintText;
        panelSo.FindProperty("refreshAdButton").objectReferenceValue = refreshButton;
        panelSo.FindProperty("selectAllAdButton").objectReferenceValue = selectAllButton;
        SerializedProperty cardArray = panelSo.FindProperty("choiceCards");
        cardArray.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            cardArray.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }

        panelSo.ApplyModifiedPropertiesWithoutUndo();
        panelRoot.SetActive(false);
        return panel;
    }

    /// <summary>创建单张升级选项卡片。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    private static UpgradeChoiceCardView CreateUpgradeChoiceCard(RectTransform parent, string name, Vector2 position)
    {
        Image background = CreatePanel(parent, name, position, new Vector2(280f, 520f), PanelSteel);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Image header = CreatePanel(background.rectTransform, "BuffNameBar", new Vector2(0f, 210f), new Vector2(280f, 64f), CardHeader);
        TMP_Text buffNameText = CreateText(header.rectTransform, "BuffNameText", "Buff名称", 30, TextWhite, TextAlignmentOptions.Center, Vector2.zero, new Vector2(260f, 52f));

        Image iconBg = CreatePanel(background.rectTransform, "BuffIconBg", new Vector2(0f, 50f), new Vector2(168f, 168f), PanelDeep);
        Image buffIconImage = CreatePanel(iconBg.rectTransform, "BuffIconImage", Vector2.zero, new Vector2(128f, 128f), PrimaryCyan);
        buffIconImage.preserveAspect = true;
        buffIconImage.type = Image.Type.Simple;

        TMP_Text buffDescriptionText = CreateText(
            background.rectTransform,
            "BuffDescriptionText",
            "Buff效果描述",
            24,
            TextSecondary,
            TextAlignmentOptions.Top,
            new Vector2(0f, -150f),
            new Vector2(248f, 140f));

        TMP_Text learnHintText = CreateText(background.rectTransform, "LearnHintText", "学习技能", 24, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -60f), new Vector2(250f, 36f));
        learnHintText.gameObject.SetActive(false);

        GameObject newBadge = CreatePanel(background.rectTransform, "NewBadge", new Vector2(-100f, 220f), new Vector2(56f, 36f), WallHpGreen).gameObject;
        CreateText(newBadge.transform as RectTransform, "NewBadgeText", "新", 22, TextWhite, TextAlignmentOptions.Center, Vector2.zero, new Vector2(56f, 36f));

        Image border = CreatePanel(background.rectTransform, "RarityBorder", Vector2.zero, new Vector2(280f, 520f), Color.clear);
        border.raycastTarget = false;
        UiRarityVisual rarityVisual = border.gameObject.AddComponent<UiRarityVisual>();
        SerializedObject raritySo = new SerializedObject(rarityVisual);
        raritySo.FindProperty("borderImage").objectReferenceValue = border;
        raritySo.FindProperty("useOutlineBorder").boolValue = true;
        raritySo.ApplyModifiedPropertiesWithoutUndo();

        UpgradeChoiceCardView card = background.gameObject.AddComponent<UpgradeChoiceCardView>();
        SerializedObject cardSo = new SerializedObject(card);
        cardSo.FindProperty("selectButton").objectReferenceValue = button;
        cardSo.FindProperty("buffNameText").objectReferenceValue = buffNameText;
        cardSo.FindProperty("buffDescriptionText").objectReferenceValue = buffDescriptionText;
        cardSo.FindProperty("buffIconImage").objectReferenceValue = buffIconImage;
        cardSo.FindProperty("learnHintText").objectReferenceValue = learnHintText;
        cardSo.FindProperty("newBadgeRoot").objectReferenceValue = newBadge;
        cardSo.FindProperty("rarityVisual").objectReferenceValue = rarityVisual;
        cardSo.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return card;
    }

    /// <summary>创建带 AD 角标的激励广告按钮。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="label">按钮文本。</param>
    private static UiRewardedAdButton CreateRewardedAdButton(RectTransform parent, string name, Vector2 position, Vector2 size, string label)
    {
        Image background = CreatePanel(parent, name, position, size, PrimaryCyan);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        TMP_Text labelText = CreateText(background.rectTransform, "LabelText", label, 32, TextWhite, TextAlignmentOptions.Center, new Vector2(-20f, 0f), new Vector2(size.x - 80f, size.y));
        Image adBadge = CreatePanel(background.rectTransform, "AdBadge", new Vector2(size.x * 0.32f, 0f), new Vector2(52f, 36f), AccentAmber);
        TMP_Text adBadgeText = CreateText(adBadge.rectTransform, "AdBadgeText", "AD", 18, TextWhite, TextAlignmentOptions.Center, Vector2.zero, new Vector2(52f, 36f));

        UiRewardedAdButton widget = background.gameObject.AddComponent<UiRewardedAdButton>();
        SerializedObject so = new SerializedObject(widget);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("labelText").objectReferenceValue = labelText;
        so.FindProperty("adBadgeImage").objectReferenceValue = adBadge;
        so.FindProperty("adBadgeText").objectReferenceValue = adBadgeText;
        so.ApplyModifiedPropertiesWithoutUndo();
        return widget;
    }

    /// <summary>创建波次过渡提示面板。</summary>
    /// <param name="canvasRoot">Canvas 根节点。</param>
    private static WaveTransitionPanelUI CreateWaveTransitionPanel(RectTransform canvasRoot)
    {
        WaveTransitionPanelUI panel = CreateSimpleOverlayPanel<WaveTransitionPanelUI>(
            canvasRoot,
            "WaveTransitionPanel",
            GameConstants.UiPanelIds.WaveTransition,
            "WAVE INBOUND");
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("bannerText").objectReferenceValue =
            panel.transform.Find("Content/BannerText")?.GetComponent<TMP_Text>();
        so.FindProperty("subtitleText").objectReferenceValue =
            panel.transform.Find("Content/SubtitleText")?.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static T CreateSimpleOverlayPanel<T>(RectTransform canvasRoot, string name, string panelId, string title)
        where T : UiPanelBase
    {
        GameObject panelRoot = new GameObject(name, typeof(RectTransform), typeof(T), typeof(UiPanelTransition));
        panelRoot.transform.SetParent(canvasRoot, false);
        StretchFull(panelRoot.GetComponent<RectTransform>());

        Image scrim = CreatePanel(panelRoot.transform as RectTransform, "Scrim", Vector2.zero, ReferenceResolution, Scrim);
        StretchFull(scrim.rectTransform);

        RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panelRoot.transform, false);
        content.sizeDelta = new Vector2(800f, 500f);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;

        Image panelBg = CreatePanel(content, "PanelBg", Vector2.zero, new Vector2(800f, 500f), PanelSteel);
        TMP_Text bannerText = CreateText(content, "BannerText", title, 40, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 80f), new Vector2(700f, 60f));
        TMP_Text subtitleText = CreateText(content, "SubtitleText", "...", 28, TextSecondary, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(700f, 50f));

        T panel = panelRoot.GetComponent<T>();
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = panelId;
        so.FindProperty("root").objectReferenceValue = panelRoot;
        if (panel is PausePanelUI)
        {
            so.FindProperty("titleText").objectReferenceValue = bannerText;
        }
        else if (panel is GameOverPanelUI)
        {
            so.FindProperty("headerText").objectReferenceValue = bannerText;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        panelRoot.SetActive(false);
        return panel;
    }

    /// <summary>创建 Slider 控件。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="fillColor">填充色。</param>
    private static Slider CreateSlider(RectTransform parent, string name, Vector2 position, Vector2 size, Color fillColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image background = CreatePanel(rect, "Background", Vector2.zero, size, PanelDeep);
        StretchFull(background.rectTransform);
        Image fill = CreatePanel(rect, "Fill", Vector2.zero, size, fillColor);
        StretchFull(fill.rectTransform);

        Slider slider = go.GetComponent<Slider>();
        slider.targetGraphic = background;
        slider.fillRect = fill.rectTransform;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    /// <summary>创建带标签的 Button。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="label">标签文本。</param>
    /// <param name="color">背景色。</param>
    private static Button CreateButton(RectTransform parent, string name, Vector2 position, Vector2 size, string label, Color color)
    {
        Image background = CreatePanel(parent, name, position, size, color);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        CreateText(background.rectTransform, "Label", label, 28, TextWhite, TextAlignmentOptions.Center, Vector2.zero, size);
        return button;
    }

    /// <summary>创建 Image 面板。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="anchoredPosition">锚点坐标。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="color">颜色。</param>
    private static Image CreatePanel(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>创建 TMP 文本节点。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="text">文本。</param>
    /// <param name="fontSize">字号。</param>
    /// <param name="color">颜色。</param>
    /// <param name="alignment">对齐。</param>
    /// <param name="anchoredPosition">锚点坐标。</param>
    /// <param name="size">尺寸。</param>
    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = size.y > fontSize * 1.5f;
        return tmp;
    }

    /// <summary>将 RectTransform 四向拉伸铺满父节点。</summary>
    /// <param name="rect">目标 RectTransform。</param>
    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    /// <summary>解析 HTML 颜色字符串。</summary>
    /// <param name="html">十六进制颜色值。</param>
    private static Color Hex(string html) =>
        ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
}
#endif
