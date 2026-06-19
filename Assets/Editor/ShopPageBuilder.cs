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

    private const float ShopTopBarHeight = 88f;
    private const float ShopTopBarPaddingTop = 8f;
    private const float ShopStatusBarHeight = 44f;
    private const float ShopStatusBarPaddingBottom = 8f;
    private const float ShopScrollSectionSpacing = 16f;
    private const float SupplyRowHeight = 820f;
    private const float ExchangeSectionHeight = 520f;

    private static int ShopScrollHostPaddingTop =>
        Mathf.RoundToInt(ShopTopBarHeight + ShopTopBarPaddingTop);

    private static int ShopScrollHostPaddingBottom =>
        Mathf.RoundToInt(ShopStatusBarHeight + ShopStatusBarPaddingBottom);

    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Color PanelBlue = Hex("#0C2A5A");
    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color NeonPurple = Hex("#A95CFF");
    private static readonly Color TextWhite = Hex("#F5FAFF");

    /// <summary>菜单：在 MainScene 搭建商城页并绑定 MainSceneView。</summary>
    [MenuItem("Attack Barbarians/UI/Build Shop Page In MainScene")]
    public static void BuildShopPageInMainScene()
    {
        ShopConfigBootstrapMenu.CreateDefaultShopAssets();
        UpgradeCardConfigBootstrapMenu.CreateAllUpgradeCardAssets();

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

    /// <summary>菜单：修复商城 ScrollView 顶栏/底栏固定与内容纵向布局。</summary>
    [MenuItem("Attack Barbarians/UI/Fix Shop Scroll Layout In MainScene")]
    public static void FixShopScrollLayoutInMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        Transform scrollHost = FindChildRecursive(Object.FindObjectOfType<MainSceneView>()?.transform, "ShopScrollHost");
        if (scrollHost == null)
        {
            Debug.LogError("[ShopPageBuilder] 未找到 ShopScrollHost，请先执行 Build Shop Page。");
            return;
        }

        UiRectLayout hostLayout = scrollHost.GetComponent<UiRectLayout>();
        if (hostLayout != null)
        {
            SetLayout(hostLayout, UiRectLayout.LayoutPreset.FullStretch,
                new RectOffset(8, 8, ShopScrollHostPaddingTop, ShopScrollHostPaddingBottom));
        }

        Transform contentTransform = scrollHost.Find("ShopScroll/Viewport/Content");
        if (contentTransform == null)
        {
            contentTransform = FindChildRecursive(scrollHost, "Content");
        }

        if (contentTransform == null)
        {
            Debug.LogError("[ShopPageBuilder] 未找到 ShopScroll Content。");
            return;
        }

        ApplyScrollContentLayout(contentTransform as RectTransform);
        ApplyScrollSectionLayout(FindChildRecursive(contentTransform, "SupplyRow") as RectTransform, SupplyRowHeight);
        ApplyScrollSectionLayout(FindChildRecursive(contentTransform, "ExchangeSection") as RectTransform, ExchangeSectionHeight);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ShopPageBuilder] ShopScrollHost 布局已修复：内容自顶向下排列，顶栏/底栏固定不滚动。");
    }

    /// <summary>配置 ScrollView Content 的 VerticalLayoutGroup 与 ContentSizeFitter。</summary>
    /// <param name="content">Content RectTransform。</param>
    private static void ApplyScrollContentLayout(RectTransform content)
    {
        if (content == null)
        {
            return;
        }

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = ShopScrollSectionSpacing;
        layout.padding = new RectOffset(0, 0, 4, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>为 Scroll 区块设置固定高度与 LayoutElement。</summary>
    /// <param name="section">区块 RectTransform。</param>
    /// <param name="height">区块高度。</param>
    private static void ApplyScrollSectionLayout(RectTransform section, float height)
    {
        if (section == null)
        {
            return;
        }

        section.anchorMin = new Vector2(0f, 1f);
        section.anchorMax = new Vector2(1f, 1f);
        section.pivot = new Vector2(0.5f, 1f);
        section.anchoredPosition = Vector2.zero;
        section.sizeDelta = new Vector2(0f, height);

        LayoutElement element = section.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = section.gameObject.AddComponent<LayoutElement>();
        }

        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleWidth = 1f;
    }

    /// <summary>创建完整商城页面层级并返回 ShopSceneView。</summary>
    /// <param name="canvasRoot">Canvas 根节点。</param>
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
            new RectOffset(12, 12, (int)ShopTopBarPaddingTop, 0),
            new Vector2(0f, ShopTopBarHeight));
        ShopResourcePanel resourcePanel = topBar.gameObject.AddComponent<ShopResourcePanel>();
        CreateShopResources(topBar, resourcePanel);

        RectTransform scrollHost = CreateLayoutHost(
            safeArea,
            "ShopScrollHost",
            UiRectLayout.LayoutPreset.FullStretch,
            new RectOffset(8, 8, ShopScrollHostPaddingTop, ShopScrollHostPaddingBottom),
            Vector2.zero);
        ScrollRect scroll = CreateScrollArea(scrollHost);

        RectTransform supplyRow = CreateScrollSection(scroll.content, "SupplyRow", SupplyRowHeight);
        ShopSupplySectionPanel supplyPanel = supplyRow.gameObject.AddComponent<ShopSupplySectionPanel>();
        ShopCrateWidget commonCrate = CreateCrateWidget(supplyRow, "CommonCrate", new Vector2(-350f, 0f), new Vector2(330f, 780f), NeonBlue,
            GameConstants.ConfigIds.ShopCrateCommonSingle,
            GameConstants.ConfigIds.ShopCrateCommonTen,
            GameConstants.ConfigIds.ShopAdCrateCommon,
            UpgradeCardConstants.PoolIds.ShopCrateCommon,
            "普通补给箱");
        ShopCrateWidget premiumCrate = CreateCrateWidget(supplyRow, "PremiumCrate", new Vector2(0f, 0f), new Vector2(330f, 780f), NeonPurple,
            GameConstants.ConfigIds.ShopCratePremiumSingle,
            GameConstants.ConfigIds.ShopCratePremiumTen,
            GameConstants.ConfigIds.ShopAdCratePremium,
            UpgradeCardConstants.PoolIds.ShopCratePremium,
            "高级补给箱");
        ShopGoldSupplyWidget goldSupply = CreateGoldSupplyWidget(supplyRow, "GoldSupply", new Vector2(350f, 0f), new Vector2(330f, 780f));

        RectTransform exchangeRoot = CreateScrollSection(scroll.content, "ExchangeSection", ExchangeSectionHeight);
        ShopAdTicketExchangePanel exchangePanel = exchangeRoot.gameObject.AddComponent<ShopAdTicketExchangePanel>();
        CreateExchangeSection(exchangeRoot, exchangePanel);

        RectTransform statusBar = CreateLayoutHost(
            safeArea,
            "ShopStatusBar",
            UiRectLayout.LayoutPreset.BottomStretch,
            new RectOffset(16, 16, 0, (int)ShopStatusBarPaddingBottom),
            new Vector2(0f, ShopStatusBarHeight));
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

        ShopCratePopupBuilder.BuildAndWire(shell.PageRoot, shopView);

        return shopView;
    }

    /// <summary>创建商城 TopBar 四槽资源条。</summary>
    /// <param name="root">TopBar 根节点。</param>
    /// <param name="panel">ShopResourcePanel。</param>
    private static void CreateShopResources(RectTransform root, ShopResourcePanel panel)
    {
        TopResourceBarEditorUtility.Build(root, panel, TopResourceBarEditorUtility.ShopBarPositions);
    }
    /// <summary>创建补给箱 Widget（单抽/十连/广告/预览）。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="accent">强调色。</param>
    /// <param name="singleId">单抽商品 ID。</param>
    /// <param name="tenId">十连商品 ID。</param>
    /// <param name="adId">广告抽商品 ID。</param>
    /// <param name="previewPoolId">预览奖池 ID。</param>
    /// <param name="crateTitle">补给箱标题。</param>

    private static ShopCrateWidget CreateCrateWidget(
        RectTransform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color accent,
        string singleId,
        string tenId,
        string adId,
        string previewPoolId,
        string crateTitle)
    {
        Image background = CreatePanel(parent, name, position, size, PanelDeep);
        CreateText(background.rectTransform, "PanelIndex", name.Contains("Common") ? "01" : "02", 28, NeonBlue, TextAlignmentOptions.TopLeft, new Vector2(-size.x * 0.42f, size.y * 0.42f), new Vector2(80f, 40f));
        TMP_Text title = CreateText(background.rectTransform, "Title", crateTitle, 26, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, size.y * 0.34f), new Vector2(size.x - 24f, 40f));
        Image crate = CreatePanel(background.rectTransform, "CrateImage", new Vector2(0f, size.y * 0.08f), new Vector2(180f, 180f), accent);
        TMP_Text rewards = CreateText(background.rectTransform, "RewardsHint", "技能升级卡 · 属性升级卡", 18, Hex("#9EC8FF"), TextAlignmentOptions.Center, new Vector2(0f, -size.y * 0.12f), new Vector2(size.x - 24f, 60f));
        Button previewBtn = CreateActionButton(background.rectTransform, "Preview", new Vector2(0f, -size.y * 0.2f), new Vector2(size.x - 32f, 44f), "奖励预览", accent, out _);

        Button singleBtn = CreateActionButton(background.rectTransform, "SinglePull", new Vector2(0f, -size.y * 0.3f), new Vector2(size.x - 32f, 56f), "单抽", NeonBlue, out TMP_Text singleCost);
        Button tenBtn = CreateActionButton(background.rectTransform, "TenPull", new Vector2(0f, -size.y * 0.4f), new Vector2(size.x - 32f, 56f), "十连抽", accent, out TMP_Text tenCost);
        Button adBtn = CreateActionButton(background.rectTransform, "AdPull", new Vector2(0f, -size.y * 0.5f), new Vector2(size.x - 32f, 56f), "观看广告免费抽取", PanelBlue, out TMP_Text adText);

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
        so.FindProperty("previewButton").objectReferenceValue = previewBtn;
        so.FindProperty("singleItemConfigId").stringValue = singleId;
        so.FindProperty("tenItemConfigId").stringValue = tenId;
        so.FindProperty("adFreeConfigId").stringValue = adId;
        so.FindProperty("previewPoolConfigId").stringValue = previewPoolId;
        so.FindProperty("crateTitle").stringValue = crateTitle;
        so.ApplyModifiedPropertiesWithoutUndo();
        return widget;
    }

    /// <summary>创建金币补给 Widget（低/标准档与广告领取）。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
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

    /// <summary>创建广告券兑换中心 UI 并绑定面板。</summary>
    /// <param name="root">父级。</param>
    /// <param name="panel">兑换面板组件。</param>
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

    /// <summary>创建兑换列（标题 + 多行兑换项）。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">列名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="columnTitle">列标题。</param>
    /// <param name="configIds">兑换商品 ID 数组。</param>
    /// <param name="accent">强调色。</param>
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

    /// <summary>创建单行兑换项 Widget。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">行名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="configId">兑换商品 ID。</param>
    /// <param name="accent">强调色。</param>
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

    /// <summary>创建纵向 ScrollRect（Viewport + Content）。</summary>
    /// <param name="parent">Scroll 宿主。</param>
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

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = ShopScrollSectionSpacing;
        layout.padding = new RectOffset(0, 0, 4, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        return scroll;
    }

    /// <summary>ScrollView Content 子块：由 VerticalLayoutGroup 自顶向下排布。</summary>
    private static RectTransform CreateScrollSection(RectTransform content, string name, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(content, false);
        LayoutElement element = go.GetComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleWidth = 1f;
        return go.GetComponent<RectTransform>();
    }

    /// <summary>绑定 MainSceneView 的 shopPage/bottomNav/battlePage 引用。</summary>
    /// <param name="mainView">主场景视图。</param>
    /// <param name="shopView">商城视图。</param>
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

    /// <summary>按名称查找直接子节点或递归查找。</summary>
    /// <param name="parent">父 Transform。</param>
    /// <param name="name">节点名称。</param>
    private static Transform FindChild(Transform parent, string name)
    {
        Transform direct = parent != null ? parent.Find(name) : null;
        return direct != null ? direct : FindChildRecursive(parent, name);
    }
    /// <summary>创建带 UiRectLayout 的布局宿主节点。</summary>
    /// <param name="parent">父节点。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="preset">布局预设。</param>
    /// <param name="padding">内边距。</param>
    /// <param name="fixedSize">固定尺寸。</param>

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

    /// <summary>委托 UiPageStructureEditorUtility 设置布局。</summary>
    /// <param name="layout">布局组件。</param>
    /// <param name="preset">布局预设。</param>
    /// <param name="padding">内边距。</param>
    /// <param name="fixedSize">固定尺寸。</param>
    private static void SetLayout(UiRectLayout layout, UiRectLayout.LayoutPreset preset, RectOffset padding, Vector2 fixedSize = default)
    {
        UiPageStructureEditorUtility.SetLayout(layout, preset, padding, fixedSize);
    }

    /// <summary>创建居中锚点的 RectTransform 宿主。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
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

    /// <summary>创建带 Outline 的 Image 面板。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="anchoredPosition">锚点坐标。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="color">颜色。</param>
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

    /// <summary>创建 TMP 文本（自动绑定中文字体）。</summary>
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

    /// <summary>创建带标签的操作 Button。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="label">标签。</param>
    /// <param name="accent">强调色。</param>
    /// <param name="labelText">输出标签 TMP 引用。</param>
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

    /// <summary>将 ShopExchangeRowWidget 数组写入 SerializedProperty。</summary>
    /// <param name="arrayProp">数组属性。</param>
    /// <param name="items">行 Widget 数组。</param>
    private static void SetArray(SerializedProperty arrayProp, ShopExchangeRowWidget[] items)
    {
        arrayProp.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    /// <summary>递归按名称查找子节点。</summary>
    /// <param name="root">搜索根。</param>
    /// <param name="name">节点名称。</param>
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

    /// <summary>委托 StretchFull 铺满父节点。</summary>
    /// <param name="rect">目标 RectTransform。</param>
    private static void StretchToParent(RectTransform rect)
    {
        UiPageStructureEditorUtility.StretchFull(rect);
    }

    /// <summary>解析 HTML 颜色字符串为 Color。</summary>
    /// <param name="html">十六进制颜色值。</param>
    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }
}
#endif
