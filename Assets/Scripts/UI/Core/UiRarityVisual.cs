using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按稀有度为边框/顶条着色（升级卡、装备格等）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在卡片根或顶边 Image 上。</para>
/// </remarks>
[DisallowMultipleComponent]
public class UiRarityVisual : MonoBehaviour
{
    [SerializeField] private Image borderImage;
    [SerializeField] private Image topAccentImage;
    [SerializeField] private bool useOutlineBorder;
    [SerializeField] private bool useEquipmentQuality;
    [SerializeField] private UpgradeRarity upgradeRarity = UpgradeRarity.Common;
    [SerializeField] private EquipmentQuality equipmentQuality = EquipmentQuality.Common;

    private static readonly Vector2 DefaultOutlineDistance = new Vector2(2f, -2f);

    /// <summary>编辑器 Reset 时自动绑定同物体 Image 为边框。</summary>
    private void Reset()
    {
        borderImage = GetComponent<Image>();
    }

    /// <summary>将边框改为透明底 + Outline，避免全屏 Image 遮挡卡片文字。</summary>
    public void EnsureOutlineBorderMode()
    {
        useOutlineBorder = true;
        Refresh();
    }

    /// <summary>按升级卡稀有度刷新边框与顶条颜色。</summary>
        /// <param name="rarity">升级卡稀有度。</param>
    public void ApplyUpgradeRarity(UpgradeRarity rarity)
    {
        useEquipmentQuality = false;
        upgradeRarity = rarity;
        Refresh();
    }

    /// <summary>按装备品质刷新边框与顶条颜色。</summary>
        /// <param name="quality">装备品质等级。</param>
    public void ApplyEquipmentQuality(EquipmentQuality quality)
    {
        useEquipmentQuality = true;
        equipmentQuality = quality;
        Refresh();
    }

    /// <summary>根据当前模式（升级稀有度或装备品质）重绘视觉。</summary>
    public void Refresh()
    {
        Color color = useEquipmentQuality
            ? UiTechWastelandPalette.GetEquipmentQualityColor(equipmentQuality)
            : UiTechWastelandPalette.GetUpgradeRarityColor(upgradeRarity);

        if (borderImage != null)
        {
            if (useOutlineBorder)
            {
                borderImage.color = Color.clear;
                borderImage.raycastTarget = false;
                Outline outline = GetOrAddOutline(borderImage);
                outline.effectColor = color;
            }
            else
            {
                borderImage.color = color;
            }
        }

        if (topAccentImage != null)
        {
            topAccentImage.color = color;
        }
    }

    /// <summary>启用时按序列化配置刷新稀有度配色。</summary>
    private void OnEnable()
    {
        Refresh();
    }

    private static Outline GetOrAddOutline(Image image)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline != null)
        {
            return outline;
        }

        outline = image.gameObject.AddComponent<Outline>();
        outline.effectDistance = DefaultOutlineDistance;
        outline.useGraphicAlpha = true;
        return outline;
    }
}
