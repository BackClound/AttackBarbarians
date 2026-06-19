#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 将 MainScene UI 重构为：BattlePagePanelRoot / ShopPageRoot / 公共 BottomNav。
/// </summary>
public static class MainSceneUiStructureRefactor
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    /// <summary>菜单：将 MainScene UI 重构为 BattlePage/ShopPage/BottomNav 结构。</summary>

    [MenuItem("Attack Barbarians/UI/Refactor MainScene Page Structure")]
    public static void RefactorMainScenePageStructure()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        MainSceneView mainView = Object.FindObjectOfType<MainSceneView>();
        if (mainView == null)
        {
            Debug.LogError("[MainSceneUiStructureRefactor] 未找到 MainSceneView。");
            return;
        }

        RectTransform canvasRoot = mainView.transform as RectTransform;
        Transform background = FindChild(canvasRoot, "SpaceCityBackground");
        Transform safeArea = FindChild(canvasRoot, "SafeAreaRoot");
        Transform bottomNav = FindChildRecursive(canvasRoot, "BottomNav");
        Transform shopPage = FindChildRecursive(canvasRoot, "ShopPageRoot");
        ShopSceneView shopView = shopPage != null ? shopPage.GetComponent<ShopSceneView>() : null;

        if (safeArea == null)
        {
            Debug.LogError("[MainSceneUiStructureRefactor] 未找到 SafeAreaRoot。");
            return;
        }

        CaptureBattleBindingsFromScene(safeArea, background, out BattleBindings bindings);

        Transform battleRoot = FindChild(canvasRoot, "BattlePagePanelRoot");
        if (battleRoot == null)
        {
            GameObject battleGo = new GameObject("BattlePagePanelRoot", typeof(RectTransform), typeof(MainSceneBattlePageView), typeof(UiRectLayout));
            battleGo.transform.SetParent(canvasRoot, false);
            battleRoot = battleGo.transform;
            StretchFull(battleGo.GetComponent<RectTransform>());
            SetLayout(battleGo.GetComponent<UiRectLayout>(), UiRectLayout.LayoutPreset.FullStretch, new RectOffset());
        }

        if (background != null)
        {
            background.SetParent(battleRoot, false);
            StretchFull(background as RectTransform);
        }

        safeArea.SetParent(battleRoot, false);
        StretchFull(safeArea as RectTransform);
        EnsureSafeAreaBottomInset(safeArea as RectTransform);

        if (shopPage != null)
        {
            shopPage.SetParent(canvasRoot, false);
            StretchFull(shopPage as RectTransform);
            UiRectLayout shopLayout = shopPage.GetComponent<UiRectLayout>();
            if (shopLayout == null)
            {
                shopLayout = shopPage.gameObject.AddComponent<UiRectLayout>();
            }

            SetLayout(shopLayout, UiRectLayout.LayoutPreset.FullStretch, new RectOffset());
            EnsureShopPageShell(shopPage);
        }

        if (bottomNav != null)
        {
            UiPageStructureEditorUtility.PinBottomNavToCanvas(canvasRoot, bottomNav);
        }

        MainSceneBattlePageView battlePageView = battleRoot.GetComponent<MainSceneBattlePageView>();
        ApplyBattleBindings(battlePageView, bindings);

        SerializedObject mainSo = new SerializedObject(mainView);
        mainSo.FindProperty("canvas").objectReferenceValue = mainView.GetComponent<Canvas>();
        mainSo.FindProperty("battlePage").objectReferenceValue = battlePageView;
        mainSo.FindProperty("shopPage").objectReferenceValue = shopView;
        mainSo.FindProperty("bottomNavCardPanel").objectReferenceValue =
            bottomNav != null ? bottomNav.GetComponent<MainSceneCardPanel>() : null;
        mainSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainSceneUiStructureRefactor] 页面结构已重构：BattlePagePanelRoot / ShopPageRoot / BottomNav。");
    }

    /// <summary>战斗页 UI 组件绑定集合，供结构重构时暂存引用。</summary>
    private struct BattleBindings
    {
        public string battleSceneName;
        public bool loadBattleSceneOnStart;
        public Image backgroundImage;
        public TMP_Text statusText;
        public MainSceneProfilePanel profilePanel;
        public MainSceneResourcePanel resourcePanel;
        public MainSceneCardPanel topSystemCardPanel;
        public MainSceneCardPanel promotionCardPanel;
        public MainSceneCardPanel leftSideCardPanel;
        public MainSceneCardPanel rightSideCardPanel;
        public GeneralCardPanel startBattleCard;
        public MainSceneRewardPanel rewardPanel;
    }

    /// <summary>从现有 SafeArea 采集战斗页绑定引用。</summary>
    /// <param name="safeArea">SafeArea 根节点。</param>
    /// <param name="background">背景 Transform。</param>
    /// <param name="bindings">输出绑定集合。</param>
    private static void CaptureBattleBindingsFromScene(Transform safeArea, Transform background, out BattleBindings bindings)
    {
        bindings = new BattleBindings
        {
            battleSceneName = "BattleScene",
            loadBattleSceneOnStart = true,
            backgroundImage = background != null ? background.GetComponent<Image>() : null,
            statusText = FindChildRecursive(safeArea, "StatusText")?.GetComponent<TMP_Text>(),
            profilePanel = FindChild(safeArea, "TopProfile")?.GetComponent<MainSceneProfilePanel>(),
            resourcePanel = FindChild(safeArea, "TopResources")?.GetComponent<MainSceneResourcePanel>(),
            topSystemCardPanel = FindChild(safeArea, "TopSystemIcons")?.GetComponent<MainSceneCardPanel>(),
            promotionCardPanel = FindChild(safeArea, "PromotionIcons")?.GetComponent<MainSceneCardPanel>(),
            leftSideCardPanel = FindChild(safeArea, "LeftSideIcons")?.GetComponent<MainSceneCardPanel>(),
            rightSideCardPanel = FindChild(safeArea, "RightSideIcons")?.GetComponent<MainSceneCardPanel>(),
            startBattleCard = FindChildRecursive(safeArea, "StartBattleButton")?.GetComponent<GeneralCardPanel>(),
            rewardPanel = FindChild(safeArea, "RewardRow")?.GetComponent<MainSceneRewardPanel>(),
        };
    }

    /// <summary>将采集的绑定写回 MainSceneBattlePageView。</summary>
    /// <param name="battlePage">战斗页视图。</param>
    /// <param name="bindings">绑定集合。</param>
    private static void ApplyBattleBindings(MainSceneBattlePageView battlePage, BattleBindings bindings)
    {
        if (battlePage == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(battlePage);
        so.FindProperty("battleSceneName").stringValue = bindings.battleSceneName;
        so.FindProperty("loadBattleSceneOnStart").boolValue = bindings.loadBattleSceneOnStart;
        so.FindProperty("backgroundImage").objectReferenceValue = bindings.backgroundImage;
        so.FindProperty("statusText").objectReferenceValue = bindings.statusText;
        so.FindProperty("profilePanel").objectReferenceValue = bindings.profilePanel;
        so.FindProperty("resourcePanel").objectReferenceValue = bindings.resourcePanel;
        so.FindProperty("topSystemCardPanel").objectReferenceValue = bindings.topSystemCardPanel;
        so.FindProperty("promotionCardPanel").objectReferenceValue = bindings.promotionCardPanel;
        so.FindProperty("leftSideCardPanel").objectReferenceValue = bindings.leftSideCardPanel;
        so.FindProperty("rightSideCardPanel").objectReferenceValue = bindings.rightSideCardPanel;
        so.FindProperty("startBattleCard").objectReferenceValue = bindings.startBattleCard;
        so.FindProperty("rewardPanel").objectReferenceValue = bindings.rewardPanel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>按名称查找直接子节点或递归查找。</summary>
    /// <param name="parent">父 Transform。</param>
    /// <param name="name">节点名称。</param>
    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        Transform direct = parent.Find(name);
        return direct != null ? direct : FindChildRecursive(parent, name);
    }

    /// <summary>递归按名称查找子节点。</summary>
    /// <param name="root">搜索根节点。</param>
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

    /// <summary>为 SafeArea 设置底栏预留的 UiRectLayout 边距。</summary>
    /// <param name="safeArea">SafeArea RectTransform。</param>
    private static void EnsureSafeAreaBottomInset(RectTransform safeArea)
    {
        if (safeArea == null)
        {
            return;
        }

        UiRectLayout layout = safeArea.GetComponent<UiRectLayout>();
        if (layout == null)
        {
            layout = safeArea.gameObject.AddComponent<UiRectLayout>();
        }

        SetLayout(
            layout,
            UiRectLayout.LayoutPreset.FullStretch,
            new RectOffset(0, 0, 0, UiPageStructureEditorUtility.SafeAreaBottomInset));
    }

    /// <summary>确保商城页具备 SafeArea 与背景壳层结构。</summary>
    /// <param name="shopPage">商城页根节点。</param>
    private static void EnsureShopPageShell(Transform shopPage)
    {
        Transform existingSafe = FindChild(shopPage, "SafeAreaRoot");
        if (existingSafe != null)
        {
            EnsureSafeAreaBottomInset(existingSafe as RectTransform);
            return;
        }

        Image bg = shopPage.GetComponentInChildren<Image>(true);
        Transform bgTransform = bg != null ? bg.transform : null;

        GameObject safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(UiSafeAreaFitter), typeof(UiRectLayout));
        safeGo.transform.SetParent(shopPage, false);
        StretchFull(safeGo.GetComponent<RectTransform>());
        EnsureSafeAreaBottomInset(safeGo.GetComponent<RectTransform>());

        if (bgTransform == null)
        {
            UiPageStructureEditorUtility.CreateStretchBackground(
                shopPage,
                "ShopPageBackground",
                UiPageStructureEditorUtility.DefaultPageBackground);
        }
        else if (bgTransform.parent == shopPage)
        {
            bgTransform.SetAsFirstSibling();
        }

        for (int i = shopPage.childCount - 1; i >= 0; i--)
        {
            Transform child = shopPage.GetChild(i);
            if (child == safeGo.transform || child.name.Contains("Background"))
            {
                continue;
            }

            child.SetParent(safeGo.transform, true);
        }
    }

    /// <summary>委托 UiPageStructureEditorUtility 拉伸 RectTransform。</summary>
    /// <param name="rect">目标 RectTransform。</param>
    private static void StretchFull(RectTransform rect)
    {
        UiPageStructureEditorUtility.StretchFull(rect);
    }
    /// <summary>委托 UiPageStructureEditorUtility 设置 UiRectLayout。</summary>
    /// <param name="layout">布局组件。</param>
    /// <param name="preset">布局预设。</param>
    /// <param name="padding">内边距。</param>
    /// <param name="fixedSize">固定尺寸。</param>

    private static void SetLayout(UiRectLayout layout, UiRectLayout.LayoutPreset preset, RectOffset padding, Vector2 fixedSize = default)
    {
        UiPageStructureEditorUtility.SetLayout(layout, preset, padding, fixedSize);
    }
}
#endif
