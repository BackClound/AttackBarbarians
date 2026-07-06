using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 科技页下半区：Buff 解锁卡背包（占页面约 40%，纵向可滚动）。
/// </summary>
public class TechBuffUnlockCardBagPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text sectionTitleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform slotHost;
    [SerializeField] private BuffUnlockCardSlotView slotPrefab;
    [SerializeField] private TMP_Text emptyHintText;

    private readonly List<BuffUnlockCardSlotView> activeSlots = new List<BuffUnlockCardSlotView>(16);

    /// <summary>刷新背包格子。</summary>
    public void Refresh(IReadOnlyList<BuffUnlockInventorySlot> slots)
    {
        if (sectionTitleText != null)
        {
            sectionTitleText.text = "Buff 解锁卡背包";
            sectionTitleText.color = UiTechWastelandPalette.TextPrimary;
        }

        ClearSlots();
        if (slots == null || slots.Count == 0)
        {
            if (emptyHintText != null)
            {
                emptyHintText.gameObject.SetActive(true);
                emptyHintText.text = "背包暂无解锁卡";
            }

            return;
        }

        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(false);
        }

        if (slotPrefab == null || slotHost == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            BuffUnlockCardSlotView slot = Instantiate(slotPrefab, slotHost);
            slot.SetData(slots[i]);
            activeSlots.Add(slot);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void OnDestroy()
    {
        ClearSlots();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (activeSlots[i] != null)
            {
                Destroy(activeSlots[i].gameObject);
            }
        }

        activeSlots.Clear();
    }
}
