#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在 MainScene 中搭建科技 Buff 解锁页面，并绑定 MainSceneView 页面切换。
/// </summary>
public static class TechPageBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    private const float StatusBarHeight = 44f;
    private const float SectionHeaderHeight = 48f;
    private const float PathRowHeight = 200f;
    private const float NodeWidth = 148f;
    private const float NodeHeight = 132f;
    private const float CardSlotSize = 148f;

    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color PanelBlue = Hex("#0C2A5A");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color TextWhite = Hex("#F5FAFF");

    /// <summary>菜单：在 MainScene 搭建科技 Buff 解锁页。</summary>
    [MenuItem("Attack Barbarians/UI/Build Tech Buff Unlock Page In MainScene")]
    public static void BuildTechPageInMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        MainSceneView mainView = Object.FindObjectOfType<MainSceneView>();
        if (mainView == null)
        {
            Debug.LogError("[TechPageBuilder] 未找到 MainSceneView。");
            return;
        }

        RectTransform canvasRoot = mainView.transform as RectTransform;
        Transform existing = FindChildRecursive(canvasRoot, "TechPageRoot");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        TechSceneView techView = CreateTechPage(canvasRoot);
        WireMainSceneView(mainView, techView);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TechPageBuilder] 科技 Buff 解锁页面已搭建并绑定到 MainScene。");
    }

    /// <summary>菜单：向当前存档注入测试用 Buff 解锁卡。</summary>
    [MenuItem("Attack Barbarians/Debug/Seed Buff Unlock Test Cards")]
    public static void SeedBuffUnlockTestCards()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[TechPageBuilder] 请在 Play 模式下执行，或自行调用 BuffUnlockService.TryAddCard。");
            return;
        }

        if (!ServiceLocator.TryGet(out BuffUnlockService service))
        {
            Debug.LogError("[TechPageBuilder] BuffUnlockService 未就绪。");
            return;
        }

        service.TryAddCard(BuffUnlockCardConstants.Global, 20);
        SkillType[] skills = BuffUnlockSkillDefinitions.DisplayOrder;
        for (int i = 0; i < skills.Length; i++)
        {
            string cardId = BuffUnlockCardConstants.GetSkillCardId(skills[i]);
            if (!string.IsNullOrWhiteSpace(cardId))
            {
                service.TryAddCard(cardId, 8);
            }
        }

        Debug.Log("[TechPageBuilder] 已注入测试 Buff 解锁卡。");
    }

    private static TechSceneView CreateTechPage(RectTransform canvasRoot)
    {
        UiPageStructureEditorUtility.PageShell shell = UiPageStructureEditorUtility.CreatePageShell(
            canvasRoot,
            "TechPageRoot",
            typeof(TechSceneView),
            UiPageStructureEditorUtility.DefaultPageBackground);
        shell.PageRoot.gameObject.SetActive(false);

        TechSceneView techView = shell.PageRoot.GetComponent<TechSceneView>();
        RectTransform safeArea = shell.SafeAreaRoot;

        RectTransform pathSection = CreateAnchoredSection(safeArea, "PathSection", new Vector2(0f, 0.4f), Vector2.one);
        RectTransform bagSection = CreateAnchoredSection(safeArea, "CardBagSection", Vector2.zero, new Vector2(1f, 0.4f));

        TMP_Text pathTitle = CreateStretchText(pathSection, "PathTitle", "技能 Buff 解锁路线", 28, TextWhite, TextAlignmentOptions.MidlineLeft,
            new Vector2(24f, -8f), new Vector2(-24f, -SectionHeaderHeight));
        ScrollRect pathScroll = CreateVerticalScroll(pathSection, "PathScroll",
            new Vector2(12f, -SectionHeaderHeight), new Vector2(-12f, -8f), out RectTransform pathContent);
        TechBuffUnlockPathPanel pathPanel = pathSection.gameObject.AddComponent<TechBuffUnlockPathPanel>();
        BuffUnlockSkillRowView rowPrefab = CreateRowPrefab(shell.PageRoot);
        TMP_Text pathEmpty = CreateStretchText(pathContent, "EmptyHint", "暂无可展示的技能路径", 22, NeonBlue, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.zero);
        pathEmpty.gameObject.SetActive(false);

        TMP_Text bagTitle = CreateStretchText(bagSection, "BagTitle", "Buff 解锁卡背包", 28, TextWhite, TextAlignmentOptions.MidlineLeft,
            new Vector2(24f, -8f), new Vector2(-24f, -SectionHeaderHeight));
        ScrollRect bagScroll = CreateVerticalScroll(bagSection, "BagScroll",
            new Vector2(12f, -SectionHeaderHeight), new Vector2(-12f, -StatusBarHeight - 8f), out RectTransform bagContent);
        GridLayoutGroup bagGrid = bagContent.gameObject.AddComponent<GridLayoutGroup>();
        bagGrid.cellSize = new Vector2(CardSlotSize, CardSlotSize + 36f);
        bagGrid.spacing = new Vector2(12f, 12f);
        bagGrid.padding = new RectOffset(8, 8, 8, 8);
        bagGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bagGrid.constraintCount = 5;
        ContentSizeFitter bagFitter = bagContent.gameObject.AddComponent<ContentSizeFitter>();
        bagFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TechBuffUnlockCardBagPanel bagPanel = bagSection.gameObject.AddComponent<TechBuffUnlockCardBagPanel>();
        BuffUnlockCardSlotView slotPrefab = CreateCardSlotPrefab(shell.PageRoot);
        TMP_Text bagEmpty = CreateStretchText(bagContent, "EmptyHint", "背包暂无解锁卡", 22, NeonBlue, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.zero);
        bagEmpty.gameObject.SetActive(false);

        RectTransform statusBar = CreateLayoutHost(
            safeArea,
            "TechStatusBar",
            UiRectLayout.LayoutPreset.BottomStretch,
            new RectOffset(16, 16, 0, 8),
            new Vector2(0f, StatusBarHeight));
        TMP_Text statusText = CreateStretchText(statusBar, "TechStatusText", string.Empty, 22, NeonGold, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.zero);
        statusText.gameObject.SetActive(false);

        SerializedObject pathSo = new SerializedObject(pathPanel);
        pathSo.FindProperty("sectionTitleText").objectReferenceValue = pathTitle;
        pathSo.FindProperty("scrollRect").objectReferenceValue = pathScroll;
        pathSo.FindProperty("rowHost").objectReferenceValue = pathContent;
        pathSo.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
        pathSo.FindProperty("emptyHintText").objectReferenceValue = pathEmpty;
        pathSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bagSo = new SerializedObject(bagPanel);
        bagSo.FindProperty("sectionTitleText").objectReferenceValue = bagTitle;
        bagSo.FindProperty("scrollRect").objectReferenceValue = bagScroll;
        bagSo.FindProperty("slotHost").objectReferenceValue = bagContent;
        bagSo.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        bagSo.FindProperty("emptyHintText").objectReferenceValue = bagEmpty;
        bagSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject techSo = new SerializedObject(techView);
        techSo.FindProperty("backgroundImage").objectReferenceValue = shell.BackgroundImage;
        techSo.FindProperty("pathPanel").objectReferenceValue = pathPanel;
        techSo.FindProperty("cardBagPanel").objectReferenceValue = bagPanel;
        techSo.FindProperty("statusText").objectReferenceValue = statusText;
        techSo.ApplyModifiedPropertiesWithoutUndo();

        ApplyPathContentLayout(pathContent);
        return techView;
    }

    private static BuffUnlockSkillRowView CreateRowPrefab(Transform pageRoot)
    {
        RectTransform rowHost = CreatePanelHost(pageRoot, "BuffUnlockRowPrefab", Vector2.zero, new Vector2(1000f, PathRowHeight));
        rowHost.gameObject.SetActive(false);

        TMP_Text skillName = CreateText(rowHost, "SkillName", "射击", 24, TextWhite, TextAlignmentOptions.MidlineLeft,
            new Vector2(-470f, 70f), new Vector2(900f, 36f));

        RectTransform nodeHost = CreatePanelHost(rowHost, "NodeHost", new Vector2(0f, -20f), new Vector2(980f, NodeHeight + 40f));
        HorizontalLayoutGroup layout = nodeHost.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        BuffUnlockPathNodeView nodePrefab = CreateNodePrefab(nodeHost);
        nodePrefab.gameObject.SetActive(false);

        BuffUnlockSkillRowView row = rowHost.gameObject.AddComponent<BuffUnlockSkillRowView>();
        LayoutElement rowLayout = rowHost.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = PathRowHeight;
        rowLayout.preferredHeight = PathRowHeight;
        SerializedObject rowSo = new SerializedObject(row);
        rowSo.FindProperty("skillNameText").objectReferenceValue = skillName;
        rowSo.FindProperty("nodeHost").objectReferenceValue = nodeHost;
        rowSo.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
        rowSo.ApplyModifiedPropertiesWithoutUndo();
        return row;
    }

    private static BuffUnlockPathNodeView CreateNodePrefab(Transform parent)
    {
        Image bg = CreatePanel(parent as RectTransform, "BuffUnlockNodePrefab", Vector2.zero, new Vector2(NodeWidth, NodeHeight), PanelDeep);
        LayoutElement element = bg.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = NodeWidth;
        element.preferredHeight = NodeHeight + 36f;

        Image stripe = CreatePanel(bg.rectTransform, "TypeStripe", new Vector2(0f, NodeHeight * 0.42f), new Vector2(NodeWidth - 8f, 6f), NeonBlue);
        Image icon = CreatePanel(bg.rectTransform, "Icon", new Vector2(0f, 18f), new Vector2(72f, 72f), PanelBlue);
        TMP_Text title = CreateText(bg.rectTransform, "Title", "攻速+10%", 18, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, -28f), new Vector2(NodeWidth - 12f, 44f));
        TMP_Text subtitle = CreateText(bg.rectTransform, "Subtitle", "通用 Buff", 14, NeonBlue, TextAlignmentOptions.Top, new Vector2(0f, -58f), new Vector2(NodeWidth - 12f, 24f));
        TMP_Text cost = CreateText(bg.rectTransform, "Cost", "需 1 张解锁卡", 16, NeonGold, TextAlignmentOptions.Bottom, new Vector2(0f, -NodeHeight * 0.42f), new Vector2(NodeWidth - 8f, 28f));
        Button unlockBtn = bg.gameObject.AddComponent<Button>();
        unlockBtn.targetGraphic = bg;

        GameObject locked = CreateOverlay(bg.rectTransform, "LockedOverlay", new Color(0f, 0f, 0f, 0.45f));
        GameObject unlocked = CreateOverlay(bg.rectTransform, "UnlockedMark", new Color(0.1f, 0.5f, 0.35f, 0.35f));
        TMP_Text markText = CreateText(unlocked.transform as RectTransform, "Label", "✓", 28, TextWhite, TextAlignmentOptions.Center, Vector2.zero, new Vector2(60f, 40f));

        BuffUnlockPathNodeView view = bg.gameObject.AddComponent<BuffUnlockPathNodeView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("backgroundImage").objectReferenceValue = bg;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("typeStripeImage").objectReferenceValue = stripe;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subtitleText").objectReferenceValue = subtitle;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("unlockButton").objectReferenceValue = unlockBtn;
        so.FindProperty("lockedOverlay").objectReferenceValue = locked;
        so.FindProperty("unlockedMark").objectReferenceValue = unlocked;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static BuffUnlockCardSlotView CreateCardSlotPrefab(Transform pageRoot)
    {
        Image bg = CreatePanel(pageRoot as RectTransform, "BuffUnlockCardSlotPrefab", Vector2.zero, new Vector2(CardSlotSize, CardSlotSize), PanelDeep);
        bg.gameObject.SetActive(false);
        TMP_Text title = CreateText(bg.rectTransform, "Title", "通用卡", 16, TextWhite, TextAlignmentOptions.Top, new Vector2(0f, 42f), new Vector2(CardSlotSize - 8f, 40f));
        Image icon = CreatePanel(bg.rectTransform, "Icon", new Vector2(0f, 8f), new Vector2(72f, 72f), PanelBlue);
        TMP_Text count = CreateText(bg.rectTransform, "Count", "9", 20, TextWhite, TextAlignmentOptions.BottomRight,
            new Vector2(CardSlotSize * 0.38f, -CardSlotSize * 0.38f), new Vector2(48f, 28f));

        BuffUnlockCardSlotView view = bg.gameObject.AddComponent<BuffUnlockCardSlotView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("backgroundImage").objectReferenceValue = bg;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("countText").objectReferenceValue = count;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static void ApplyPathContentLayout(RectTransform content)
    {
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(4, 4, 4, 12);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static RectTransform CreateAnchoredSection(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.04f, 0.06f, 0.1f, 0.35f);
        image.raycastTarget = false;
        return rect;
    }

    private static ScrollRect CreateVerticalScroll(RectTransform parent, string name, Vector2 offsetMin, Vector2 offsetMax, out RectTransform content)
    {
        GameObject scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = offsetMin;
        scrollRect.offsetMax = offsetMax;
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewport = viewportGo.GetComponent<RectTransform>();
        StretchToParent(viewport);
        viewportGo.GetComponent<Image>().color = Color.clear;
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return scroll;
    }

    private static GameObject CreateOverlay(RectTransform parent, string name, Color color)
    {
        Image image = CreatePanel(parent, name, Vector2.zero, Vector2.zero, color);
        StretchToParent(image.rectTransform);
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image.gameObject;
    }

    private static TMP_Text CreateStretchText(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    private static void WireMainSceneView(MainSceneView mainView, TechSceneView techView)
    {
        RectTransform canvasRoot = mainView.transform as RectTransform;
        SerializedObject mainSo = new SerializedObject(mainView);
        mainSo.FindProperty("techPage").objectReferenceValue = techView;
        mainSo.FindProperty("bottomNavCardPanel").objectReferenceValue =
            FindChildRecursive(canvasRoot, "BottomNav")?.GetComponent<MainSceneCardPanel>();
        mainSo.FindProperty("battlePage").objectReferenceValue =
            FindChild(canvasRoot, "BattlePagePanelRoot")?.GetComponent<MainSceneBattlePageView>();
        mainSo.FindProperty("shopPage").objectReferenceValue =
            FindChild(canvasRoot, "ShopPageRoot")?.GetComponent<ShopSceneView>();
        mainSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindChild(Transform parent, string name)
    {
        Transform direct = parent != null ? parent.Find(name) : null;
        return direct != null ? direct : FindChildRecursive(parent, name);
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

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

    private static RectTransform CreateLayoutHost(
        Transform parent,
        string name,
        UiRectLayout.LayoutPreset preset,
        RectOffset padding,
        Vector2 fixedSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(UiRectLayout));
        go.transform.SetParent(parent, false);
        UiPageStructureEditorUtility.SetLayout(go.GetComponent<UiRectLayout>(), preset, padding, fixedSize);
        return go.GetComponent<RectTransform>();
    }

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
        outline.effectColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.35f);
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

    private static void StretchToParent(RectTransform rect)
    {
        UiPageStructureEditorUtility.StretchFull(rect);
    }

    private static Color Hex(string html) =>
        ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
}
#endif
