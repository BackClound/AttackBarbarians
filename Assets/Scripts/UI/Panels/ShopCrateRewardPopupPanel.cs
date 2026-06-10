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

    public void BindCallbacks(Action<string, string> previewRequested, Action<string> drawAgainRequested)
    {
        onPreviewRequested = previewRequested;
        onDrawAgainRequested = drawAgainRequested;
    }

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

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

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

    private static void HideLegacyRewardRoots(Transform panel)
    {
        SetInactiveIfExists(panel, "SingleDrawRoot");
        SetInactiveIfExists(panel, "TenDrawRoot");
    }

    private static void SetInactiveIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

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

    private void OnPreviewClicked()
    {
        if (!string.IsNullOrWhiteSpace(lastPoolConfigId))
        {
            onPreviewRequested?.Invoke(lastPoolConfigId, lastCrateTitle);
        }
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
