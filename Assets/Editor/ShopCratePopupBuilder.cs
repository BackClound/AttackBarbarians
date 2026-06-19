#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 搭建商城补给箱奖励弹窗与奖池预览弹窗，并绑定到 ShopSceneView。
/// </summary>
public static class ShopCratePopupBuilder
{
    /// <summary>菜单：在 MainScene 搭建补给箱奖励/预览弹窗。</summary>
    [MenuItem("Attack Barbarians/UI/Build Shop Crate Popups In MainScene")]
    public static void BuildShopCratePopupsInMainScene()
    {
        ShopSceneView shopView = Object.FindObjectOfType<ShopSceneView>();
        if (shopView == null)
        {
            Debug.LogError("[ShopCratePopupBuilder] 未找到 ShopSceneView，请先执行 Build Shop Page。");
            return;
        }

        BuildAndWire(shopView.transform, shopView);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shopView.gameObject.scene);
        Debug.Log("[ShopCratePopupBuilder] 商城抽取弹窗已搭建并绑定。");
    }
    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color PanelBlue = Hex("#0C2A5A");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonPurple = Hex("#A95CFF");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color TextWhite = Hex("#F5FAFF");
    private static readonly Color Scrim = new Color(0f, 0f, 0f, 0.72f);

    /// <summary>创建弹窗层级并绑定 ShopSceneView 引用。</summary>
    /// <param name="shopPageRoot">商城页根节点。</param>
    /// <param name="shopView">商城视图。</param>
    public static void BuildAndWire(Transform shopPageRoot, ShopSceneView shopView)
    {
        if (shopPageRoot == null || shopView == null)
        {
            return;
        }

        Transform existingReward = shopPageRoot.Find("ShopCrateRewardPopup");
        if (existingReward != null)
        {
            Object.DestroyImmediate(existingReward.gameObject);
        }

        Transform existingPreview = shopPageRoot.Find("ShopCratePoolPreview");
        if (existingPreview != null)
        {
            Object.DestroyImmediate(existingPreview.gameObject);
        }

        UpgradeCardDisplayView cardPrefab = CreateCardSlotPrefab(shopPageRoot);
        ShopCrateRewardPopupPanel rewardPopup = CreateRewardPopup(shopPageRoot, cardPrefab);
        ShopCratePoolPreviewPanel poolPreview = CreatePoolPreviewPopup(shopPageRoot, cardPrefab);

        SerializedObject shopSo = new SerializedObject(shopView);
        shopSo.FindProperty("crateRewardPopup").objectReferenceValue = rewardPopup;
        shopSo.FindProperty("cratePoolPreview").objectReferenceValue = poolPreview;
        shopSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>为 ShopCrateWidget 写入预览奖池与按钮引用。</summary>
    /// <param name="widget">补给箱 Widget。</param>
    /// <param name="previewPoolId">预览奖池 ID。</param>
    /// <param name="crateTitle">补给箱标题。</param>
    /// <param name="previewButton">预览按钮。</param>
    public static void WireCrateWidget(
        ShopCrateWidget widget,
        string previewPoolId,
        string crateTitle,
        Button previewButton)
    {
        if (widget == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(widget);
        so.FindProperty("previewPoolConfigId").stringValue = previewPoolId;
        so.FindProperty("crateTitle").stringValue = crateTitle;
        so.FindProperty("previewButton").objectReferenceValue = previewButton;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>创建抽取结果奖励弹窗面板。</summary>
    /// <param name="parent">父节点。</param>
    /// <param name="cardPrefab">卡片槽预制引用。</param>
    private static ShopCrateRewardPopupPanel CreateRewardPopup(Transform parent, UpgradeCardDisplayView cardPrefab)
    {
        RectTransform root = CreateStretchHost(parent, "ShopCrateRewardPopup");
        root.gameObject.SetActive(false);

        Image scrim = CreateStretchImage(root, "Scrim", Scrim);
        Button scrimButton = scrim.gameObject.AddComponent<Button>();
        scrimButton.targetGraphic = scrim;

        RectTransform panel = CreateCenterPanel(root, "Panel", new Vector2(980f, 1680f), PanelBlue);
        TMP_Text titleCn = CreateText(panel, "TitleCn", "获得奖励", 42, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 720f), new Vector2(600f, 56f));
        TMP_Text titleEn = CreateText(panel, "TitleEn", "SUPPLY ACQUIRED", 22, NeonBlue, TextAlignmentOptions.Center, new Vector2(0f, 670f), new Vector2(600f, 36f));
        TMP_Text rewardCount = CreateText(panel, "RewardCount", "共获得 0 张升级卡", 24, NeonGold, TextAlignmentOptions.Center, new Vector2(0f, 620f), new Vector2(600f, 36f));

        ScrollRect cardScroll = CreateRewardCardScroll(panel, cardPrefab, out RectTransform cardGrid, out UpgradeCardDisplayView rewardCardPrefab);

        Button previewBtn = CreateActionButton(panel, "PreviewButton", new Vector2(380f, -520f), new Vector2(180f, 56f), "预览", NeonPurple, out _);
        Button confirmBtn = CreateActionButton(panel, "ConfirmButton", new Vector2(-180f, -720f), new Vector2(240f, 64f), "确认", NeonBlue, out _);
        Button drawAgainBtn = CreateActionButton(panel, "DrawAgainButton", new Vector2(120f, -720f), new Vector2(280f, 64f), "再抽一次", NeonPurple, out TMP_Text drawAgainLabel);
        TMP_Text tapHint = CreateText(panel, "TapHint", "点击空白处关闭", 18, Hex("#9EC8FF"), TextAlignmentOptions.BottomRight, new Vector2(360f, -760f), new Vector2(280f, 32f));

        ShopCrateRewardPopupPanel popup = root.gameObject.AddComponent<ShopCrateRewardPopupPanel>();
        SerializedObject so = new SerializedObject(popup);
        so.FindProperty("root").objectReferenceValue = root.gameObject;
        so.FindProperty("scrimButton").objectReferenceValue = scrimButton;
        so.FindProperty("titleCnText").objectReferenceValue = titleCn;
        so.FindProperty("titleEnText").objectReferenceValue = titleEn;
        so.FindProperty("rewardCountText").objectReferenceValue = rewardCount;
        so.FindProperty("cardScrollRect").objectReferenceValue = cardScroll;
        so.FindProperty("cardGridRoot").objectReferenceValue = cardGrid;
        so.FindProperty("cardSlotPrefab").objectReferenceValue = rewardCardPrefab;
        so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
        so.FindProperty("drawAgainButton").objectReferenceValue = drawAgainBtn;
        so.FindProperty("drawAgainLabel").objectReferenceValue = drawAgainLabel;
        so.FindProperty("previewButton").objectReferenceValue = previewBtn;
        so.FindProperty("tapHintText").objectReferenceValue = tapHint;
        so.ApplyModifiedPropertiesWithoutUndo();
        return popup;
    }

    /// <summary>创建奖励卡片 Grid ScrollView。</summary>
    /// <param name="panel">弹窗面板。</param>
    /// <param name="cardPrefab">卡片预制。</param>
    /// <param name="cardGrid">输出 Grid 根节点。</param>
    /// <param name="rewardCardPrefab">输出槽位预制实例。</param>
    private static ScrollRect CreateRewardCardScroll(
        RectTransform panel,
        UpgradeCardDisplayView cardPrefab,
        out RectTransform cardGrid,
        out UpgradeCardDisplayView rewardCardPrefab)
    {
        RectTransform scrollHost = CreatePanelHost(panel, "RewardScrollHost", new Vector2(0f, 80f), new Vector2(900f, 900f));
        Image scrollBg = scrollHost.gameObject.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.15f);
        ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = CreateStretchHost(scrollHost, "Viewport");
        Image viewportMask = viewport.gameObject.AddComponent<Image>();
        viewportMask.color = Color.white;
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scroll.viewport = viewport;

        cardGrid = CreateStretchHost(viewport, "RewardCardGrid");
        cardGrid.anchorMin = new Vector2(0f, 1f);
        cardGrid.anchorMax = new Vector2(1f, 1f);
        cardGrid.pivot = new Vector2(0.5f, 1f);
        cardGrid.anchoredPosition = Vector2.zero;
        cardGrid.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = cardGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(260f, 340f);
        grid.spacing = new Vector2(20f, 20f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = cardGrid.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = cardGrid;

        rewardCardPrefab = CreateCardInstance(cardGrid, "RewardCardSlotPrefab", cardPrefab, Vector2.zero, grid.cellSize);
        rewardCardPrefab.gameObject.SetActive(false);
        return scroll;
    }

    /// <summary>创建奖池预览弹窗面板。</summary>
    /// <param name="parent">父节点。</param>
    /// <param name="cardPrefab">卡片槽预制。</param>
    private static ShopCratePoolPreviewPanel CreatePoolPreviewPopup(Transform parent, UpgradeCardDisplayView cardPrefab)
    {
        RectTransform root = CreateStretchHost(parent, "ShopCratePoolPreview");
        root.gameObject.SetActive(false);

        Image scrim = CreateStretchImage(root, "Scrim", Scrim);
        Button scrimButton = scrim.gameObject.AddComponent<Button>();
        scrimButton.targetGraphic = scrim;

        RectTransform panel = CreateCenterPanel(root, "Panel", new Vector2(980f, 1560f), PanelDeep);
        TMP_Text title = CreateText(panel, "Title", "抽奖预览", 38, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, 680f), new Vector2(500f, 48f));
        TMP_Text subtitle = CreateText(panel, "Subtitle", "PREVIEW", 20, NeonBlue, TextAlignmentOptions.Center, new Vector2(0f, 630f), new Vector2(500f, 32f));
        Button closeBtn = CreateActionButton(panel, "CloseButton", new Vector2(0f, -700f), new Vector2(220f, 56f), "关闭", NeonBlue, out _);

        RectTransform scrollHost = CreatePanelHost(panel, "ScrollHost", new Vector2(0f, -20f), new Vector2(920f, 1180f));
        Image scrollBg = scrollHost.gameObject.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.2f);
        ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        RectTransform viewport = CreateStretchHost(scrollHost, "Viewport");
        Image viewportMask = viewport.gameObject.AddComponent<Image>();
        viewportMask.color = Color.white;
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scroll.viewport = viewport;

        RectTransform content = CreateStretchHost(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(280f, 380f);
        grid.spacing = new Vector2(20f, 20f);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        UpgradeCardDisplayView prefabForGrid = CreateCardInstance(content, "CardSlotPrefab", cardPrefab, Vector2.zero, grid.cellSize);
        prefabForGrid.gameObject.SetActive(false);

        ShopCratePoolPreviewPanel popup = root.gameObject.AddComponent<ShopCratePoolPreviewPanel>();
        SerializedObject so = new SerializedObject(popup);
        so.FindProperty("root").objectReferenceValue = root.gameObject;
        so.FindProperty("scrimButton").objectReferenceValue = scrimButton;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subtitleText").objectReferenceValue = subtitle;
        so.FindProperty("cardGridRoot").objectReferenceValue = content;
        so.FindProperty("cardSlotPrefab").objectReferenceValue = prefabForGrid;
        so.ApplyModifiedPropertiesWithoutUndo();
        return popup;
    }

    /// <summary>创建升级卡槽位预制模板。</summary>
    /// <param name="parent">父节点。</param>
    private static UpgradeCardDisplayView CreateCardSlotPrefab(Transform parent)
    {
        RectTransform host = CreatePanelHost(parent, "UpgradeCardSlotPrefab", Vector2.zero, new Vector2(280f, 380f));
        host.gameObject.SetActive(false);
        return BuildCardView(host, "Card");
    }
    /// <summary>从预制实例化卡片槽并设置尺寸。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">实例名称。</param>
    /// <param name="prefab">源预制。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>

    private static UpgradeCardDisplayView CreateCardInstance(
        RectTransform parent,
        string name,
        UpgradeCardDisplayView prefab,
        Vector2 position,
        Vector2 size)
    {
        UpgradeCardDisplayView instance = Object.Instantiate(prefab, parent);
        instance.name = name;
        RectTransform rect = instance.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        instance.gameObject.SetActive(true);
        return instance;
    }

    /// <summary>构建 UpgradeCardDisplayView 完整卡片 UI。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    private static UpgradeCardDisplayView BuildCardView(RectTransform parent, string name)
    {
        Image border = CreatePanel(parent, name, Vector2.zero, parent.sizeDelta, PanelDeep);
        UiRarityVisual rarity = border.gameObject.AddComponent<UiRarityVisual>();
        SerializedObject raritySo = new SerializedObject(rarity);
        raritySo.FindProperty("borderImage").objectReferenceValue = border;
        raritySo.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text english = CreateText(border.rectTransform, "EnglishTitle", "Shoot", 18, NeonBlue, TextAlignmentOptions.Top, new Vector2(0f, parent.sizeDelta.y * 0.38f), new Vector2(parent.sizeDelta.x - 16f, 28f));
        Image icon = CreatePanel(border.rectTransform, "Icon", new Vector2(0f, parent.sizeDelta.y * 0.08f), new Vector2(120f, 120f), NeonBlue);
        TMP_Text chinese = CreateText(border.rectTransform, "ChineseTitle", "技能卡", 24, TextWhite, TextAlignmentOptions.Center, new Vector2(0f, -parent.sizeDelta.y * 0.18f), new Vector2(parent.sizeDelta.x - 16f, 36f));
        TMP_Text desc = CreateText(border.rectTransform, "Description", "描述", 16, Hex("#9EC8FF"), TextAlignmentOptions.Center, new Vector2(0f, -parent.sizeDelta.y * 0.28f), new Vector2(parent.sizeDelta.x - 20f, 56f));
        TMP_Text dropRate = CreateText(border.rectTransform, "DropRate", "爆率 0.0%", 18, NeonGold, TextAlignmentOptions.Center, new Vector2(0f, -parent.sizeDelta.y * 0.4f), new Vector2(parent.sizeDelta.x - 16f, 28f));
        dropRate.gameObject.SetActive(false);

        RectTransform starsRoot = CreatePanelHost(border.rectTransform, "Stars", new Vector2(0f, -parent.sizeDelta.y * 0.46f), new Vector2(120f, 24f));
        HorizontalLayoutGroup starLayout = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        starLayout.spacing = 6f;
        starLayout.childAlignment = TextAnchor.MiddleCenter;
        var stars = new Image[3];
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i] = CreatePanel(starsRoot, $"Star_{i + 1}", Vector2.zero, new Vector2(18f, 18f), NeonBlue);
        }

        UpgradeCardDisplayView view = border.gameObject.AddComponent<UpgradeCardDisplayView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("borderImage").objectReferenceValue = border;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("englishTitleText").objectReferenceValue = english;
        so.FindProperty("chineseTitleText").objectReferenceValue = chinese;
        so.FindProperty("descriptionText").objectReferenceValue = desc;
        so.FindProperty("dropRateText").objectReferenceValue = dropRate;
        so.FindProperty("rarityVisual").objectReferenceValue = rarity;
        SerializedProperty starsProp = so.FindProperty("starImages");
        starsProp.arraySize = stars.Length;
        for (int i = 0; i < stars.Length; i++)
        {
            starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    /// <summary>创建全拉伸 RectTransform 宿主。</summary>
    /// <param name="parent">父节点。</param>
    /// <param name="name">节点名称。</param>
    private static RectTransform CreateStretchHost(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        UiPageStructureEditorUtility.StretchFull(rect);
        return rect;
    }

    /// <summary>创建全拉伸 Image。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="color">颜色。</param>
    private static Image CreateStretchImage(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        UiPageStructureEditorUtility.StretchFull(rect);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>创建居中面板 Image。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="color">颜色。</param>
    private static RectTransform CreateCenterPanel(RectTransform parent, string name, Vector2 size, Color color)
    {
        Image panel = CreatePanel(parent, name, Vector2.zero, size, color);
        return panel.rectTransform;
    }

    /// <summary>创建居中锚点 RectTransform 宿主。</summary>
    /// <param name="parent">父节点。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    private static RectTransform CreatePanelHost(Transform parent, string name, Vector2 position, Vector2 size)
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

    /// <summary>创建 TMP 文本。</summary>
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

    /// <summary>创建操作 Button。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="label">标签。</param>
    /// <param name="accent">强调色。</param>
    /// <param name="labelText">输出标签引用。</param>
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

    /// <summary>解析 HTML 颜色字符串。</summary>
    /// <param name="html">十六进制颜色值。</param>
    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }
}
#endif
