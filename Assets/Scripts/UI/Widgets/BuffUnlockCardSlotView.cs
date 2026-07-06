using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Buff 解锁卡背包格子：同类型卡叠加，右下角显示数量。
/// </summary>
public class BuffUnlockCardSlotView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countText;

    /// <summary>刷新格子展示。</summary>
    public void SetData(BuffUnlockInventorySlot slot)
    {
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = slot.DisplayName;
            titleText.color = slot.IsGlobal
                ? UiTechWastelandPalette.PrimaryCyan
                : UiTechWastelandPalette.TextPrimary;
        }

        if (countText != null)
        {
            countText.text = slot.Count.ToString();
            countText.color = UiTechWastelandPalette.TextPrimary;
        }

        Sprite icon = BuffUnlockPresentationResolver.ResolveCardIcon(slot.CardConfigId, slot.SkillType);
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = slot.IsGlobal
                ? new Color(0.1f, 0.2f, 0.22f, 0.95f)
                : new Color(0.14f, 0.15f, 0.18f, 0.95f);
        }
    }

    /// <summary>隐藏格子。</summary>
    public void Clear() => gameObject.SetActive(false);
}
