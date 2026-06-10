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

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

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
