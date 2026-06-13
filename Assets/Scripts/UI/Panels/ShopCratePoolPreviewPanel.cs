using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 补给箱奖池预览弹窗：展示当前补给箱对应奖池内的卡牌与爆率。
/// </summary>
public class ShopCratePoolPreviewPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button scrimButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Transform cardGridRoot;
    [SerializeField] private UpgradeCardDisplayView cardSlotPrefab;

    private readonly List<UpgradeCardDisplayView> spawnedSlots = new List<UpgradeCardDisplayView>(16);

    /// <summary>绑定遮罩与关闭按钮，默认隐藏。</summary>
    private void Awake()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.AddListener(Hide);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    /// <summary>解绑弹窗按钮。</summary>
    private void OnDestroy()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(Hide);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    /// <summary>打开奖池预览并填充卡牌与爆率。</summary>
    /// <param name="poolConfigId">奖池配置 ID。</param>
    /// <param name="crateTitle">补给箱标题，用于副标题。</param>
    public void Show(string poolConfigId, string crateTitle)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "抽奖预览";
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.IsNullOrWhiteSpace(crateTitle)
                ? "PREVIEW"
                : $"{crateTitle} · PREVIEW";
        }

        PopulateCards(poolConfigId);
    }

    /// <summary>关闭奖池预览弹窗。</summary>
    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    /// <summary>从 UpgradeCardManager 填充奖池预览卡片。</summary>
    private void PopulateCards(string poolConfigId)
    {
        ClearSlots();

        if (!ServiceLocator.TryGet(out UpgradeCardManager manager))
        {
            return;
        }

        IReadOnlyList<UpgradeCardPoolPreviewEntry> entries = manager.GetPoolPreviewEntries(poolConfigId);
        if (entries == null || entries.Count == 0 || cardSlotPrefab == null || cardGridRoot == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UpgradeCardPoolPreviewEntry entry = entries[i];
            UpgradeCardDisplayView slot = Instantiate(cardSlotPrefab, cardGridRoot);
            slot.gameObject.SetActive(true);
            slot.SetPreviewEntry(entry);
            spawnedSlots.Add(slot);
        }
    }

    /// <summary>销毁已生成的预览卡片实例。</summary>
    private void ClearSlots()
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
}
