using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城补给箱抽取结果弹窗：九宫格 / 瀑布流展示多张升级卡。
/// </summary>
public class ShopCrateRewardPopupPanel : MonoBehaviour
{
    private const int GridColumnCount = 3;
    private static readonly Vector2 GridCellSize = new Vector2(260f, 340f);

    [SerializeField] private GameObject root;
    [SerializeField] private Button scrimButton;
    [SerializeField] private TMP_Text titleCnText;
    [SerializeField] private TMP_Text titleEnText;
    [SerializeField] private TMP_Text rewardCountText;
    [SerializeField] private ScrollRect cardScrollRect;
    [SerializeField] private Transform cardGridRoot;
    [SerializeField] private UpgradeCardDisplayView cardSlotPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button drawAgainButton;
    [SerializeField] private TMP_Text drawAgainLabel;
    [SerializeField] private Button previewButton;
    [SerializeField] private TMP_Text tapHintText;

    private readonly List<UpgradeCardDisplayView> spawnedSlots = new List<UpgradeCardDisplayView>(12);
    private bool rewardGridInitialized;
    private string lastPurchaseConfigId;
    private string lastPoolConfigId;
    private string lastCrateTitle;
    private Action<string, string> onPreviewRequested;
    private Action<string> onDrawAgainRequested;

