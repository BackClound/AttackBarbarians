#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在 MainScene 中搭建商城页面 UI，并绑定 ShopSceneView / MainSceneView 页面切换。
/// </summary>
public static class ShopPageBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Color PanelBlue = Hex("#0C2A5A");
    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color NeonPurple = Hex("#A95CFF");
    private static readonly Color TextWhite = Hex("#F5FAFF");

    [MenuItem("Attack Barbarians/UI/Build Shop Page In MainScene")]
    public static void BuildShopPageInMainScene()
    {
        ShopConfigBootstrapMenu.CreateDefaultShopAssets();

        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        MainSceneView mainView = Object.FindObjectOfType<MainSceneView>();
        if (mainView == null)
        {
            Debug.LogError("[ShopPageBuilder] 未找到 MainSceneView。");
            return;
        }

        RectTransform canvasRoot = mainView.transform as RectTransform;
        Transform existing = FindChildRecursive(canvasRoot, "ShopPageRoot");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        ShopSceneView shopView = CreateShopPage(canvasRoot);
        WireMainSceneView(mainView, shopView);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopPageBuilder] 商城页面已搭建并绑定到 MainScene。");
    }

    private static ShopSceneView CreateShopPage(RectTransform canvasRoot)
    {
        UiPageStructureEditorUtility.PageShell shell = UiPageStructureEditorUtility.CreatePageShell(
            canvasRoot,
            "ShopPageRoot",
            typeof(ShopSceneView),
            UiPageStructureEditorUtility.DefaultPageBackground);
        shell.PageRoot.gameObject.SetActive(false);
        shell.PageRoot.name = "ShopPageRoot";

        Transform background = shell.PageRoot.Find("PageBackground");
        if (background != null)
        {
            background.name = "ShopPageBackground";
        }

        ShopSceneView shopView = shell.PageRoot.GetComponent<ShopSceneView>();
        RectTransform safeArea = shell.SafeAreaRoot;

        RectTransform topBar = CreateLayoutHost(
            safeArea,
            "ShopTopBar",
            UiRectLayout.LayoutPreset.TopStretch,
            new RectOffset(12, 12, 8, 0),
            new Vector2(0f, 88f));
        ShopResourcePanel resourcePanel = topBar.gameObject.AddComponent<ShopResourcePanel>();
        CreateShopResources(topBar, resourcePanel);

        RectTransform scrollHost = CreateLayoutHost(
            safeArea,
            "ShopScrollHost",
            UiRectLayout.LayoutPreset.FullStretch,
            new RectOffset(8, 8, 100, 56),
            Vector2.zero);
        ScrollRect scroll = CreateScrollArea(scrollHost);

        RectTransform supplyRow = CreatePanelHost(scroll.content, "SupplyRow", new Vector2(0f, -420f), new Vector2(1040f, 820f));
        ShopSupplySectionPanel supplyPanel = supplyRow.gameObject.AddComponent<ShopSupplySectionPanel>();
        ShopCrateWidget commonCrate = CreateCrateWidget(supplyRow, "CommonCrate", new Vector2(-350f, 0f), new Vector2(330f, 780f), NeonBlue,
            GameConstants.ConfigIds.ShopCrateCommonSingle,
            GameConstants.ConfigIds.ShopCrateCommonTen,
            GameConstants.ConfigIds.ShopAdCrateCommon);
        ShopCrateWidget premiumCrate = CreateCrateWidget(supplyRow, "PremiumCrate", new Vector2(0f, 0f), new Vector2(330f, 780f), NeonPurple,
            GameConstants.ConfigIds.ShopCratePremiumSingle,
            GameConstants.ConfigIds.ShopCratePremiumTen,
            GameConstants.ConfigIds.ShopAdCratePremium);
        ShopGoldSupplyWidget goldSupply = CreateGoldSupplyWidget(supplyRow, "GoldSupply", new Vector2(350f, 0f), new Vector2(330f, 780f));

        RectTransform exchangeRoot = CreatePanelHost(scroll.content, "ExchangeSection", new Vector2(0f, -980f), new Vector2(1040f, 520f));
        ShopAdTicketExchangePanel exchangePanel = exchangeRoot.gameObject.AddComponent<ShopAdTicketExchangePanel>();
        CreateExchangeSection(exchangeRoot, exchangePanel);

        scroll.content.sizeDelta = new Vector2(0f, 1280f);

        RectTransform statusBar = CreateLayoutHost(
            safeArea,
            "ShopStatusBar",
            UiRectLayout.LayoutPreset.BottomStretch,
            new RectOffset(16, 16, 0, 8),
            new Vector2(0f, 44f));
        TMP_Text status = CreateText(statusBar, "ShopStatusText", string.Empty, 22, NeonGold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(900f, 40f));

        SerializedObject shopSo = new SerializedObject(shopView);
        shopSo.FindProperty("backgroundImage").objectReferenceValue = shell.BackgroundImage;
        shopSo.FindProperty("resourcePanel").objectReferenceValue = resourcePanel;
        shopSo.FindProperty("supplySection").objectReferenceValue = supplyPanel;
        shopSo.FindProperty("exchangePanel").objectReferenceValue = exchangePanel;
        shopSo.FindProperty("statusText").objectReferenceValue = status;
        shopSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject supplySo = new SerializedObject(supplyPanel);
        supplySo.FindProperty("commonCrate").objectReferenceValue = commonCrate;
        supplySo.FindProperty("premiumCrate").objectReferenceValue = premiumCrate;
        supplySo.FindProperty("goldSupply").objectReferenceValue = goldSupply;
        supplySo.ApplyModifiedPropertiesWithoutUndo();

        return shopView;
    }

    private static void CreateShopResources(RectTransform root, ShopResourcePanel panel)
    {
        UI_ItemSlot diamond = CreateResourcePill(root, "ShopDiamond", new Vector2(-360f, 0f), "12,450", NeonBlue, CurrencyType.Diamond);
        UI_ItemSlot gold = CreateResourcePill(root, "ShopGold", new Vector2(-120f, 0f), "8,752,300", NeonGold, CurrencyType.Gold);
        UI_ItemSlot energy = CreateResourcePill(root, "ShopEnergy", new Vector2(120f, 0f), "120/120", Hex("#FF5E7E"), CurrencyType.Energy);
        UI_ItemSlot tech = CreateResourcePill(root, "ShopTechPoint", new Vector2(360f, 0f), "3,260", Hex("#5BE7FF"), CurrencyType.TechPoint);

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("diamondSlot").objectReferenceValue = diamond;
        so.FindProperty("goldSlot").objectReferenceValue = gold;
        so.FindProperty("energySlot").objectReferenceValue = energy;
        so.FindProperty("techPointSlot").objectReferenceValue = tech;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ShopCrateWidget CreateCrateWidget(
        RectTransform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color accent,
        string singleId,
        string tenId,
        string adId)
    {
        Image background = CreatePanel(parent, name, position, size, PanelDeep);
        CreateText(background.rectTransform, "PanelIndex", name.Contains("Common") ? "01" : "02", 28, NeonBlue, TextAlignmentOptions.TopLeft, new Vector2(-size.x * 0.42f, size.y * 0.42f), new Vector2(80f, 40f));
        TMP_Text title = CreateText(background.rectTransform, "Title", name.Contains("Common") ? "普通补给箱" : "高级补给箱", 26, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, size.y * 0.34f), new Vector2(size.x - 24f, 40f));
        Image crate = CreatePanel(background.rectTransform, "CrateImage", new Vector2(0f, size.y * 0.08f), new Vector2(180f, 180f), accent);
        TMP_Text rewards = CreateText(background.rectTransform, "RewardsHint", "可获得奖励", 18, Hex("#9EC8FF"), TextAlignmentOptions.Center, new Vector2(0f, -size.y * 0.12f), new Vector2(size.x - 24f, 60f));

        Button singleBtn = CreateActionButton(background.rectTransform, "SinglePull", new Vector2(0f, -size.y * 0.28f), new Vector2(size.x - 32f, 56f), "单抽", NeonBlue, out TMP_Text singleCost);
        Button tenBtn = CreateActionButton(background.rectTransform, "TenPull", new Vector2(0f, -size.y * 0.38f), new Vector2(size.x - 32f, 56f), "十连抽", accent, out TMP_Text tenCost);
        Button adBtn = CreateActionButton(background.rectTransform, "AdPull", new Vector2(0f, -size.y * 0.48f), new Vector2(size.x - 32f, 56f), "观看广告免费抽取", PanelBlue, out TMP_Text adText);

        ShopCrateWidget widget = background.gameObject.AddComponent<ShopCrateWidget>();
        SerializedObject so = new SerializedObject(widget);
        so.FindProperty("panelIndexText").objectReferenceValue = background.transform.Find("PanelIndex")?.GetComponent<TMP_Text>();
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("crateImage").objectReferenceValue = crate;
        so.FindProperty("rewardsHintText").objectReferenceValue = rewards;
        so.FindProperty("singlePullButton").objectReferenceValue = singleBtn;
        so.FindProperty("singleCostText").objectReferenceValue = singleCost;
        so.FindProperty("tenPullButton").objectReferenceValue = tenBtn;
        so.FindProperty("tenCostText").objectReferenceValue = tenCost;
        so.FindProperty("adPullButton").objectReferenceValue = adBtn;
        so.FindProperty("adPullText").objectReferenceValue = adText;
        so.FindProperty("singleItemConfigId").stringValue = singleId;
        so.FindProperty("tenItemConfigId").stringValue = tenId;
        so.FindProperty("adFreeConfigId").stringValue = adId;
        so.ApplyModifiedPropertiesWithoutUndo();
        return widget;
    }

    private static ShopGoldSupplyWidget CreateGoldSupplyWidget(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        Image background = CreatePanel(parent, name, position, size, PanelDeep);
        CreateText(background.rectTransform, "PanelIndex", "03", 28, NeonBlue, TextAlignmentOptions.TopLeft, new Vector2(-size.x * 0.42f, size.y * 0.42f), new Vector2(80f, 40f));
        CreateText(background.rectTransform, "Title", "金币补给", 26, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, size.y * 0.38f), new Vector2(size.x - 24f, 40f));

        ShopGoldSupplyWidget.GoldSupplyTier lowTier = CreateGoldTier(background.rectTransform, "LowTier", new Vector2(0f, size.y * 0.12f),
            GameConstants.ConfigIds.ShopGoldSupplyLowSingle, GameConstants.ConfigIds.ShopGoldSupplyLowTen, "低级补给", Hex("#8B9AAB"));
        ShopGoldSupplyWidget.GoldSupplyTier stdTier = CreateGoldTier(background.rectTransform, "StdTier", new Vector2(0f, -size.y * 0.08f),
            GameConstants.ConfigIds.ShopGoldSupplyStdSingle, GameConstants.ConfigIds.ShopGoldSupplyStdTen, "标准补给", NeonGold);
        Button adBtn = CreateActionButton(background.rectTransform, "AdClaim", new Vector2(0f, -size.y * 0.38f), new Vector2(size.x - 32f, 56f), "广告领取", NeonBlue, out TMP_Text adText);

        ShopGoldSupplyWidget widget = background.gameObject.AddComponent<ShopGoldSupplyWidget>();
        SerializedObject so = new SerializedObject(widget);
        so.FindProperty("panelIndexText").objectReferenceValue = background.transform.Find("PanelIndex")?.GetComponent<TMP_Text>();
        so.FindProperty("titleText").objectReferenceValue = background.transform.Find("Title")?.GetComponent<TMP_Text>();
        so.FindProperty("lowTier").FindPropertyRelative("title").stringValue = "低级补给";
        so.FindProperty("lowTier").FindPropertyRelative("singleItemConfigId").stringValue = GameConstants.ConfigIds.ShopGoldSupplyLowSingle;
        so.FindProperty("lowTier").FindPropertyRelative("tenItemConfigId").stringValue = GameConstants.ConfigIds.ShopGoldSupplyLowTen;
        so.FindProperty("lowTier").FindPropertyRelative("singlePullButton").objectReferenceValue = lowTier.singlePullButton;
        so.FindProperty("lowTier").FindPropertyRelative("singleCostText").objectReferenceValue = lowTier.singleCostText;
        so.FindProperty("lowTier").FindPropertyRelative("tenPullButton").objectReferenceValue = lowTier.tenPullButton;
        so.FindProperty("lowTier").FindPropertyRelative("tenCostText").objectReferenceValue = lowTier.tenCostText;
        so.FindProperty("standardTier").FindPropertyRelative("title").stringValue = "标准补给";
        so.FindProperty("standardTier").FindPropertyRelative("singleItemConfigId").stringValue = GameConstants.ConfigIds.ShopGoldSupplyStdSingle;
        so.FindProperty("standardTier").FindPropertyRelative("tenItemConfigId").stringValue = GameConstants.ConfigIds.ShopGoldSupplyStdTen;
        so.FindProperty("standardTier").FindPropertyRelative("singlePullButton").objectReferenceValue = stdTier.singlePullButton;
        so.FindProperty("standardTier").FindPropertyRelative("singleCostText").objectReferenceValue = stdTier.singleCostText;
        so.FindProperty("standardTier").FindPropertyRelative("tenPullButton").objectReferenceValue = stdTier.tenPullButton;
        so.FindProperty("standardTier").FindPropertyRelative("tenCostText").objectReferenceValue = stdTier.tenCostText;
        so.FindProperty("adClaimButton").objectReferenceValue = adBtn;
        so.FindProperty("adClaimText").objectReferenceValue = adText;
        so.FindProperty("adClaimConfigId").stringValue = GameConstants.ConfigIds.ShopAdGoldSupply;
        so.ApplyModifiedPropertiesWithoutUndo();
        return widget;
    }

    private static ShopGoldSupplyWidget.GoldSupplyTier CreateGoldTier(
        RectTransform parent,
        string name,
        Vector2 position,
        string singleId,
        string tenId,
        string title,
        Color accent)
    {
        RectTransform host = CreatePanelHost(parent, name, position, new Vector2(300f, 150f));
        CreateText(host, "TierTitle", title, 20, TextWhite, TextAlignmentOptions.TopLeft, new Vector2(-90f, 50f), new Vector2(180f, 30f));
        Image crate = CreatePanel(host, "CrateIcon", new Vector2(-100f, -10f), new Vector2(64f, 64f), accent);
        Button singleBtn = CreateActionButton(host, "Single", new Vector2(40f, 20f), new Vector2(130f, 44f), "单抽", PanelBlue, out TMP_Text singleCost);
        Button tenBtn = CreateActionButton(host, "Ten", new Vector2(40f, -35f), new Vector2(130f, 44f), "十连", accent, out TMP_Text tenCost);
        return new ShopGoldSupplyWidget.GoldSupplyTier
        {
            title = title,
            singleItemConfigId = singleId,
            tenItemConfigId = tenId,
            singlePullButton = singleBtn,
            singleCostText = singleCost,
            tenPullButton = tenBtn,
            tenCostText = tenCost,
        };
    }

    private static void CreateExchangeSection(RectTransform root, ShopAdTicketExchangePanel panel)
    {
        Image background = CreatePanel(root, "ExchangeBackground", Vector2.zero, root.sizeDelta, PanelBlue);
        StretchToParent(background.rectTransform);
        TMP_Text title = CreateText(background.rectTransform, "SectionTitle", "广告券兑换中心", 30, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, 220f), new Vector2(500f, 44f));
        Image ticketPanel = CreatePanel(background.rectTransform, "TicketPanel", new Vector2(-300f, -20f), new Vector2(280f, 360f), PanelDeep);
        CreatePanel(ticketPanel.rectTransform, "TicketIcon", new Vector2(0f, 40f), new Vector2(120f, 120f), NeonBlue);
        TMP_Text held = CreateText(ticketPanel.rectTransform, "HeldText", "持有数量 12", 24, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -80f), new Vector2(240f, 40f));

        string[] diamondIds =
        {
            GameConstants.ConfigIds.ShopExchangeDiamond1,
            GameConstants.ConfigIds.ShopExchangeDiamond2,
            GameConstants.ConfigIds.ShopExchangeDiamond3,
            GameConstants.ConfigIds.ShopExchangeDiamond4,
        };
        string[] goldIds =
        {
            GameConstants.ConfigIds.ShopExchangeGold1,
            GameConstants.ConfigIds.ShopExchangeGold2,
            GameConstants.ConfigIds.ShopExchangeGold3,
            GameConstants.ConfigIds.ShopExchangeGold4,
        };

        ShopExchangeRowWidget[] diamondRows = CreateExchangeColumn(background.rectTransform, "DiamondColumn", new Vector2(40f, -20f), "兑换水晶", diamondIds, NeonBlue);
        ShopExchangeRowWidget[] goldRows = CreateExchangeColumn(background.rectTransform, "GoldColumn", new Vector2(320f, -20f), "兑换金币", goldIds, NeonGold);

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("sectionTitleText").objectReferenceValue = title;
        so.FindProperty("heldTicketText").objectReferenceValue = held;
        SetArray(so.FindProperty("diamondRows"), diamondRows);
        SetArray(so.FindProperty("goldRows"), goldRows);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ShopExchangeRowWidget[] CreateExchangeColumn(
        RectTransform parent,
        string name,
        Vector2 position,
        string columnTitle,
        string[] configIds,
        Color accent)
    {
        RectTransform column = CreatePanelHost(parent, name, position, new Vector2(300f, 360f));
        CreateText(column, "ColumnTitle", columnTitle, 22, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, 150f), new Vector2(260f, 36f));
        var rows = new ShopExchangeRowWidget[configIds.Length];
        for (int i = 0; i < configIds.Length; i++)
        {
            float y = 90f - i * 72f;
            rows[i] = CreateExchangeRow(column, $"Row{i + 1}", new Vector2(0f, y), configIds[i], accent);
        }

        return rows;
    }

    private static ShopExchangeRowWidget CreateExchangeRow(RectTransform parent, string name, Vector2 position, string configId, Color accent)
    {
        Image rowBg = CreatePanel(parent, name, position, new Vector2(280f, 60f), PanelDeep);
        CreateText(rowBg.rectTransform, "TicketCost", "1 券", 20, TextWhite, TextAlignmentOptions.Left, new Vector2(-70f, 0f), new Vector2(80f, 40f));
        TMP_Text reward = CreateText(rowBg.rectTransform, "Reward", "10 水晶", 20, accent, TextAlignmentOptions.Left, new Vector2(20f, 0f), new Vector2(120f, 40f));
        Button btn = CreateActionButton(rowBg.rectTransform, "Exchange", new Vector2(110f, 0f), new Vector2(52f, 44f), ">>", accent, out _);

        ShopExchangeRowWidget row = rowBg.gameObject.AddComponent<ShopExchangeRowWidget>();
        SerializedObject so = new SerializedObject(row);
        so.FindProperty("ticketCostText").objectReferenceValue = rowBg.transform.Find("TicketCost")?.GetComponent<TMP_Text>();
        so.FindProperty("rewardText").objectReferenceValue = reward;
        so.FindProperty("exchangeButton").objectReferenceValue = btn;
        so.FindProperty("exchangeButtonImage").objectReferenceValue = btn.GetComponent<Image>();
        so.FindProperty("exchangeConfigId").stringValue = configId;
        so.ApplyModifiedPropertiesWithoutUndo();
        row.SetButtonAccent(accent);
        return row;
    }

    private static ScrollRect CreateScrollArea(RectTransform parent)
    {
        GameObject scrollGo = new GameObject("ShopScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(parent, false);
        StretchToParent(scrollGo.GetComponent<RectTransform>());
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewport = viewportGo.GetComponent<RectTransform>();
        StretchToParent(viewport);
        viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(1040f, 1300f);

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return scroll;
    }

    private static void WireMainSceneView(MainSceneView mainView, ShopSceneView shopView)
    {
        RectTransform canvasRoot = mainView.transform as RectTransform;
        Transform bottomNav = FindChildRecursive(canvasRoot, "BottomNav");
        UiPageStructureEditorUtility.PinBottomNavToCanvas(canvasRoot, bottomNav);

        SerializedObject mainSo = new SerializedObject(mainView);
        mainSo.FindProperty("shopPage").objectReferenceValue = shopView;
        mainSo.FindProperty("bottomNavCardPanel").objectReferenceValue =
            bottomNav != null ? bottomNav.GetComponent<MainSceneCardPanel>() : null;
        mainSo.FindProperty("battlePage").objectReferenceValue =
            FindChild(canvasRoot, "BattlePagePanelRoot")?.GetComponent<MainSceneBattlePageView>();
        mainSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindChild(Transform parent, string name)
    {
        Transform direct = parent != null ? parent.Find(name) : null;
        return direct != null ? direct : FindChildRecursive(parent, name);
    }

    private static RectTransform CreateLayoutHost(
        Transform parent,
        string name,
        UiRectLayout.LayoutPreset preset,
        RectOffset padding,
        Vector2 fixedSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(UiRectLayout));
        go.transform.SetParent(parent, false);
        SetLayout(go.GetComponent<UiRectLayout>(), preset, padding, fixedSize);
        return go.GetComponent<RectTransform>();
    }

    private static void SetLayout(UiRectLayout layout, UiRectLayout.LayoutPreset preset, RectOffset padding, Vector2 fixedSize = default)
    {
        UiPageStructureEditorUtility.SetLayout(layout, preset, padding, fixedSize);
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
        outline.effectColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);
        return image;
    }

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
        TMP_FontAsset font = UiChineseTmpFontBuilder.CreateOrLoadChineseFontAsset();
        if (font != null)
        {
            label.font = font;
        }

        return label;
    }

    private static Button CreateActionButton(
        RectTransform parent,
        string name,
        Vector2 position,
        Vector2 size,
        string label,
        Color accent,
        out TMP_Text labelText)
    {
        Image bg = CreatePanel(parent, name, position, size, accent * 0.85f + PanelBlue * 0.15f);
        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        labelText = CreateText(bg.rectTransform, "Label", label, 20, TextWhite, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(8f, 8f));
        return button;
    }

    private static UI_ItemSlot CreateResourcePill(RectTransform parent, string name, Vector2 position, string value, Color accent, CurrencyType currency)
    {
        Image panel = CreatePanel(parent, name, position, new Vector2(230f, 58f), PanelDeep);
        Button add = panel.gameObject.AddComponent<Button>();
        CreatePanel(panel.rectTransform, "Icon", new Vector2(-78f, 0f), new Vector2(42f, 42f), accent);
        TMP_Text valueText = CreateText(panel.rectTransform, "ValueText", value, 22, TextWhite, TextAlignmentOptions.Left, new Vector2(8f, 0f), new Vector2(120f, 42f));
        CreateText(panel.rectTransform, "AddText", "+", 28, NeonBlue, TextAlignmentOptions.Center, new Vector2(88f, 0f), new Vector2(34f, 42f));
        UI_ItemSlot slot = panel.gameObject.AddComponent<UI_ItemSlot>();
        SerializedObject so = new SerializedObject(slot);
        so.FindProperty("currency").enumValueIndex = (int)currency;
        so.FindProperty("iconImage").objectReferenceValue = panel.transform.Find("Icon")?.GetComponent<Image>();
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.FindProperty("addButton").objectReferenceValue = add;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    private static void SetArray(SerializedProperty arrayProp, ShopExchangeRowWidget[] items)
    {
        arrayProp.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void StretchToParent(RectTransform rect)
    {
        UiPageStructureEditorUtility.StretchFull(rect);
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }
}
#endif