    /// <summary>绑定遮罩、确认、再抽与预览按钮，默认隐藏。</summary>
    private void Awake()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.AddListener(Hide);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(Hide);
        }

        if (drawAgainButton != null)
        {
            drawAgainButton.onClick.AddListener(OnDrawAgainClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.AddListener(OnPreviewClicked);
        }

        Hide();
    }

    /// <summary>解绑所有弹窗按钮。</summary>
    private void OnDestroy()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(Hide);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Hide);
        }

        if (drawAgainButton != null)
        {
            drawAgainButton.onClick.RemoveListener(OnDrawAgainClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.RemoveListener(OnPreviewClicked);
        }
    }

    /// <summary>绑定预览与再抽一次的外部回调。</summary>
    /// <param name="previewRequested">预览按钮回调。</param>
    /// <param name="drawAgainRequested">再抽一次回调。</param>
    public void BindCallbacks(Action<string, string> previewRequested, Action<string> drawAgainRequested)
    {
        onPreviewRequested = previewRequested;
        onDrawAgainRequested = drawAgainRequested;
    }

    /// <summary>展示抽取结果：填充九宫格升级卡并显示数量。</summary>
    /// <param name="args">升级卡发放事件参数。</param>
    /// <param name="purchaseConfigId">本次购买配置 ID，用于再抽。</param>
    /// <param name="poolConfigId">奖池 ID，用于预览。</param>
    /// <param name="crateTitle">补给箱标题。</param>
    public void Show(
        UpgradeCardGrantedEventArgs args,
        string purchaseConfigId,
        string poolConfigId,
        string crateTitle)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        lastPurchaseConfigId = purchaseConfigId;
        lastPoolConfigId = poolConfigId;
        lastCrateTitle = crateTitle;

        if (titleCnText != null)
        {
            titleCnText.text = "获得奖励";
        }

        if (titleEnText != null)
        {
            titleEnText.text = "SUPPLY ACQUIRED";
        }

        if (tapHintText != null)
        {
            tapHintText.text = "点击空白处关闭";
        }

        if (drawAgainLabel != null)
        {
            drawAgainLabel.text = "再抽一次";
        }

        EnsureRewardGridReady();

        int count = args.Grants?.Count ?? 0;
        if (rewardCountText != null)
        {
            rewardCountText.text = count > 0 ? $"共获得 {count} 张升级卡" : "未获得升级卡";
        }

        PopulateRewardGrid(args);
        ResetScrollPosition();

        if (drawAgainButton != null)
        {
            drawAgainButton.interactable = !string.IsNullOrWhiteSpace(purchaseConfigId);
        }
    }

    /// <summary>关闭抽取结果弹窗。</summary>
    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    /// <summary>确保奖励 ScrollView 网格已初始化。</summary>
    private void EnsureRewardGridReady()
    {
        if (rewardGridInitialized && cardGridRoot != null && cardSlotPrefab != null)
        {
            return;
        }

        Transform panel = transform.Find("Panel");
        if (panel == null)
        {
            panel = transform;
        }

        HideLegacyRewardRoots(panel);

        if (cardGridRoot == null || cardScrollRect == null)
        {
            Transform scrollHost = panel.Find("RewardScrollHost");
            if (scrollHost == null)
            {
                scrollHost = CreateRuntimeScrollHost(panel);
            }

            cardScrollRect = scrollHost.GetComponent<ScrollRect>();
            cardGridRoot = scrollHost.Find("Viewport/RewardCardGrid");
        }

        if (cardSlotPrefab == null && cardGridRoot != null)
        {
            cardSlotPrefab = cardGridRoot.GetComponentInChildren<UpgradeCardDisplayView>(true);
        }

        if (cardSlotPrefab == null)
        {
            Transform legacyCard = panel.Find("SingleDrawRoot/SingleCard");
            if (legacyCard != null)
            {
                cardSlotPrefab = legacyCard.GetComponent<UpgradeCardDisplayView>();
            }
        }

        if (cardSlotPrefab == null)
        {
            cardSlotPrefab = GetComponentInChildren<UpgradeCardDisplayView>(true);
        }

        if (cardSlotPrefab != null)
        {
            cardSlotPrefab.gameObject.SetActive(false);
        }

        rewardGridInitialized = cardGridRoot != null && cardSlotPrefab != null;
    }

    /// <summary>隐藏旧版单抽/十连展示根节点。</summary>
    private static void HideLegacyRewardRoots(Transform panel)
    {
        SetInactiveIfExists(panel, "SingleDrawRoot");
        SetInactiveIfExists(panel, "TenDrawRoot");
    }

    /// <summary>若子节点存在则设为不活跃。</summary>
    private static void SetInactiveIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    /// <summary>运行时创建奖励卡片滚动网格（兼容旧 Prefab）。</summary>
    private Transform CreateRuntimeScrollHost(Transform panel)
    {
        GameObject scrollHostGo = new GameObject(
            "RewardScrollHost",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ScrollRect));
        scrollHostGo.transform.SetParent(panel, false);

        RectTransform scrollHostRect = scrollHostGo.GetComponent<RectTransform>();
        scrollHostRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollHostRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollHostRect.pivot = new Vector2(0.5f, 0.5f);
        scrollHostRect.anchoredPosition = new Vector2(0f, 80f);
        scrollHostRect.sizeDelta = new Vector2(900f, 900f);

        Image scrollBg = scrollHostGo.GetComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.15f);

        ScrollRect scroll = scrollHostGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportGo = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Mask));
        viewportGo.transform.SetParent(scrollHostGo.transform, false);
        RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportGo.GetComponent<Image>().color = Color.white;
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewportRect;

        GameObject contentGo = new GameObject(
            "RewardCardGrid",
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = contentGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = GridCellSize;
        grid.spacing = new Vector2(20f, 20f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridColumnCount;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        cardScrollRect = scroll;
        cardGridRoot = contentRect.transform;
        return scrollHostGo.transform;
    }

    /// <summary>根据发放列表实例化并填充奖励卡片。</summary>
    private void PopulateRewardGrid(UpgradeCardGrantedEventArgs args)
    {
        ClearSpawnedSlots();

        if (args.Grants == null || args.Grants.Count == 0)
        {
            Debug.LogWarning("[ShopCrateRewardPopupPanel] 奖励列表为空。");
            return;
        }

        if (!rewardGridInitialized || cardSlotPrefab == null || cardGridRoot == null)
        {
            Debug.LogError(
                "[ShopCrateRewardPopupPanel] 奖励网格未就绪。" +
                "请执行菜单 Attack Barbarians → UI → Build Shop Crate Popups In MainScene");
            return;
        }

        List<UpgradeCardGrantEntry> displayGrants = ExpandGrants(args.Grants);
        for (int i = 0; i < displayGrants.Count; i++)
        {
            UpgradeCardGrantEntry grant = displayGrants[i];
            UpgradeCardDisplayView slot = Instantiate(cardSlotPrefab, cardGridRoot);
            slot.gameObject.SetActive(true);
            slot.SetFromGrant(grant);
            spawnedSlots.Add(slot);
        }

        if (cardGridRoot is RectTransform gridRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }
    }

    /// <summary>将 Count>1 的发放条目展开为多张卡片。</summary>
    private static List<UpgradeCardGrantEntry> ExpandGrants(IReadOnlyList<UpgradeCardGrantEntry> grants)
    {
        var expanded = new List<UpgradeCardGrantEntry>(grants.Count);
        for (int i = 0; i < grants.Count; i++)
        {
            UpgradeCardGrantEntry grant = grants[i];
            int copies = Mathf.Max(1, grant.Count);
            for (int copy = 0; copy < copies; copy++)
            {
                expanded.Add(new UpgradeCardGrantEntry(grant.CardConfigId, grant.DisplayName, 1));
            }
        }

        return expanded;
    }

    /// <summary>重置滚动视图到顶部。</summary>
    private void ResetScrollPosition()
    {
        if (cardScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        cardScrollRect.verticalNormalizedPosition = 1f;
        cardScrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>销毁动态生成的奖励卡片。</summary>
    private void ClearSpawnedSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                Destroy(spawnedSlots[i].gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    /// <summary>再抽一次按钮回调。</summary>
    private void OnDrawAgainClicked()
    {
        if (string.IsNullOrWhiteSpace(lastPurchaseConfigId))
        {
            return;
        }

        string configId = lastPurchaseConfigId;
        Hide();
        onDrawAgainRequested?.Invoke(configId);
    }

    /// <summary>预览按钮回调。</summary>
    private void OnPreviewClicked()
    {
        if (!string.IsNullOrWhiteSpace(lastPoolConfigId))
        {
            onPreviewRequested?.Invoke(lastPoolConfigId, lastCrateTitle);
        }
    }

    /// <summary>将 RectTransform 设为全拉伸锚点。</summary>
    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
